# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: smoke.spec.ts >> @smoke public signup page loads
- Location: tests\smoke.spec.ts:13:5

# Error details

```
Error: page.goto: net::ERR_CONNECTION_REFUSED at http://localhost:4200/signup
Call log:
  - navigating to "http://localhost:4200/signup", waiting until "load"

```

# Test source

```ts
  1  | import { test, expect } from '@playwright/test';
  2  | 
  3  | test('@smoke health check — API responds', async ({ request }) => {
  4  |   test.skip(!process.env.MAEMO_API_URL, 'Set MAEMO_API_URL to run API health check (no server → skipped)');
  5  | 
  6  |   const base = process.env.MAEMO_API_URL!.replace(/\/$/, '');
  7  |   const resp = await request.get(`${base}/health/live`);
  8  |   expect(resp.status()).toBe(200);
  9  |   const body = await resp.json();
  10 |   expect(body.status).toBe('Healthy');
  11 | });
  12 | 
  13 | test('@smoke public signup page loads', async ({ page }) => {
> 14 |   await page.goto('/signup');
     |              ^ Error: page.goto: net::ERR_CONNECTION_REFUSED at http://localhost:4200/signup
  15 |   await expect(page.locator('h1,h2')).toContainText(/create|sign up|get started|workspace/i);
  16 |   await expect(page.locator('[formcontrolname="companyName"]')).toBeVisible();
  17 | });
  18 | 
  19 | test('@smoke login page loads and shows Microsoft login button', async ({ page }) => {
  20 |   await page.goto('/login');
  21 |   const loginBtn = page.getByTestId('login-button');
  22 |   await expect(loginBtn).toBeVisible();
  23 | });
  24 | 
  25 | test('@smoke accept invite page loads with valid token', async ({ page }) => {
  26 |   const token = process.env.MAEMO_TEST_INVITE_TOKEN;
  27 |   test.skip(!token, 'MAEMO_TEST_INVITE_TOKEN not set');
  28 | 
  29 |   await page.goto(`/accept-invite?token=${token}`);
  30 |   await expect(page.locator('h1,h2')).toContainText(/accept|invite/i);
  31 | });
  32 | 
```