// Approval Chat Interface JavaScript

let chatHistory = [];
let isAIResponding = false;

document.addEventListener('DOMContentLoaded', function() {
    const messageInput = document.getElementById('messageInput');
    const sendButton = document.getElementById('sendButton');
    const chatMessages = document.getElementById('chatMessages');
    const submissionModal = document.getElementById('submissionModal');
    const loadingModal = document.getElementById('loadingModal');

    // Initialize chat
    initializeChat();

    // Message input handlers
    if (messageInput) {
        messageInput.addEventListener('keydown', function(e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendMessage();
            }
        });

        messageInput.addEventListener('input', function() {
            sendButton.disabled = this.value.trim().length === 0 || isAIResponding;
        });
    }

    // Auto-resize textarea
    if (messageInput) {
        messageInput.addEventListener('input', autoResize);
        function autoResize() {
            this.style.height = 'auto';
            this.style.height = this.scrollHeight + 'px';
        }
    }
});

function initializeChat() {
    // Focus on message input
    const messageInput = document.getElementById('messageInput');
    if (messageInput) {
        setTimeout(() => messageInput.focus(), 500);
    }

    // Scroll to bottom of chat
    scrollToBottom();
}

function sendMessage() {
    const messageInput = document.getElementById('messageInput');
    const message = messageInput.value.trim();
    
    if (message && !isAIResponding) {
        // Add user message to chat
        addUserMessage(message);
        messageInput.value = '';
        messageInput.style.height = 'auto';
        
        // Send to AI agent
        sendToAI(message);
    }
}

function sendQuickMessage(message) {
    if (!isAIResponding) {
        addUserMessage(message);
        sendToAI(message);
    }
}

function addUserMessage(message) {
    const chatMessages = document.getElementById('chatMessages');
    const messageElement = createMessageElement('user', message, 'You');
    
    chatMessages.appendChild(messageElement);
    scrollToBottom();
    
    // Add to chat history
    chatHistory.push({ role: 'user', content: message, timestamp: new Date() });
}

function addAIMessage(message) {
    const chatMessages = document.getElementById('chatMessages');
    const messageElement = createMessageElement('ai', message, 'Approval Assistant');
    
    chatMessages.appendChild(messageElement);
    scrollToBottom();
    
    // Add to chat history
    chatHistory.push({ role: 'assistant', content: message, timestamp: new Date() });
}

function createMessageElement(type, content, sender) {
    const wrapper = document.createElement('div');
    wrapper.className = 'message-wrapper mb-4';
    
    const messageDiv = document.createElement('div');
    messageDiv.className = `message ${type === 'ai' ? 'is-info' : 'is-primary'}`;
    
    const header = document.createElement('div');
    header.className = 'message-header';
    header.innerHTML = `
        <span class="icon">
            <i class="fas ${type === 'ai' ? 'fa-robot' : 'fa-user'}"></i>
        </span>
        <span>${sender}</span>
        <span class="is-size-7">${formatTimestamp(new Date())}</span>
    `;
    
    const body = document.createElement('div');
    body.className = 'message-body';
    
    const contentDiv = document.createElement('div');
    contentDiv.className = 'content';
    
    // Process message content (support basic markdown-like formatting)
    contentDiv.innerHTML = processMessageContent(content);
    
    body.appendChild(contentDiv);
    messageDiv.appendChild(header);
    messageDiv.appendChild(body);
    wrapper.appendChild(messageDiv);
    
    return wrapper;
}

function processMessageContent(content) {
    // Basic text processing for better display
    return content
        .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>') // Bold
        .replace(/\*(.*?)\*/g, '<em>$1</em>') // Italic
        .replace(/`(.*?)`/g, '<code>$1</code>') // Inline code
        .replace(/\n/g, '<br>'); // Line breaks
}

function formatTimestamp(date) {
    return date.toLocaleTimeString('en-US', { 
        hour: '2-digit', 
        minute: '2-digit',
        hour12: true 
    });
}

function scrollToBottom() {
    const chatMessages = document.getElementById('chatMessages');
    if (chatMessages) {
        setTimeout(() => {
            chatMessages.scrollTop = chatMessages.scrollHeight;
        }, 100);
    }
}

