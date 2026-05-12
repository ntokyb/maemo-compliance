import { APP_INITIALIZER, ApplicationConfig, provideBrowserGlobalErrorListeners, provideZoneChangeDetection, isDevMode } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors, HTTP_INTERCEPTORS } from '@angular/common/http';
import { provideServiceWorker } from '@angular/service-worker';
import { MSAL_INSTANCE, MSAL_INTERCEPTOR_CONFIG, MsalInterceptor, MsalInterceptorConfiguration, MsalService, MSAL_GUARD_CONFIG, MsalGuardConfiguration, MsalGuard } from '@azure/msal-angular';
import { PublicClientApplication, InteractionType, IPublicClientApplication } from '@azure/msal-browser';
import { environment } from '../environments/environment';

import { routes } from './app.routes';
import { authInterceptor } from './interceptors/auth.interceptor';
import { errorInterceptor } from './interceptors/error.interceptor';

export function MSALInstanceFactory(): IPublicClientApplication {
  // Check if MSAL configuration is valid
  const clientId = environment.azureAd.clientId;
  const authority = environment.azureAd.authority;
  
  if (!clientId || clientId.includes('{') || !authority || authority.includes('{')) {
    console.warn('MSAL configuration appears incomplete. Using placeholder values may cause authentication issues.');
    console.warn('Please update environment.ts with valid Azure AD configuration.');
  }

  try {
    return new PublicClientApplication({
      auth: {
        clientId: clientId,
        authority: authority,
        redirectUri: environment.azureAd.redirectUri
      },
      cache: {
        cacheLocation: 'localStorage',
        storeAuthStateInCookie: false
      }
    });
  } catch (error) {
    console.error('Failed to create MSAL instance:', error);
    throw error;
  }
}

export function MSALInterceptorConfigFactory(): MsalInterceptorConfiguration {
  const protectedResourceMap = new Map<string, Array<string>>();
  const origin = typeof window !== 'undefined' ? window.location.origin : 'http://localhost';
  const scope = environment.azureAd.apiScope;
  protectedResourceMap.set(`${origin}/api`, [scope]);
  protectedResourceMap.set(`${origin}/engine`, [scope]);
  protectedResourceMap.set(`${origin}/admin`, [scope]);

  return {
    interactionType: InteractionType.Popup,
    protectedResourceMap
  };
}

export function MSALGuardConfigFactory(): MsalGuardConfiguration {
  return {
    interactionType: InteractionType.Popup,
    authRequest: {
      scopes: [environment.azureAd.apiScope]
    }
  };
}

export function msalRedirectInitializer(msalService: MsalService) {
  return () =>
    msalService.instance.initialize().then(() =>
      msalService.instance.handleRedirectPromise().then(() => {
        const accounts = msalService.instance.getAllAccounts();
        if (accounts.length > 0) {
          msalService.instance.setActiveAccount(accounts[0]);
        }
      })
    );
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),
    provideServiceWorker('ngsw-worker.js', {
      enabled: !isDevMode(),
      registrationStrategy: 'registerWhenStable:30000'
    }),
    {
      provide: MSAL_INSTANCE,
      useFactory: MSALInstanceFactory
    },
    {
      provide: MSAL_INTERCEPTOR_CONFIG,
      useFactory: MSALInterceptorConfigFactory
    },
    {
      provide: MSAL_GUARD_CONFIG,
      useFactory: MSALGuardConfigFactory
    },
    MsalService,
    MsalGuard,
    {
      provide: APP_INITIALIZER,
      multi: true,
      deps: [MsalService],
      useFactory: msalRedirectInitializer
    }
  ]
};
