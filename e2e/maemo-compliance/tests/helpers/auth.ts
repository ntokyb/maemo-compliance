import type { Page } from '@playwright/test';

/**
 * Maemo uses Azure AD (MSAL). For E2E tests, the app must have a test user configured in Azure AD.
 * This helper fills in the Microsoft login form after MSAL redirects.
 */
async function loginWithCredentials(page: Page, email: string, password: string) {
  await page.goto('/login');
  // Click the login button that triggers MSAL redirect
  await page.click('[data-testid="login-button"]');
  // Handle Microsoft login page
  await page.waitForURL('**/login.microsoftonline.com/**');
  await page.fill('[name="loginfmt"]', email);
  await page.click('[type="submit"]');
  await page.waitForSelector('[name="passwd"]');
  await page.fill('[name="passwd"]', password);
  await page.click('[type="submit"]');
  // Handle "Stay signed in" prompt if it appears
  await page.waitForURL('**/maemo**', { timeout: 15_000 }).catch(() => {});
  if (page.url().includes('kmsi')) {
    await page.click('[type="submit"]');
  }
  await page.waitForURL('**/dashboard**');
}

export { loginWithCredentials };
