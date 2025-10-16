using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Azure;
using Azure.AI.OpenAI.Assistants;
using Azure.Identity;
using System.Text.Json;

namespace Atlas_Web.Pages.ApprovalRequest
{
    public class ChatModel : PageModel
    {
        private readonly ILogger<ChatModel> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public ChatModel(ILogger<ChatModel> logger, IConfiguration configuration, HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public string? FileName { get; set; }
        public string? FileUrl { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string? RequestId { get; set; }
        public bool HasPHI { get; set; }
        public List<string> DetectedPHIColumns { get; set; } = new List<string>();
        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            // Load session data
            FileName = HttpContext.Session.GetString("ApprovalRequest_FileName");
            FileUrl = HttpContext.Session.GetString("ApprovalRequest_FileUrl");
            UserName = HttpContext.Session.GetString("ApprovalRequest_UserName");
            UserEmail = HttpContext.Session.GetString("ApprovalRequest_UserEmail");
            RequestId = HttpContext.Session.GetString("ApprovalRequest_RequestId");
            
            var detectedPHI = HttpContext.Session.GetString("ApprovalRequest_DetectedPHI");
            if (!string.IsNullOrEmpty(detectedPHI))
            {
                DetectedPHIColumns = detectedPHI.Split(',').Select(c => c.Trim()).ToList();
                HasPHI = DetectedPHIColumns.Count > 0;
            }

            // Validate session data
            if (string.IsNullOrEmpty(FileName) || string.IsNullOrEmpty(RequestId))
            {
                ErrorMessage = "Session expired or invalid. Please upload your file again.";
                return RedirectToPage("/ApprovalRequest/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSendMessageAsync([FromBody] ChatMessageRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.Message))
                {
                    return new JsonResult(new { success = false, error = "Message cannot be empty" });
                }

                // Get Azure AI configuration
                var endpoint = _configuration["AzureAI:Endpoint"];
                var apiKey = _configuration["AzureAI:ApiKey"];
                var agentId = _configuration["AzureAI:AgentId"];

                if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(agentId))
                {
                    _logger.LogError("Azure AI configuration is missing");
                    return new JsonResult(new { success = false, error = "Azure AI is not configured. Please contact IT support." });
                }

                // Initialize Assistants Client
                AssistantsClient client = new AssistantsClient(
                    new Uri(endpoint),
                    new AzureKeyCredential(apiKey)
                );

                // Get or create thread
                var threadId = HttpContext.Session.GetString("ApprovalRequest_ThreadId");
                AssistantThread thread;

                if (string.IsNullOrEmpty(threadId))
                {
                    _logger.LogInformation("Creating new assistant thread");
                    Response<AssistantThread> threadResponse = await client.CreateThreadAsync();
                    thread = threadResponse.Value;
                    threadId = thread.Id;
                    HttpContext.Session.SetString("ApprovalRequest_ThreadId", threadId);
                    _logger.LogInformation("Created thread: {ThreadId}", threadId);
                }

                // Prepare message with session context for first message
                var messageToSend = request.Message;
                var isFirstUserMessage = HttpContext.Session.GetInt32("MessageCount") ?? 0;
                
                if (isFirstUserMessage == 0)
                {
                    // Include session context in first message
                    var fileName = HttpContext.Session.GetString("ApprovalRequest_FileName");
                    var fileUrl = HttpContext.Session.GetString("ApprovalRequest_FileUrl");
                    var userName = HttpContext.Session.GetString("ApprovalRequest_UserName");
                    var userEmail = HttpContext.Session.GetString("ApprovalRequest_UserEmail");
                    var requestIdFromSession = HttpContext.Session.GetString("ApprovalRequest_RequestId");
                    var detectedPHI = HttpContext.Session.GetString("ApprovalRequest_DetectedPHI") ?? "";

                    messageToSend = $@"[Session Context]
FileName: {fileName}
FileUrl: {fileUrl}
UserName: {userName}
UserEmail: {userEmail}
RequestId: {requestIdFromSession}
DetectedPHI: {detectedPHI}

[User Message]
{request.Message}";

                    _logger.LogInformation("Sending first message with session context");
                }

                // Increment message count
                HttpContext.Session.SetInt32("MessageCount", isFirstUserMessage + 1);

                // Add user message to thread
                Response<ThreadMessage> messageResponse = await client.CreateMessageAsync(
                    threadId,
                    MessageRole.User,
                    messageToSend
                );

                _logger.LogInformation("User message added to thread {ThreadId}", threadId);

                // Create and run the assistant
                Response<ThreadRun> runResponse = await client.CreateRunAsync(
                    threadId,
                    new CreateRunOptions(agentId)
                );

                ThreadRun run = runResponse.Value;
                _logger.LogInformation("Assistant run created: {RunId}, Status: {Status}", run.Id, run.Status);

                // Poll for completion (with timeout)
                int maxAttempts = 60; // 60 seconds timeout
                int attempts = 0;
                
                while ((run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress) && attempts < maxAttempts)
                {
                    await Task.Delay(1000);
                    Response<ThreadRun> runStatusResponse = await client.GetRunAsync(threadId, run.Id);
                    run = runStatusResponse.Value;
                    attempts++;
                    
                    if (attempts % 5 == 0)
                    {
                        _logger.LogInformation("Assistant run {RunId} status: {Status} (attempt {Attempt})", run.Id, run.Status, attempts);
                    }
                }

                if (run.Status != RunStatus.Completed)
                {
                    _logger.LogWarning("Assistant run did not complete. Final status: {Status}", run.Status);
                    return new JsonResult(new 
                    { 
                        success = false, 
                        error = $"Assistant did not respond in time. Status: {run.Status}" 
                    });
                }

                _logger.LogInformation("Assistant run completed successfully");

                // Get the assistant's response
                Response<PageableList<ThreadMessage>> messagesResponse = await client.GetMessagesAsync(threadId);
                PageableList<ThreadMessage> messages = messagesResponse.Value;
                
                var lastAssistantMessage = messages.Data
                    .Where(m => m.Role == MessageRole.Assistant)
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefault();

                if (lastAssistantMessage == null)
                {
                    _logger.LogWarning("No assistant message found in thread");
                    return new JsonResult(new { success = false, error = "No response from assistant" });
                }

                // Extract text from message content
                var responseText = string.Empty;
                foreach (var contentItem in lastAssistantMessage.ContentItems)
                {
                    if (contentItem is MessageTextContent textContent)
                    {
                        responseText += textContent.Text;
                    }
                }

                _logger.LogInformation("Assistant response retrieved successfully");

                return new JsonResult(new
                {
                    success = true,
                    message = responseText,
                    threadId = threadId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error communicating with Azure AI assistant");
                return new JsonResult(new 
                { 
                    success = false, 
                    error = "Failed to communicate with AI assistant. Please try again or contact IT support.",
                    details = ex.Message 
                });
            }
        }

        public async Task<IActionResult> OnPostSubmitApprovalAsync([FromBody] ApprovalSubmitRequest request)
        {
            try
            {
                _logger.LogInformation("Manual approval submission received");
                
                return new JsonResult(new
                {
                    success = true,
                    message = "The assistant handles approval submission through the workflow orchestrator."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in manual approval submission");
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }
    }

    // Request models
    public class ChatMessageRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ApprovalSubmitRequest
    {
        public string ApproverEmail { get; set; } = string.Empty;
        public string ApproverName { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
    }
}
