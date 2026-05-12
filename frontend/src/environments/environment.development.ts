// Local `ng serve` (--configuration development): proxy.conf.json forwards /api, /admin, etc. to http://localhost:8080.
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:8080',
  azureAd: {
    clientId: 'e90f0190-597f-448b-b1d7-491e32371d43',
    authority: 'https://login.microsoftonline.com/e3adb32b-0cea-4a22-b04b-e9e25db1ccbb',
    redirectUri: 'http://localhost:4200',
    postLogoutRedirectUri: 'http://localhost:4200',
    apiScope: 'api://94011671-063e-4bd5-a1c9-fa8b5b07cd13/.default'
  }
};
