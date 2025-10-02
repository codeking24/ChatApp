// chat.js - ChatApp SignalR Client
class ChatApp {
    constructor() {
        this.userId = window.currentUserId || 'anonymous';
        this.selectedUserId = null;
        this.selectedUsername = null;
        this.typingTimeout = null;
        this.connection = null;

        this.init();
    }

    init() {
        this.setupSignalR();
        this.bindUIEvents();
    }

    setupSignalR() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl('/chathub')
            .withAutomaticReconnect()
            .build();

        this.connection.onreconnecting(() => this.updateConnectionStatus(false));
        this.connection.onreconnected(() => {
            this.updateConnectionStatus(true);
            this.connection.invoke('Register', this.userId);
        });

        this.connection.on('ReceiveMessage', (msg) => this.onReceiveMessage(msg));
        this.connection.on('MessageSent', (msg) => this.appendMessage(msg, true));
        this.connection.on('UserTyping', (fromUserId, fromUserName) => this.showTypingIndicator(fromUserId, fromUserName));

        this.startConnection();
    }

    async startConnection() {
        try {
            await this.connection.start();
            console.log('Connected to SignalR Hub');
            await this.connection.invoke('Register', this.userId);
            this.updateConnectionStatus(true);
        } catch (err) {
            console.error('SignalR connection error:', err);
            this.updateConnectionStatus(false);
        }
    }

    bindUIEvents() {
        // User selection
        document.querySelectorAll('.user-item').forEach(item => {
            item.addEventListener('click', () => this.selectUser(item));
        });

        // Send buttons
        const sendBtn = document.getElementById('sendBtn');
        const sendOneTimeBtn = document.getElementById('sendOneTimeBtn');
        const messageInput = document.getElementById('messageInput');

        if (sendBtn) sendBtn.addEventListener('click', () => this.sendMessage(false));
        if (sendOneTimeBtn) sendOneTimeBtn.addEventListener('click', () => this.sendMessage(true));

        if (messageInput) {
            messageInput.addEventListener('input', () => this.onMessageInput());
            messageInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    this.sendMessage(false);
                } else if (this.selectedUserId) {
                    this.connection.invoke('Typing', this.userId, this.selectedUserId).catch(console.error);
                }
            });
        }
    }

    selectUser(item) {
        document.querySelectorAll('.user-item').forEach(i => i.classList.remove('active'));
        item.classList.add('active');

        this.selectedUserId = item.getAttribute('data-user-id');
        this.selectedUsername = item.querySelector('.user-name').textContent;

        document.getElementById('chatHeaderUsername').textContent = this.selectedUsername;
        document.getElementById('messagesContainer').innerHTML = '';
        document.getElementById('messageInput').disabled = false;
        document.getElementById('sendBtn').disabled = true;
        document.getElementById('sendOneTimeBtn').disabled = true;

        // Reset unread badge
        const badge = document.getElementById('unread-' + this.selectedUserId);
        if (badge) { badge.style.display = 'none'; badge.innerText = '0'; }

        // Load conversation
        fetch(`/Chat/Conversation?otherUserId=${this.selectedUserId}`)
            .then(res => res.json())
            .then(msgs => {
                if (msgs.length === 0) {
                    document.getElementById('messagesContainer').innerHTML = `
                        <div class="welcome-message">
                            <div class="welcome-icon">
                                <i class="fas fa-comments fa-4x text-muted mb-3"></i>
                            </div>
                            <h4 class="text-muted">No messages yet</h4>
                            <p class="text-muted">Start a conversation with ${this.selectedUsername}</p>
                        </div>
                    `;
                } else {
                    msgs.forEach(m => this.appendMessage(m, m.from === this.userId));
                }
                this.scrollToBottom(true);
                this.markAsRead(this.selectedUserId);
            }).catch(err => console.error(err));
    }

    onReceiveMessage(msg) {
        if (this.selectedUserId === msg.from) {
            this.appendMessage(msg, false);
            this.markAsRead(msg.from);
        } else {
            const badge = document.getElementById('unread-' + msg.from);
            if (badge) {
                badge.style.display = 'inline-block';
                badge.innerText = (parseInt(badge.innerText || '0') + 1).toString();
            }
            this.showNotification(msg.fromName, msg.message);
        }
    }

    appendMessage(msg, isOwn) {
        const messagesContainer = document.getElementById('messagesContainer');
        if (!messagesContainer) return;

        // Remove welcome message if exists
        const welcome = messagesContainer.querySelector('.welcome-message');
        if (welcome) welcome.remove();

        const div = document.createElement('div');
        div.classList.add('message-bubble', isOwn ? 'own' : 'other');

        const senderLabel = isOwn ? '' : `<div class="message-sender">${this.selectedUsername}</div>`;
        const time = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        const statusIcon = isOwn ? `<span class="message-status sent" id="status-${msg.id}">✓</span>` : '';

        div.innerHTML = `
            ${senderLabel}
            <div class="message-content">${msg.message}</div>
            <div class="message-time">${time} ${statusIcon}</div>
        `;

        div.setAttribute('data-message-id', msg.id || Date.now());
        messagesContainer.appendChild(div);
        this.scrollToBottom();
    }

    sendMessage(oneTime) {
        const textInput = document.getElementById('messageInput');
        const text = textInput.value.trim();
        if (!this.selectedUserId || !text) return;

        this.connection.invoke('SendMessage', this.userId, this.selectedUserId, text, oneTime)
            .catch(console.error);

        textInput.value = '';
        textInput.style.height = 'auto';
        document.getElementById('sendBtn').disabled = true;
        document.getElementById('sendOneTimeBtn').disabled = true;
    }

    onMessageInput() {
        const input = document.getElementById('messageInput');
        input.style.height = 'auto';
        input.style.height = (input.scrollHeight) + 'px';

        const hasText = input.value.trim().length > 0;
        document.getElementById('sendBtn').disabled = !hasText;
        document.getElementById('sendOneTimeBtn').disabled = !hasText;

        if (hasText && this.selectedUserId) {
            this.connection.invoke('Typing', this.userId, this.selectedUserId).catch(console.error);
        }
    }

    showTypingIndicator(userId, username) {
        if (this.selectedUserId !== userId) return;
        const indicator = document.getElementById('typingIndicator');
        const typingUser = document.getElementById('typingUser');
        if (typingUser) typingUser.textContent = username || 'Someone';
        if (indicator) {
            indicator.style.display = 'block';
            clearTimeout(this.typingTimeout);
            this.typingTimeout = setTimeout(() => indicator.style.display = 'none', 1500);
        }
    }

    markAsRead(fromUserId) {
        fetch('/Chat/MarkRead', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ fromUserId })
        }).catch(console.error);
    }

    scrollToBottom(instant = false) {
        const container = document.getElementById('messagesContainer');
        if (!container) return;

        if (instant) container.scrollTop = container.scrollHeight;
        else container.scrollTo({ top: container.scrollHeight, behavior: 'smooth' });
    }

    showNotification(from, message) {
        if (!("Notification" in window)) return;
        if (Notification.permission === "granted") {
            new Notification(`💬 New message from ${from}`, { body: message });
        } else if (Notification.permission !== "denied") {
            Notification.requestPermission().then(p => {
                if (p === "granted") new Notification(`💬 New message from ${from}`, { body: message });
            });
        }
    }

    updateConnectionStatus(isConnected) {
        const statusEl = document.getElementById('connection-status');
        if (!statusEl) return;
        const icon = statusEl.querySelector('i');
        const text = isConnected ? 'Connected' : 'Reconnecting...';
        if (icon) icon.className = isConnected ? 'fas fa-circle text-success me-1' : 'fas fa-circle text-warning me-1';
        statusEl.innerHTML = (icon ? icon.outerHTML : '') + text;
    }
}

// Initialize ChatApp
document.addEventListener('DOMContentLoaded', () => {
    window.chatApp = new ChatApp();
});

document.querySelectorAll('.user-item').forEach(item => {
    item.addEventListener('click', () => chatApp.selectUser(item));
});
