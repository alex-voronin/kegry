export async function registerServiceWorker(): Promise<ServiceWorkerRegistration> {
  if (!('serviceWorker' in navigator)) {
    throw new Error('Service Worker not supported');
  }

  const reg = await navigator.serviceWorker.register('/sw.js');
  await reg.update();
  await navigator.serviceWorker.ready;
  return reg;
}
