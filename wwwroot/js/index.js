// Chat Application Main Class
class ChatApplication {
    constructor(vapidPublicKey) {
        this.userId = document.querySelector('.chat-app')?.getAttribute('data-user-id') || '1';
        this.username = document.querySelector('.chat-app')?.getAttribute('data-username') || 'John Doe';
        this.selectedUserId = null;
        this.selectedUsername = null;
        this.typingTimeout = null;
        this.isDarkTheme = false;
        this.onlineUsers = 3;
        this.isAutoScrolling = true;
        this.connection = null;
        this.vapidPublicKey = vapidPublicKey;
        this.notificationCount = 0;
        this.notificationBadge = document.getElementById('notificationCount');
        this.notificationList = document.getElementById('notificationList');

        this.init();
    }

    init() {
        this.setupSignalR();
        this.setupEventListeners();
        this.loadSavedTheme();
        this.setupAutoScrollIndicator();
        this.requestNotificationPermission();

        // Update header with username
        document.getElementById('header-username').textContent = this.username;

        this.registerPush(this.userId);
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
        this.connection.on('ReceiveFollowRequest', (requests) => {
            this.notificationCount = requests.length;
            if (this.notificationCount > 0) {
                this.notificationBadge.style.display = 'inline-block';
                this.notificationBadge.textContent = this.notificationCount;
            } else {
                this.notificationBadge.style.display = 'none';
            }

            this.notificationList.innerHTML = '';
            requests.forEach(req => {
                const div = document.createElement('div');
                div.className = 'dropdown-item d-flex justify-content-between align-items-center';
                div.innerHTML = `
            <span>${req.fullName}</span>
            <button class="btn btn-sm btn-success accept-btn" data-user-id="${req.id}">Accept</button>
        `;
                this.notificationList.appendChild(div);
            });
        });

        this.startConnection();
    }
    async startConnection() {
        try {
            await this.connection.start();
            console.log('Connected to SignalR Hub');
            await this.connection.invoke('Register', this.userId);
            this.updateConnectionStatus(true);
            this.loadPendingFollowRequests();
        } catch (err) {
            console.error('SignalR connection error:', err);
            this.updateConnectionStatus(false);
        }
    }

    async loadPendingFollowRequests() {
        try {
            const res = await fetch('/Account/PendingFollowRequests');
            const data = await res.json();
            if (!data.success) return;

            this.notificationCount = data.pending.length;
            this.updateNotificationBadge(data.pending);
        } catch (err) {
            console.error(err);
        }
    }

