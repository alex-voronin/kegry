// 1. Import FCM SW logic
importScripts('/firebase-messaging-sw.js');

// 2. Import auto-generated static assets
importScripts('/pwa-assets.js');

const CACHE_NAME = 'kegrycorn-static-v1';

// STATIC_ASSETS is defined in pwa-assets.js
const ASSETS_TO_CACHE =
  typeof STATIC_ASSETS !== 'undefined'
    ? STATIC_ASSETS
    : [
        '/icons/icon-192x192.png',
        '/icons/icon-512x512.png',
        '/manifest.json',
        '/icons/favicon.ico',
      ];

self.addEventListener('install', (event) => {
  console.log('[SW] Installing...');
  event.waitUntil(
    caches.open(CACHE_NAME).then((cache) => {
      return cache.addAll(ASSETS_TO_CACHE);
    }),
  );
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  console.log('[SW] Activating...');
  event.waitUntil(
    caches.keys().then((cacheNames) => {
      return Promise.all(
        cacheNames.map((cacheName) => {
          if (cacheName !== CACHE_NAME) {
            console.log('[SW] Deleting old cache:', cacheName);
            return caches.delete(cacheName);
          }
        }),
      );
    }),
  );
  self.clients.claim();
});

self.addEventListener('fetch', (event) => {
  // Static assets: Cache First
  if (
    event.request.destination === 'image' ||
    event.request.destination === 'font' ||
    event.request.url.includes('/icons/') ||
    ASSETS_TO_CACHE.some((url) => event.request.url.endsWith(url))
  ) {
    event.respondWith(
      caches
        .match(event.request)
        .then((response) => {
          if (response) {
            return response;
          }
          return fetch(event.request).then((networkResponse) => {
            return networkResponse;
          });
        })
        .catch(() => {
          return new Response('Offline', {
            status: 503,
            statusText: 'Service Unavailable',
          });
        }),
    );
    return;
  }

  // CSS & JS: Stale-While-Revalidate
  if (event.request.url.endsWith('.js') || event.request.url.endsWith('.css')) {
    event.respondWith(
      caches.open(CACHE_NAME).then((cache) => {
        return cache.match(event.request).then((cachedResponse) => {
          const fetchPromise = fetch(event.request)
            .then((networkResponse) => {
              if (networkResponse.ok) {
                cache.put(event.request, networkResponse.clone());
              }
              return networkResponse;
            })
            .catch((err) => {
              console.warn('[SW] Network failed for:', url, err);
              return new Response('Offline content unavailable', {
                status: 503,
                statusText: 'Service Unavailable',
              });
            });
          return cachedResponse || fetchPromise;
        });
      }),
    );
    return;
  }
});
