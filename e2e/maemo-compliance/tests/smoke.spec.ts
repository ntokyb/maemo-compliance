import { test, expect } from '@playwright/test';

test('@smoke health check — API responds', async ({ request }) => {
  test.skip(!process.env.MAEMO_API_URL, 'Set MAEMO_API_URL to run API health check (no server → skipped)');

  const base = process.env.MAEMO_API_URL!.replace(/\/$/, '');
  const resp = await request.get(`${base}/health/live`);
  expect(resp.status()).toBe(200);
  const body = await resp.json();
  expect(body.status).toBe('Healthy');
});

test('@smoke public signup page loads', async ({ page }) => {
  await page.goto('/signup');
  await expect(page.locator('h1,h2')).toContainText(/create|sign up|get started|workspace/i);
  await expect(page.locator('[formcontrolname="companyName"]')).toBeVisible();
});

test('@smoke login page loads and shows Microsoft login button', async ({ page }) => {
  await page.goto('/login');
  const loginBtn = page.getByTestId('login-button');
  await expect(loginBtn).toBeVisible();
});

test('@smoke accept invite page loads with valid token', async ({ page }) => {
  const token = process.env.MAEMO_TEST_INVITE_TOKEN;
  test.skip(!token, 'MAEMO_TEST_INVITE_TOKEN not set');

  await page.goto(`/accept-invite?token=${token}`);
  await expect(page.locator('h1,h2')).toContainText(/accept|invite/i);
});
