// sw.js - Service Worker
self.addEventListener('push', function (event) {
    console.log('[Service Worker] Push Received.');
    let data = {};
    if (event.data) {
        data = event.data.json();
    }

    const title = data.title || 'New Message';
    const options = {
        body: data.body || 'You have a new notification',
        icon: '/images/icon-192.png', // optional
        badge: '/images/badge-72.png', // optional
        data: data.url || '/'
    };

    event.waitUntil(self.registration.showNotification(title, options));
});

self.addEventListener('notificationclick', function (event) {
    event.notification.close();
    const url = event.notification.data || '/';
    event.waitUntil(clients.openWindow(url));
});