    updateNotificationBadge(requests) {
        if (!this.notificationBadge || !this.notificationList) return;

        if (requests.length > 0) {
            this.notificationBadge.style.display = 'inline-block';
            this.notificationBadge.textContent = requests.length;
        } else {
            this.notificationBadge.style.display = 'none';
        }

        this.notificationList.innerHTML = '';
        requests.forEach(req => {
            const div = document.createElement('div');
            div.className = 'dropdown-item d-flex justify-content-between align-items-center';
            div.innerHTML = `
            <span>${req.fullName}</span>
            <button class="btn btn-sm btn-success accept-btn" data-user-id="${req.id}">Accept</button>
        `;
            this.notificationList.appendChild(div);
        });
    }
    updateConnectionStatus(isConnected) {
        const statusEl = document.getElementById('connection-status');
        if (!statusEl) return;
        const icon = statusEl.querySelector('i');
        const text = isConnected ? 'Connected' : 'Reconnecting...';
        if (icon) icon.className = isConnected ? 'fas fa-circle text-success me-1' : 'fas fa-circle text-warning me-1';
        statusEl.innerHTML = (icon ? icon.outerHTML : '') + text;
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

    markAsRead(fromUserId) {
        fetch('/Chat/MarkRead', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ fromUserId })
        }).catch(console.error);
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

    setupEventListeners() {
        // Send message buttons
        const sendBtn = document.getElementById('sendBtn');
        const sendOneTimeBtn = document.getElementById('sendOneTimeBtn');
        const messageInput = document.getElementById('messageInput');
        
        if (sendBtn) {
            sendBtn.addEventListener('click', () => this.sendMessage(false));
        }

        if (sendOneTimeBtn) {
            sendOneTimeBtn.addEventListener('click', () => this.sendMessage(true));
        }

        if (messageInput) {
            // Message input events
            messageInput.addEventListener('input', () => {
                this.handleMessageInput();
            });

            messageInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    this.sendMessage(false);
                }
            });
        }

        // Theme toggle
        const themeToggle = document.getElementById('theme-toggle');
        if (themeToggle) {
            themeToggle.addEventListener('click', (e) => {
                e.preventDefault();
                this.toggleTheme();
            });
        }

        // Toggle users list on mobile
        const toggleUsers = document.getElementById('toggle-users');
        if (toggleUsers) {
            toggleUsers.addEventListener('click', (e) => {
                e.preventDefault();
                document.querySelector('.chat-users').classList.toggle('collapsed');
            });
        }

        const notificationToggle = document.getElementById('notificationToggle');
        if (notificationToggle) {
            notificationToggle.addEventListener('click', (e) => {
                e.preventDefault();
                document.querySelector('.chat-users').classList.toggle('collapsed');
            });
        }

        // Auto-scroll indicator
        const autoScrollIndicator = document.getElementById('autoScrollIndicator');
        if (autoScrollIndicator) {
            autoScrollIndicator.addEventListener('click', () => {
                this.scrollToBottom(true);
            });
        }

        // Messages container scroll listener
        const messagesContainer = document.getElementById('messagesContainer');
        if (messagesContainer) {
            messagesContainer.addEventListener('scroll', () => {
                this.checkScrollPosition();
            });
        }

        // User search
        const userSearch = document.getElementById('userSearch');
        if (userSearch) {
            userSearch.addEventListener('input', (e) => {
                this.filterUsers(e.target.value);
            });
        }

        if (this.notificationList) {
            this.notificationList.addEventListener('click', async (e) => {
                if (!e.target.classList.contains('accept-btn')) return;

                const btn = e.target;
                const userId = btn.dataset.userId;

                try {
                    const res = await apiFetch(`/Account/AcceptFollow/${userId}`, { method: 'POST' });
                    if (res?.success) {
                        // Remove the accepted request from the dropdown
                        btn.closest('.dropdown-item').remove();
                        this.notificationCount--;
                        if (this.notificationCount <= 0) this.notificationBadge.style.display = 'none';
                        else this.notificationBadge.textContent = this.notificationCount;
                    }
                } catch (err) {
                    console.error(err);
                }
            });
        }

    }

    filterUsers(searchTerm) {
        const userItems = document.querySelectorAll('.user-item');

        userItems.forEach(item => {
            const userName = item.querySelector('.user-name').textContent.toLowerCase();
            if (userName.includes(searchTerm.toLowerCase())) {
                item.style.display = 'flex';
            } else {
                item.style.display = 'none';
            }
        });
    }

    checkScrollPosition() {
        const messagesContainer = document.getElementById('messagesContainer');
        const autoScrollIndicator = document.getElementById('autoScrollIndicator');

        if (!messagesContainer || !autoScrollIndicator) return;

        const scrollBottom = messagesContainer.scrollHeight - messagesContainer.scrollTop - messagesContainer.clientHeight;
        this.isAutoScrolling = scrollBottom <= 50;

        if (this.isAutoScrolling) {
            autoScrollIndicator.classList.remove('show');
        } else {
            autoScrollIndicator.classList.add('show');
        }
    }

    scrollToBottom(instant = false) {
        const messagesContainer = document.getElementById('messagesContainer');
        if (!messagesContainer) return;

        if (instant) {
            messagesContainer.scrollTop = messagesContainer.scrollHeight;
        } else {
            messagesContainer.scrollTo({
                top: messagesContainer.scrollHeight,
                behavior: 'smooth'
            });
        }

        this.checkScrollPosition();
    }

    appendMessage(msg, isOwn) {
        // Clear welcome message if it exists
        const welcomeMessage = document.querySelector('.welcome-message');
        if (welcomeMessage) {
            welcomeMessage.style.display = 'none';
        }

        const messagesContainer = document.getElementById('messagesContainer');
        if (!messagesContainer) return;

        const messageDate = new Date(msg.sentAt);

        // Format date for grouping
        const today = new Date();
        const yesterday = new Date();
        yesterday.setDate(today.getDate() - 1);

        let dateLabel = '';
        if (messageDate.toDateString() === today.toDateString()) {
            dateLabel = 'Today';
        } else if (messageDate.toDateString() === yesterday.toDateString()) {
            dateLabel = 'Yesterday';
        } else {
            // Format as "23 Sep 2025"
            const options = { day: '2-digit', month: 'short', year: 'numeric' };
            dateLabel = messageDate.toLocaleDateString(undefined, options);
        }

        if (!messagesContainer.querySelector(`.date-separator[data-date="${messageDate.toDateString()}"]`)) {
            const dateDiv = document.createElement('div');
            dateDiv.classList.add('date-separator');
            dateDiv.setAttribute('data-date', messageDate.toDateString());
            dateDiv.textContent = dateLabel;
            messagesContainer.appendChild(dateDiv);
        }

        // Create message element
        const messageDiv = document.createElement('div');
        messageDiv.classList.add('message-bubble', isOwn ? 'own' : 'other');

        const senderName = isOwn ? 'You' : (msg.fromName || this.selectedUsername || 'Unknown');
        const time = messageDate.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        const statusIcon = isOwn ? `<span class="message-status sent" id="status-${msg.id}">✓</span>` : '';
        const senderLabel = isOwn ? '' : `<div class="message-sender">${senderName}</div>`;

        messageDiv.innerHTML = `
                    ${senderLabel}
                    <div class="message-content">${msg.message}</div>
                    <div class="message-time">${time} ${statusIcon}</div>
                `;

        messageDiv.setAttribute('data-message-id', msg.id || Date.now());
        messagesContainer.appendChild(messageDiv);

        // Scroll to bottom if auto-scrolling is enabled
        if (this.isAutoScrolling) {
            this.scrollToBottom();
        }
    }

    setupAutoScrollIndicator() {
        const autoScrollIndicator = document.getElementById('autoScrollIndicator');
        if (autoScrollIndicator) {
            this.checkScrollPosition();
        }
    }

    handleMessageInput() {
        const messageInput = document.getElementById('messageInput');
        if (!messageInput) return;

        // Auto-resize textarea
        messageInput.style.height = 'auto';
        messageInput.style.height = (messageInput.scrollHeight) + 'px';

        // Enable/disable send buttons
        const hasText = messageInput.value.trim().length > 0;
        const sendBtn = document.getElementById('sendBtn');
        const sendOneTimeBtn = document.getElementById('sendOneTimeBtn');

        if (sendBtn) sendBtn.disabled = !hasText;
        if (sendOneTimeBtn) sendOneTimeBtn.disabled = !hasText;

        // Simulate typing indicator
        if (hasText && this.selectedUserId) {
            this.showTypingIndicator(this.username);

            // Clear previous timeout
            if (this.typingTimeout) {
                clearTimeout(this.typingTimeout);
            }

            // Set new timeout to hide typing indicator
            this.typingTimeout = setTimeout(() => {
                this.hideTypingIndicator();
            }, 1000);
        } else {
            this.hideTypingIndicator();
        }
    }
    showTypingIndicator(userName) {
        const indicator = document.getElementById('typingIndicator');
        const typingUser = document.getElementById('typingUser');

        if (typingUser && userName) {
            typingUser.textContent = userName;
        }

        if (indicator) {
            indicator.style.display = 'flex';
        }
    }
    hideTypingIndicator() {
        const indicator = document.getElementById('typingIndicator');
        if (indicator) {
            indicator.style.display = 'none';
        }
    }

    showNotification(from, message) {
        // Browser notification
        if (Notification.permission === "granted") {
            new Notification(`💬 New message from ${from}`, { body: message });
        } else if (Notification.permission !== "denied") {
            Notification.requestPermission().then(p => {
                if (p === "granted") new Notification(`💬 New message from ${from}`, { body: message });
            });
        }
    }

    toggleTheme() {
        this.isDarkTheme = !this.isDarkTheme;
        document.documentElement.setAttribute('data-theme', this.isDarkTheme ? 'dark' : 'light');

        const icon = document.querySelector('#theme-toggle i');
        if (icon) {
            icon.className = this.isDarkTheme ? 'fas fa-sun me-1' : 'fas fa-moon me-1';
        }

        // Save preference
        localStorage.setItem('chatTheme', this.isDarkTheme ? 'dark' : 'light');
    }

    loadSavedTheme() {
        const savedTheme = localStorage.getItem('chatTheme') || 'light';
        this.isDarkTheme = savedTheme === 'dark';
        document.documentElement.setAttribute('data-theme', savedTheme);

        const icon = document.querySelector('#theme-toggle i');
        if (icon) {
            icon.className = this.isDarkTheme ? 'fas fa-sun me-1' : 'fas fa-moon me-1';
        }
    }

    requestNotificationPermission() {
        if ('Notification' in window && Notification.permission === 'default') {
            Notification.requestPermission();
        }
    }

    async registerPush(userId) {
        if (!('serviceWorker' in navigator) || !('PushManager' in window)) return;

        try {

            const reg = await navigator.serviceWorker.register('/sw.js', { scope: '/' });
            let sub = await reg.pushManager.getSubscription();
            if (sub) {
                await sub.unsubscribe();
                console.log('Old subscription unsubscribed');
            }
            const applicationServerKey = this.urlBase64ToUint8Array(this.vapidPublicKey);
            sub = await reg.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey
            });

            const subJson = sub.toJSON();
            // Send subscription to server
            await fetch('/PushSubscription/save', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    userId: this.userId,
                    endpoint: subJson.endpoint,
                    p256dh: subJson.keys.p256dh,
                    auth: subJson.keys.auth
                })
            });

            console.log('Push subscription registered');
        } catch (err) {
            console.error('Push registration error:', err);
        }
    }

    urlBase64ToUint8Array(base64String) {
        const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
        const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
        const rawData = atob(base64);
        return new Uint8Array([...rawData].map(c => c.charCodeAt(0)));
    }

}

document.addEventListener('DOMContentLoaded', function () {
    const vapidPublicKey = 'BCuxfTxtbORt1GqvRvfvt994raDxJOPHLDOeeQFfaOGdhl1mj4zYIymUSUcN8lvSP_Yw2-PhSRXrjg0LTdAIyl4';
    window.chatApp = new ChatApplication(vapidPublicKey);
});

document.querySelectorAll('.user-item').forEach(item => {
    item.addEventListener('click', () => chatApp.selectUser(item));
});
