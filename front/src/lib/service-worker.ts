export async function registerServiceWorker(): Promise<ServiceWorkerRegistration> {
  if (!('serviceWorker' in navigator)) {
    throw new Error('Service Worker not supported');
  }

  const reg = await navigator.serviceWorker.register('/sw.js');
  await reg.update();
  await navigator.serviceWorker.ready;
  return reg;
}

export async function reRegisterServiceWorker(): Promise<void> {
  if (typeof window === 'undefined' || !('serviceWorker' in navigator)) return;

  const regs = await navigator.serviceWorker.getRegistrations();
  await Promise.all(regs.map((r) => r.unregister()));
  await navigator.serviceWorker.register('/sw.js');
  await navigator.serviceWorker.ready;
  console.log('[SW] Re-registered after login');
}
