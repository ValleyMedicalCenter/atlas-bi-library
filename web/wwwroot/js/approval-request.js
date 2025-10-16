// Approval Request File Upload JavaScript

document.addEventListener('DOMContentLoaded', function() {
    const fileInput = document.getElementById('fileInput');
    const fileName = document.getElementById('fileName');
    const uploadButton = document.getElementById('uploadButton');
    const uploadForm = document.getElementById('uploadForm');
    const fileUploadArea = document.getElementById('fileUploadArea');
    const uploadProgress = document.getElementById('uploadProgress');
    const progressBar = document.getElementById('progressBar');

    // File input change handler
    if (fileInput) {
        fileInput.addEventListener('change', function(e) {
            const file = e.target.files[0];
            if (file) {
                handleFileSelection(file);
            }
        });
    }

    // Drag and drop functionality
    if (fileUploadArea) {
        ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
            fileUploadArea.addEventListener(eventName, preventDefaults, false);
        });

        function preventDefaults(e) {
            e.preventDefault();
            e.stopPropagation();
        }

        ['dragenter', 'dragover'].forEach(eventName => {
            fileUploadArea.addEventListener(eventName, highlight, false);
        });

        ['dragleave', 'drop'].forEach(eventName => {
            fileUploadArea.addEventListener(eventName, unhighlight, false);
        });

        function highlight() {
            fileUploadArea.classList.add('drag-over');
        }

        function unhighlight() {
            fileUploadArea.classList.remove('drag-over');
        }

        fileUploadArea.addEventListener('drop', handleDrop, false);

        function handleDrop(e) {
            const files = e.dataTransfer.files;
            if (files.length > 0) {
                const file = files[0];
                fileInput.files = files;
                handleFileSelection(file);
            }
        }
    }

    function handleFileSelection(file) {
        // Validate file type
        const allowedTypes = ['.xlsx', '.xls', '.csv', '.txt', '.pdf', '.docx', '.doc'];
        const fileExtension = '.' + file.name.split('.').pop().toLowerCase();
        
        if (!allowedTypes.includes(fileExtension)) {
            showError('Invalid file type. Please select a supported file format.');
            resetFileInput();
            return;
        }

        // Update UI
        fileName.textContent = file.name;
        uploadButton.disabled = false;
        
        // Show file size
        const fileSize = formatFileSize(file.size);
        const helpText = document.querySelector('.help');
        if (helpText) {
            helpText.textContent = `Selected: ${file.name} (${fileSize})`;
        }
    }

    function resetFileInput() {
        fileInput.value = '';
        fileName.textContent = 'No file selected';
        uploadButton.disabled = true;
        const helpText = document.querySelector('.help');
        if (helpText) {
            helpText.textContent = 'Supported formats: .xlsx, .xls, .csv, .txt, .pdf, .docx, .doc';
        }
    }

    function formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }

    // Form submission with progress
    if (uploadForm) {
        uploadForm.addEventListener('submit', function(e) {
            e.preventDefault();
            
            const formData = new FormData(uploadForm);
            
            // Show progress
            showUploadProgress();
            
            // Create XMLHttpRequest for progress tracking
            const xhr = new XMLHttpRequest();
            
            // Progress handler
            xhr.upload.addEventListener('progress', function(e) {
                if (e.lengthComputable) {
                    const percentComplete = (e.loaded / e.total) * 100;
                    updateProgress(percentComplete);
                }
            });

            // Completion handler
            xhr.addEventListener('load', function() {
                if (xhr.status === 200) {
                    // Reload page to show results
                    window.location.reload();
                } else {
                    hideUploadProgress();
                    showError('Upload failed. Please try again.');
                }
            });

            // Error handler
            xhr.addEventListener('error', function() {
                hideUploadProgress();
                showError('Upload failed. Please check your connection and try again.');
            });

            // Send request
            xhr.open('POST', uploadForm.action);
            xhr.send(formData);
        });
    }

    function showUploadProgress() {
        const uploadContent = document.querySelector('.file-upload-content');
        if (uploadContent) {
            uploadContent.style.display = 'none';
        }
        if (uploadProgress) {
            uploadProgress.style.display = 'block';
        }
        if (uploadButton) {
            uploadButton.disabled = true;
        }
    }

    function hideUploadProgress() {
        const uploadContent = document.querySelector('.file-upload-content');
        if (uploadContent) {
            uploadContent.style.display = 'block';
        }
        if (uploadProgress) {
            uploadProgress.style.display = 'none';
        }
        if (uploadButton) {
            uploadButton.disabled = false;
        }
        if (progressBar) {
            progressBar.value = 0;
        }
    }

    function updateProgress(percent) {
        if (progressBar) {
            progressBar.value = percent;
        }
    }

    function showError(message) {
        // Create or update error notification
        let errorNotification = document.querySelector('.notification.is-danger');
        
        if (!errorNotification) {
            errorNotification = document.createElement('div');
            errorNotification.className = 'notification is-danger';
            errorNotification.innerHTML = `
                <button class="delete" onclick="this.parentElement.style.display='none'"></button>
                <strong>Error:</strong> <span class="error-message"></span>
            `;
            
            const container = document.querySelector('.column.is-8');
            if (container) {
                container.insertBefore(errorNotification, container.firstChild);
            }
        }
        
        const errorMessageSpan = errorNotification.querySelector('.error-message');
        if (errorMessageSpan) {
            errorMessageSpan.textContent = message;
        }
        
        errorNotification.style.display = 'block';
        
        // Scroll to top to show error
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    // Keyboard shortcuts
    document.addEventListener('keydown', function(e) {
        // Enter key on file input
        if (e.target === fileInput && e.key === 'Enter') {
            e.target.click();
        }
        
        // Ctrl+Enter to submit form
        if (e.ctrlKey && e.key === 'Enter' && !uploadButton.disabled) {
            uploadForm.dispatchEvent(new Event('submit'));
        }
    });
});

// CSS for drag and drop styling
const style = document.createElement('style');
style.textContent = `
.file-upload-area {
    border: 2px dashed #dbdbdb;
    border-radius: 6px;
    padding: 2rem;
    text-align: center;
    transition: all 0.3s ease;
    cursor: pointer;
}

.file-upload-area:hover,
.file-upload-area.drag-over {
    border-color: #3273dc;
    background-color: #f0f8ff;
}

.file-upload-area .fa-cloud-upload-alt {
    margin-bottom: 1rem;
}

.file-upload-progress {
    text-align: center;
    padding: 2rem;
}

.loader {
    border: 4px solid #f3f3f3;
    border-top: 4px solid #3273dc;
    border-radius: 50%;
    width: 40px;
    height: 40px;
    animation: spin 1s linear infinite;
    margin: 0 auto;
}

@keyframes spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
}

.approval-request-page .card {
    box-shadow: 0 0.5em 1em -0.125em rgba(10, 10, 10, 0.1), 0 0px 0 1px rgba(10, 10, 10, 0.02);
}

.approval-request-page .hero {
    margin-bottom: 2rem;
}
`;

document.head.appendChild(style);