async function sendToAI(message) {
    const sendButton = document.getElementById('sendButton');
    const messageInput = document.getElementById('messageInput');
    
    try {
        isAIResponding = true;
        sendButton.disabled = true;
        messageInput.disabled = true;
        
        // Show typing indicator
        const typingIndicator = showTypingIndicator();
        
        const response = await fetch('/ApprovalRequest/Chat?handler=SendMessage', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify({ content: message })
        });
        
        // Remove typing indicator
        if (typingIndicator) {
            typingIndicator.remove();
        }
        
        if (response.ok) {
            const data = await response.json();
            if (data.success) {
                addAIMessage(data.message);
            } else {
                addAIMessage(data.error || 'Sorry, I encountered an error processing your message.');
            }
        } else {
            addAIMessage('Sorry, I\'m having trouble connecting right now. Please try again in a moment.');
        }
        
    } catch (error) {
        console.error('Error sending message to AI:', error);
        addAIMessage('Sorry, I encountered an error. Please try again.');
    } finally {
        isAIResponding = false;
        sendButton.disabled = false;
        messageInput.disabled = false;
        messageInput.focus();
    }
}

function showTypingIndicator() {
    const chatMessages = document.getElementById('chatMessages');
    const typingDiv = document.createElement('div');
    typingDiv.className = 'typing-indicator mb-4';
    typingDiv.innerHTML = `
        <div class="message is-light">
            <div class="message-header">
                <span class="icon">
                    <i class="fas fa-robot"></i>
                </span>
                <span>Approval Assistant</span>
                <span class="is-size-7">typing...</span>
            </div>
            <div class="message-body">
                <div class="typing-dots">
                    <span></span>
                    <span></span>
                    <span></span>
                </div>
            </div>
        </div>
    `;
    
    chatMessages.appendChild(typingDiv);
    scrollToBottom();
    
    return typingDiv;
}

function showSubmissionForm() {
    const submissionModal = document.getElementById('submissionModal');
    if (submissionModal) {
        submissionModal.classList.add('is-active');
        
        // Focus on description field
        const descriptionField = document.getElementById('requestDescription');
        if (descriptionField) {
            setTimeout(() => descriptionField.focus(), 100);
        }
    }
}

function closeSubmissionForm() {
    const submissionModal = document.getElementById('submissionModal');
    if (submissionModal) {
        submissionModal.classList.remove('is-active');
    }
}

async function submitApprovalRequest() {
    const description = document.getElementById('requestDescription').value.trim();
    const urgency = document.getElementById('urgencyLevel').value;
    const notes = document.getElementById('additionalNotes').value.trim();
    
    if (!description) {
        showNotification('Please provide a description for your request.', 'is-warning');
        return;
    }
    
    try {
        // Show loading
        showLoadingModal();
        closeSubmissionForm();
        
        // Prepare chat summary
        const chatSummary = generateChatSummary();
        
        const submissionData = {
            description: description,
            urgency: urgency,
            chatSummary: chatSummary
        };
        
        const response = await fetch('/ApprovalRequest/Chat?handler=SubmitApproval', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            },
            body: JSON.stringify(submissionData)
        });
        
        hideLoadingModal();
        
        if (response.ok) {
            const data = await response.json();
            if (data.success) {
                showSuccessPage(data.message, data.requestId);
            } else {
                showNotification(data.error || 'Failed to submit request. Please try again.', 'is-danger');
            }
        } else {
            showNotification('Failed to submit request. Please try again.', 'is-danger');
        }
        
    } catch (error) {
        hideLoadingModal();
        console.error('Error submitting approval request:', error);
        showNotification('An error occurred. Please try again.', 'is-danger');
    }
}

function generateChatSummary() {
    const userMessages = chatHistory.filter(msg => msg.role === 'user');
    const assistantMessages = chatHistory.filter(msg => msg.role === 'assistant');
    
    return {
        totalMessages: chatHistory.length,
        userQuestions: userMessages.length,
        assistantResponses: assistantMessages.length,
        keyTopics: extractKeyTopics(userMessages),
        duration: chatHistory.length > 0 ? 
            Math.round((new Date() - chatHistory[0].timestamp) / 60000) : 0 // minutes
    };
}

