// Production: used when building with --configuration production (see angular.json fileReplacements).
export const environment = {
  production: true,
  apiBaseUrl: 'https://api.maemo-compliance.co.za',
  azureAd: {
    clientId: 'e90f0190-597f-448b-b1d7-491e32371d43',
    authority: 'https://login.microsoftonline.com/e3adb32b-0cea-4a22-b04b-e9e25db1ccbb',
    redirectUri: 'https://maemo-compliance.co.za',
    postLogoutRedirectUri: 'https://maemo-compliance.co.za',
    apiScope: 'api://94011671-063e-4bd5-a1c9-fa8b5b07cd13/.default'
  }
};
