// Firebase Messaging Service Worker

importScripts('/firebase-config.js');

importScripts(
  'https://www.gstatic.com/firebasejs/12.8.0/firebase-app-compat.js',
);
importScripts(
  'https://www.gstatic.com/firebasejs/12.8.0/firebase-messaging-compat.js',
);

firebase.initializeApp(self.firebaseConfig);
const messaging = firebase.messaging();

function getMessageId(payload) {
  return payload.messageId || payload.data?.messageId || null;
}

function getTargetUrl(userAction) {
  if (userAction === 'openNotificationsScreen') return '/notifications';
  if (userAction === 'openChatScreen') return '/chat';
  return '/';
}

messaging.onBackgroundMessage(async (payload) => {
  const messageId = getMessageId(payload);
  if (!messageId) return;

  const { notification, data } = payload;

  // Post to clients
  try {
    const clients = await self.clients.matchAll({
      type: 'window',
      includeUncontrolled: true,
    });

    for (const client of clients) {
      client.postMessage({
        type: 'FCM_MESSAGE',
        messageId,
        data,
        notification,
      });
    }
  } catch (err) {
    console.error('[SW] matchAll error:', err);
  }

  // Show notification for non-silent pushes
  if (!notification) return;

  try {
    await self.registration.showNotification(
      notification.title ?? 'Hoppyfy.me',
      {
        body: notification.body ?? '',
        icon: notification.icon ?? '/assets/logo.png',
        tag: `hoppyfy-${messageId}`,
        data: { ...data, messageId },
      },
    );
  } catch (err) {
    console.error('[SW] showNotification error:', err);
  }
});

self.addEventListener('notificationclick', (event) => {
  event.notification.close();

  const data = event.notification.data || {};
  const { userAction, messageId } = data;

  if (!messageId) return;

  const targetUrl = getTargetUrl(userAction);

  event.waitUntil(
    (async () => {
      try {
        const clients = await self.clients.matchAll({
          type: 'window',
          includeUncontrolled: true,
        });

        for (const client of clients) {
          if ('focus' in client) {
            await client.focus();
            client.postMessage({
              type: 'NOTIFICATION_CLICK',
              messageId,
              userAction,
              data,
            });
            return;
          }
        }

        // No window found, open target URL based on userAction
        await self.clients.openWindow(targetUrl);
      } catch (err) {
        console.error('[SW] notificationclick error:', err);
      }
    })(),
  );
});