function extractKeyTopics(messages) {
    // Simple keyword extraction
    const keywords = [];
    const text = messages.map(msg => msg.content).join(' ').toLowerCase();
    
    const topicKeywords = {
        'timeline': ['timeline', 'time', 'when', 'how long', 'duration'],
        'requirements': ['require', 'need', 'necessary', 'must', 'should'],
        'process': ['process', 'workflow', 'steps', 'procedure'],
        'phi': ['phi', 'hipaa', 'privacy', 'protected', 'health'],
        'approval': ['approve', 'approval', 'permission', 'authorize']
    };
    
    for (const [topic, words] of Object.entries(topicKeywords)) {
        if (words.some(word => text.includes(word))) {
            keywords.push(topic);
        }
    }
    
    return keywords;
}

function showSuccessPage(message, requestId) {
    document.body.innerHTML = `
        <div class="hero is-success is-fullheight">
            <div class="hero-body">
                <div class="container has-text-centered">
                    <div class="columns is-centered">
                        <div class="column is-6">
                            <span class="icon is-large has-text-success">
                                <i class="fas fa-check-circle fa-4x"></i>
                            </span>
                            <h1 class="title is-2 has-text-white mt-4">Request Submitted Successfully!</h1>
                            <p class="subtitle has-text-white">${message}</p>
                            <div class="box mt-5">
                                <p><strong>Request ID:</strong> <code>${requestId}</code></p>
                                <p class="mt-3">You will receive email updates on the status of your request.</p>
                                <div class="buttons is-centered mt-5">
                                    <a href="/approval-request" class="button is-primary">
                                        <span class="icon">
                                            <i class="fas fa-plus"></i>
                                        </span>
                                        <span>Submit Another Request</span>
                                    </a>
                                    <a href="/" class="button is-light">
                                        <span class="icon">
                                            <i class="fas fa-home"></i>
                                        </span>
                                        <span>Return to Dashboard</span>
                                    </a>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `;
}

function showLoadingModal() {
    const loadingModal = document.getElementById('loadingModal');
    if (loadingModal) {
        loadingModal.classList.add('is-active');
    }
}

function hideLoadingModal() {
    const loadingModal = document.getElementById('loadingModal');
    if (loadingModal) {
        loadingModal.classList.remove('is-active');
    }
}

function showNotification(message, type = 'is-info') {
    const notification = document.createElement('div');
    notification.className = `notification ${type} is-fixed-notification`;
    notification.innerHTML = `
        <button class="delete" onclick="this.parentElement.remove()"></button>
        ${message}
    `;
    
    document.body.appendChild(notification);
    
    // Auto-remove after 5 seconds
    setTimeout(() => {
        if (notification.parentElement) {
            notification.remove();
        }
    }, 5000);
}

function getAntiForgeryToken() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]');
    return token ? token.value : '';
}

// Add CSS for chat styling
const style = document.createElement('style');
style.textContent = `
.typing-dots {
    display: inline-block;
}

.typing-dots span {
    display: inline-block;
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background-color: #3273dc;
    margin: 0 2px;
    animation: typing 1.4s infinite ease-in-out;
}

.typing-dots span:nth-child(1) { animation-delay: -0.32s; }
.typing-dots span:nth-child(2) { animation-delay: -0.16s; }
.typing-dots span:nth-child(3) { animation-delay: 0; }

@keyframes typing {
    0%, 80%, 100% { transform: scale(0); }
    40% { transform: scale(1); }
}

.chat-messages {
    scroll-behavior: smooth;
}

.message-wrapper:last-child {
    margin-bottom: 1rem !important;
}

.is-fixed-notification {
    position: fixed;
    top: 20px;
    right: 20px;
    z-index: 1000;
    max-width: 400px;
}

.approval-chat-page .panel {
    height: 100vh;
    overflow-y: auto;
}

.chat-input textarea {
    resize: none;
    min-height: 60px;
    max-height: 120px;
}

.loader-wrapper {
    display: flex;
    justify-content: center;
    align-items: center;
    height: 60px;
}
`;

document.head.appendChild(style);