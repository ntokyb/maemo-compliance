import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

bootstrapApplication(App, appConfig)
  .catch((err) => {
    console.error('Error bootstrapping application:', err);
    // Display error in console and on page if possible
    const errorDiv = document.createElement('div');
    errorDiv.style.cssText = 'padding: 20px; background: #fee; border: 2px solid #f00; margin: 20px; font-family: monospace;';
    errorDiv.innerHTML = `
      <h2>Application Bootstrap Error</h2>
      <p>Check the browser console for details.</p>
      <pre>${err.message || err}</pre>
      <p><strong>Common issues:</strong></p>
      <ul>
        <li>MSAL configuration: Check environment.ts for valid Azure AD client ID and tenant ID</li>
        <li>API URL: Ensure the backend API is running</li>
        <li>Network: Check CORS settings</li>
      </ul>
    `;
    document.body.appendChild(errorDiv);
  });
