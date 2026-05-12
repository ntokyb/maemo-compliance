// Local `ng serve`: relative API URLs + proxy.conf.json → API (default http://localhost:8080).
export const environment = {
  production: false,
  apiBaseUrl: '',
  azureAd: {
    clientId: '{your-client-id}',
    authority: 'https://login.microsoftonline.com/{tenantId}',
    redirectUri: typeof window !== 'undefined' ? window.location.origin + '/' : 'http://localhost:4200/',
    apiScope: 'api://maemo-api-client-id/.default'
  }
};
