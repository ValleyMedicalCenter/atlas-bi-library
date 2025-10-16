using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;

namespace Atlas_Web.Pages.ApprovalRequest
{
    public class ApprovalHandlerModel : PageModel
    {
        private readonly ILogger<ApprovalHandlerModel> _logger;
        private readonly IConfiguration _configuration;

        public ApprovalHandlerModel(ILogger<ApprovalHandlerModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public string? RequestId { get; set; }
        public string? Action { get; set; }
        public string? ApproverName { get; set; }
        public string? RequesterName { get; set; }
        public string? FileName { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string requestId, string action)
        {
            // Validate parameters
            if (string.IsNullOrEmpty(requestId) || string.IsNullOrEmpty(action))
            {
                ErrorMessage = "Invalid request. Missing required parameters.";
                return Page();
            }

            // Validate action
            if (action.ToLower() != "approve" && action.ToLower() != "reject")
            {
                ErrorMessage = "Invalid action. Must be 'approve' or 'reject'.";
                return Page();
            }

            RequestId = requestId;
            Action = action.ToLower() == "approve" ? "Approved" : "Rejected";
            ApproverName = User.Identity?.Name ?? "Unknown";

            try
            {
                // **CORRECTED:** Get approval workflow database connection string
                var connectionString = _configuration.GetConnectionString("ApprovalWorkflowDatabase");

                if (string.IsNullOrEmpty(connectionString))
                {
                    ErrorMessage = "Database configuration error. Please contact IT support.";
                    _logger.LogError("ApprovalWorkflowDatabase connection string not found in configuration");
                    return Page();
                }

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // First, check if request exists and is still pending
                    var checkQuery = @"
                        SELECT UserName, FileName, Status 
                        FROM dbo.ApprovalRequests 
                        WHERE RequestID = @RequestId";

                    using (var checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@RequestId", requestId);

                        using (var reader = await checkCommand.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                            {
                                ErrorMessage = "Request not found. It may have been cancelled or does not exist.";
                                return Page();
                            }

                            RequesterName = reader["UserName"].ToString();
                            FileName = reader["FileName"].ToString();
                            var currentStatus = reader["Status"].ToString();

                            if (currentStatus != "Pending")
                            {
                                ErrorMessage = $"This request has already been {currentStatus.ToLower()}. No further action needed.";
                                return Page();
                            }
                        }
                    }

                    // Update the request with decision
                    var updateQuery = @"
                        UPDATE dbo.ApprovalRequests 
                        SET Status = @Status,
                            DecisionDate = @DecisionDate,
                            DecisionBy = @DecisionBy,
                            ModifiedDate = @ModifiedDate
                        WHERE RequestID = @RequestId";

                    using (var updateCommand = new SqlCommand(updateQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@Status", Action);
                        updateCommand.Parameters.AddWithValue("@DecisionDate", DateTime.Now);
                        updateCommand.Parameters.AddWithValue("@DecisionBy", ApproverName);
                        updateCommand.Parameters.AddWithValue("@ModifiedDate", DateTime.Now);
                        updateCommand.Parameters.AddWithValue("@RequestId", requestId);

                        int rowsAffected = await updateCommand.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            Success = true;
                            _logger.LogInformation(
                                "Approval request {RequestId} {Action} by {Approver}", 
                                requestId, Action, ApproverName);
                        }
                        else
                        {
                            ErrorMessage = "Failed to update request. Please contact IT support.";
                        }
                    }
                }

                // TODO: Send notification email to requester (optional)
                // await SendNotificationEmailAsync(RequesterName, Action, requestId);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error processing approval for request {RequestId}", requestId);
                ErrorMessage = "A database error occurred. Please contact IT support with request ID: " + requestId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing approval for request {RequestId}", requestId);
                ErrorMessage = "An error occurred while processing your decision. Please contact IT support.";
            }

            return Page();
        }
    }
}
