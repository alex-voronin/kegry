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

// TODO: replace with enum from fcm/types.ts after moving to modular firebase SDK
const MessageType = {
  FCM_MESSAGE: 'FCM_MESSAGE',
  NOTIFICATION_CLICK: 'NOTIFICATION_CLICK',
};

function getMessageId(payload) {
  return payload.data?.messageId || payload.messageId || 'unknown';
}

function getTargetUrl(type, payload) {
  if (type === 'participationApproved') {
    const eventId = payload?.data?.eventId;
    if (eventId) return `/meetups/${eventId}`;
  }
  if (type === 'userRequestedApprove') {
    const eventId = payload?.data?.eventId;
    if (eventId) return `/meetups/${eventId}/approvals`;
  }
  if (type === 'userInvited') return '/notifications';
  if (
    type === 'chatMessageReceived' ||
    type === 'meetingArranged' ||
    type === 'showEndMeetingSuggestion'
  )
    return '/chat';
  if (type === 'referrerRewardReceived')
    return '/friends-and-foes?tab=imaginary';
  return '/';
}

messaging.onBackgroundMessage(async (payload) => {
  const messageId = getMessageId(payload);
  const type = payload?.data?.type || '';

  const { data } = payload;

  // Post to clients
  try {
    const clients = await self.clients.matchAll({
      type: 'window',
      includeUncontrolled: true,
    });

    for (const client of clients) {
      client.postMessage({
        type: MessageType.FCM_MESSAGE,
        messageId,
        payload,
      });
    }
  } catch (err) {
    console.error('[SW] matchAll error:', err);
  }

  try {
    await self.registration.showNotification(
      // TODO: Use BRAND_NAME constant here after moving to modular firebase SDK
      data.title ?? 'Kegry',
      {
        body: data.body ?? '',
        icon: data.icon ?? '/assets/notifications/notification-logo.webp',
        badge: '/assets/notifications/notification-badge.png',
        tag: `${type}-${messageId}`, // tag for deduplication
        data: payload,
      },
    );
  } catch (err) {
    console.error('[SW] showNotification error:', err);
  }
});

self.addEventListener('notificationclick', (event) => {
  event.notification.close();

  const payload = event.notification.data;
  const type = payload?.data?.type;
  const messageId = payload?.data?.messageId || payload?.messageId;

  if (!messageId) return;

  const targetUrl = getTargetUrl(type, payload);

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
              type: MessageType.NOTIFICATION_CLICK,
              data: payload,
            });
            return;
          }
        }

        // No window found, open target URL based on notification type
        await self.clients.openWindow(targetUrl);
      } catch (err) {
        console.error('[SW] notificationclick error:', err);
      }
    })(),
  );
});
