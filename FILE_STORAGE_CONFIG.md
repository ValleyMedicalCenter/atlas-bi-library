# File Storage Configuration for Approval Workflow

## Current Configuration

### Upload Location
Files are now saved to: **`\\valleymed\int\AtlasInbound\DataApprovalRequests`**

### File Naming
Files are automatically renamed using the format:
```
{timestamp}_{username}_{sanitized_original_name}.{extension}
```

Example: `20241016_143025_DOMAIN_jdoe_Monthly_Sales_Report.xlsx`

### Configuration Settings

In `appsettings.json`:
```json
{
  "ApprovalWorkflow": {
    "UploadDirectory": "\\\\valleymed\\int\\AtlasInbound\\DataApprovalRequests"
  }
}
```

## Key Changes Made

1. **Updated `Index.cshtml.cs`**:
   - Changed upload path from local `wwwroot/uploads` to network location
   - Added configurable upload directory via app settings
   - Enhanced filename generation with timestamp and username
   - Added file name sanitization for invalid characters
   - Added network access error handling
   - Added actual saved filename to session data

2. **Updated `appsettings.json`**:
   - Added `ApprovalWorkflow` configuration section
   - Set default network path
   - Added placeholders for Azure AI Agent endpoint (Logic App handled by AI agent)

3. **Enhanced Error Handling**:
   - Graceful handling of network path access issues
   - Clear error messages for users when storage is unavailable
   - Logging for troubleshooting network access problems

## Deployment Requirements

### Critical Steps:
1. **Network Access**: Ensure the web application's service account has read/write access to `\\valleymed\int\AtlasInbound\DataApprovalRequests`
2. **Directory Structure**: The application will automatically create the directory if it doesn't exist (permissions allowing)
3. **Testing**: Verify file upload works by testing the `/approval-request` page

### Service Account Permissions Needed:
- **Read** access to the network share
- **Write** access to create files
- **Create Folders** permission (for automatic directory creation)

### Troubleshooting:
- Check Windows Event Logs for access denied errors
- Verify the network path is accessible from the web server
- Test permissions using the same account that runs the web application
- Ensure the network share is online and available

## File Location Reference

**Network Location**: `\\valleymed\int\AtlasInbound\DataApprovalRequests`
**Configuration Key**: `ApprovalWorkflow:UploadDirectory`
**Fallback**: Uses network path as default if config is missing

Files uploaded through the approval workflow will now be immediately available at the network location for processing by other systems.