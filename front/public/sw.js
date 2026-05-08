// 1. Import FCM SW logic
importScripts('/firebase-messaging-sw.js');

// 2. Import auto-generated static assets
importScripts('/pwa-assets.js');

self.addEventListener('install', (event) => {
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  self.clients.claim();
});
