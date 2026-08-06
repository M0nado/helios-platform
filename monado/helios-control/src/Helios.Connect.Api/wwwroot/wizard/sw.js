const cachePrefix = 'helios-control-wizard-';
const cacheName = 'helios-control-wizard-v0.4.2';
const shell = ['/wizard/index.html', '/wizard/wizard.css', '/wizard/wizard.js', '/wizard/icon.svg', '/wizard/icon-192.png', '/wizard/icon-512.png', '/wizard/manifest.webmanifest'];
self.addEventListener('install', event => event.waitUntil(
  caches.open(cacheName).then(cache => cache.addAll(shell)).then(() => self.skipWaiting())
));
self.addEventListener('activate', event => event.waitUntil(
  caches.keys()
    .then(keys => Promise.all(keys.filter(key => key.startsWith(cachePrefix) && key !== cacheName).map(key => caches.delete(key))))
    .then(() => self.clients.claim())
));
self.addEventListener('fetch', event => {
  if (event.request.method !== 'GET' || !new URL(event.request.url).pathname.startsWith('/wizard/')) return;
  event.respondWith((async () => {
    try {
      const response = await fetch(event.request);
      if (response.ok && response.type === 'basic') {
        const cache = await caches.open(cacheName);
        await cache.put(event.request, response.clone());
      }
      return response;
    } catch {
      const cache = await caches.open(cacheName);
      return await cache.match(event.request) || Response.error();
    }
  })());
});
