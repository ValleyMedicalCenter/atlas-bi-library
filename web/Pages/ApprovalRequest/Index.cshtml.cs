using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using OfficeOpenXml; // For Excel
using System.Data;

namespace Atlas_Web.Pages.ApprovalRequest
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IConfiguration _configuration;

        public IndexModel(ILogger<IndexModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [BindProperty]
        public FileUploadData UploadData { get; set; } = new FileUploadData();

        public string? UploadedFileName { get; set; }
        public string? FileUrl { get; set; }
        public bool HasPHI { get; set; }
        public List<string> DetectedPHIColumns { get; set; } = new List<string>();
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public void OnGet()
        {
            // Initialize the page
        }

        public async Task<IActionResult> OnPostUploadAsync()
        {
            if (UploadData.File == null || UploadData.File.Length == 0)
            {
                ErrorMessage = "Please select a file to upload.";
                return Page();
            }

            // Validate file type
            var allowedExtensions = new[] { ".xlsx", ".xls", ".csv", ".txt", ".pdf", ".docx", ".doc" };
            var fileExtension = Path.GetExtension(UploadData.File.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                ErrorMessage = "Invalid file type. Please upload .xlsx, .xls, .csv, .txt, .pdf, .docx, or .doc files only.";
                return Page();
            }

            try
            {
                // Generate unique filename with timestamp and user info
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var userName = User.Identity?.Name?.Replace("\\", "_") ?? "Unknown";
                var sanitizedOriginalName = SanitizeFileName(Path.GetFileNameWithoutExtension(UploadData.File.FileName));
                fileExtension = Path.GetExtension(UploadData.File.FileName);
                var fileName = $"{timestamp}_{userName}_{sanitizedOriginalName}{fileExtension}";

                // Get upload directory from configuration, with fallback
                var uploadsPath = _configuration["ApprovalWorkflow:UploadDirectory"] ?? @"\\valleymed\int\AtlasInbound\DataApprovalRequests";

                // Create directory if it doesn't exist
                try
                {
                    Directory.CreateDirectory(uploadsPath);
                }
                catch (Exception dirEx)
                {
                    _logger.LogError(dirEx, "Failed to create directory: {UploadPath}", uploadsPath);
                    ErrorMessage = "Unable to access the file storage location. Please contact your system administrator.";
                    return Page();
                }

                var filePath = Path.Combine(uploadsPath, fileName);

                // Save file to network location
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await UploadData.File.CopyToAsync(stream);
                }

                // Store file info in session
                UploadedFileName = UploadData.File.FileName;
                FileUrl = filePath; // Store full network path instead of web URL
                HttpContext.Session.SetString("ApprovalRequest_FileName", UploadedFileName);
                HttpContext.Session.SetString("ApprovalRequest_FileUrl", FileUrl);
                HttpContext.Session.SetString("ApprovalRequest_ActualFileName", fileName); // Store the actual saved filename
                HttpContext.Session.SetString("ApprovalRequest_UserName", User.Identity?.Name ?? "Unknown");
                HttpContext.Session.SetString("ApprovalRequest_UserEmail", User.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? "");
                HttpContext.Session.SetString("ApprovalRequest_RequestId", Guid.NewGuid().ToString());
                HttpContext.Session.SetString("ApprovalRequest_UploadTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                // Perform PHI detection for supported file types
                if (fileExtension == ".csv" || fileExtension == ".xlsx" || fileExtension == ".xls")
                {
                    var (hasPHI, columns) = await DetectPHIInFileAsync(filePath);
                    DetectedPHIColumns = columns;
                    HasPHI = hasPHI;
                    HttpContext.Session.SetString("ApprovalRequest_HasPHI", HasPHI.ToString());
                    HttpContext.Session.SetString("ApprovalRequest_DetectedPHI", string.Join(",", DetectedPHIColumns));
                }

                SuccessMessage = "File uploaded successfully!";
                _logger.LogInformation("File uploaded successfully: {FileName} by user {UserName}", UploadedFileName, User.Identity?.Name);
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred during file upload. Please try again.";
                _logger.LogError(ex, "Error uploading file: {FileName}", UploadData.File.FileName);
            }

            return Page();
        }

        private async Task<(bool hasPHI, List<string> columns)> DetectPHIInFileAsync(string filePath)
        {
            var phiKeywords = new List<string>
            {
                "mrn", "medical_record", "patient_id", "patient_number", "patient_name",
                "first_name", "last_name", "ssn", "social_security", "dob", "date_of_birth",
                "address", "street", "zip", "phone", "email", "diagnosis", "icd", "procedure",
                "cpt", "insurance", "subscriber"
            };

            var detectedColumns = new List<string>();
            var fileExtension = Path.GetExtension(filePath).ToLower();

            try
            {
                List<string> columnNames = new List<string>();

                // Handle CSV files
                if (fileExtension == ".csv")
                {
                    using (var reader = new StreamReader(filePath))
                    {
                        var headerLine = await reader.ReadLineAsync();
                        if (headerLine != null)
                        {
                            columnNames = headerLine.Split(',').Select(c => c.Trim()).ToList();
                        }
                    }
                }
                // Handle Excel files
                else if (fileExtension == ".xlsx" || fileExtension == ".xls")
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Or Commercial
                    using (var package = new ExcelPackage(new FileInfo(filePath)))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet != null && worksheet.Dimension != null)
                        {
                            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                            {
                                var cellValue = worksheet.Cells[1, col].Value?.ToString();
                                if (!string.IsNullOrEmpty(cellValue))
                                {
                                    columnNames.Add(cellValue);
                                }
                            }
                        }
                    }
                }

                // Check each column for PHI keywords
                foreach (var column in columnNames)
                {
                    var columnLower = column.ToLower().Replace(" ", "_").Replace("-", "_");
                    if (phiKeywords.Any(keyword => columnLower.Contains(keyword)))
                    {
                        detectedColumns.Add(column);
                    }
                }

                return (detectedColumns.Count > 0, detectedColumns);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting PHI in file {FilePath}", filePath);
                return (false, new List<string>());
            }
        }

        private static string SanitizeFileName(string fileName)
        {
            // Remove invalid characters from filename
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());

            // Replace spaces with underscores and limit length
            sanitized = sanitized.Replace(" ", "_").Replace("-", "_");

            // Limit filename length to prevent issues
            if (sanitized.Length > 50)
            {
                sanitized = sanitized.Substring(0, 50);
            }

            return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
        }

        public IActionResult OnGetContinueToChat()
        {
            // Verify file was uploaded
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("ApprovalRequest_FileName")))
            {
                ErrorMessage = "No file uploaded. Please upload a file first.";
                return Page();
            }

            return RedirectToPage("/ApprovalRequest/Chat");
        }
    }

    public class FileUploadData
    {
        [Required]
        public IFormFile? File { get; set; }
    }
}
