# Document Approval Workflow Implementation

## Overview
This document outlines the complete implementation of the Document Approval Workflow feature with AI agent chat interface for the Atlas BI Library.

## Features Implemented

### 1. File Upload Page (`/approval-request`)
- **Location**: `web/Pages/ApprovalRequest/Index.cshtml`
- **Controller**: `web/Pages/ApprovalRequest/Index.cshtml.cs`
- **Features**:
  - Drag-and-drop file upload
  - File type validation (.xlsx, .xls, .csv, .txt, .pdf, .docx, .doc)
  - No file size limits (as requested)
  - Progress indicator during upload
  - PHI detection for CSV and Excel files
  - Session management for workflow state
  - User-friendly error handling

### 2. AI Chat Interface (`/approval-request/chat`)
- **Location**: `web/Pages/ApprovalRequest/Chat.cshtml`
- **Controller**: `web/Pages/ApprovalRequest/Chat.cshtml.cs`
- **Features**:
  - Real-time chat with Azure AI Agent
  - Context-aware conversations (file info, PHI status)
  - Quick action buttons for common questions
  - Typing indicators and message history
  - Submission form for final approval request
  - AI agent handles Logic App integration for workflow processing

### 3. User Interface Components
- **File Upload Widget**: Bulma-based design with hover effects
- **PHI Detection Display**: Warning/success notifications with column details
- **Chat Interface**: Professional messaging layout with timestamps
- **Progress Indicators**: Loading states and upload progress
- **Responsive Design**: Mobile-friendly layout

### 4. Technical Implementation
- **Session Management**: Secure storage of workflow state
- **Error Handling**: Comprehensive error messages and recovery
- **API Integration**: Azure AI Agent and Logic App endpoints
- **Security**: File validation, XSS protection, CSRF tokens
- **Performance**: Async operations, optimized file handling

## File Structure

```
web/
├── Pages/ApprovalRequest/
│   ├── Index.cshtml                 # File upload page
│   ├── Index.cshtml.cs             # Upload controller
│   ├── Chat.cshtml                 # AI chat interface
│   └── Chat.cshtml.cs              # Chat controller
├── wwwroot/
│   ├── js/
│   │   ├── approval-request.js     # Upload functionality
│   │   └── approval-chat.js        # Chat functionality
│   └── css/
│       └── approval-workflow.scss   # Styling
└── uploads/approval-requests/       # Local fallback directory (files saved to network location)
```

## PHI Detection Algorithm

The system scans CSV and Excel file headers for common PHI indicators:
- Medical Record Numbers (MRN, patient_mrn, medical_record)
- Social Security Numbers (SSN, social_security, patient_ssn)
- Dates of Birth (DOB, date_of_birth, birthdate, patient_dob)
- Names (patient_name, first_name, last_name, full_name)
- Contact Information (address, street, phone, email, patient_email)
- Insurance Information (insurance, policy_number, member_id)

## Configuration Requirements

### App Settings
```json
{
  "ApprovalWorkflow": {
    "UploadDirectory": "\\\\valleymed\\int\\AtlasInbound\\DataApprovalRequests"
  },
  "AzureAI": {
    "ApprovalAgentEndpoint": "https://your-ai-agent.endpoint",
    "ApiKey": "your-api-key"
  }
}
```

**Note**: No Logic App configuration is needed in the application. The AI agent handles calling the Logic App behind the scenes.

### Session Configuration
Ensure session state is enabled in `Program.cs`:
```csharp
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
```

## User Workflow

1. **File Upload**: User navigates to `/approval-request` and uploads document
2. **PHI Detection**: System analyzes file and displays PHI status
3. **Chat Interface**: User clicks "Continue" to access AI assistant
4. **Information Gathering**: AI helps user prepare approval request
5. **Submission**: User fills final form and submits to Logic App
6. **Confirmation**: System displays success message with request ID

## Testing Checklist

### File Upload Testing
- [ ] Drag and drop file upload works
- [ ] File type validation prevents invalid uploads
- [ ] PHI detection correctly identifies sensitive columns
- [ ] Progress indicator shows during upload
- [ ] Error messages display for invalid files
- [ ] Session data persists between pages

### Chat Interface Testing
- [ ] AI agent responses work (with proper configuration)
- [ ] Quick action buttons send messages
- [ ] Typing indicator appears during AI responses
- [ ] Chat history maintains throughout session
- [ ] Submission modal opens and validates input
- [ ] Final submission redirects to success page

### Integration Testing
- [ ] Session management works between pages
- [ ] File upload data passes to chat interface
- [ ] PHI detection results display in chat sidebar
- [ ] Final submission includes all required data
- [ ] Logic App receives properly formatted payload

## Browser Compatibility
- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+

## Security Considerations
- File uploads are validated for type and content
- PHI detection is performed server-side
- Session data is encrypted and expires automatically
- CSRF protection on all forms
- XSS protection via content sanitization
- No sensitive data stored permanently

## Performance Notes
- File uploads use streaming for large files
- Chat interface uses WebSocket-like polling for real-time feel
- Assets are minified and optimized for production
- Responsive design reduces mobile bandwidth usage

## Future Enhancements
- Real-time WebSocket chat connections
- Advanced PHI detection using ML models
- File preview capabilities
- Batch file upload support
- Email notifications for status updates
- Admin dashboard for approval management

## Dependencies Added
- No new NuGet packages required
- Uses existing Bulma CSS framework
- Leverages built-in ASP.NET Core features
- Compatible with current Atlas BI Library architecture

## File Storage

Files are uploaded to the network location: `\\valleymed\int\AtlasInbound\DataApprovalRequests`

### File Naming Convention
Uploaded files are renamed using the pattern:
`{timestamp}_{username}_{sanitized_filename}.{extension}`

Example: `20241016_143025_domain_user_quarterly_report.xlsx`

### Configuration
- Default path: `\\valleymed\int\AtlasInbound\DataApprovalRequests`
- Configurable via `ApprovalWorkflow:UploadDirectory` in appsettings.json
- Automatic directory creation if path doesn't exist
- Network path permissions must allow write access for the application service account

## Deployment Notes
- **CRITICAL**: Ensure the application service account has read/write permissions to `\\valleymed\int\AtlasInbound\DataApprovalRequests`
- Configure Azure AI Agent endpoint and API key
- Test session configuration in production environment
- Set up file cleanup scheduled tasks if needed
- Test network path accessibility from the web server
- The AI agent will handle Logic App integration and email notifications

## Support
For questions about this implementation, refer to:
- `WARP.md` for development guidance
- Individual file comments for specific functionality
- Atlas BI Library documentation for broader context