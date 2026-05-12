// Default / production build: same-origin API (Docker nginx proxies /api, /engine, /admin, /health).
// `ng serve` uses proxy.conf.json to forward those paths to the API.
export const environment = {
  production: true,
  apiBaseUrl: '',
  azureAd: {
    clientId: '{your-client-id}',
    authority: 'https://login.microsoftonline.com/{tenantId}',
    redirectUri: typeof window !== 'undefined' ? window.location.origin + '/' : 'http://localhost/',
    apiScope: 'api://maemo-api-client-id/.default'
  }
};
