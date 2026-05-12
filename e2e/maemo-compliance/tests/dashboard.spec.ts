import { test, expect } from '@playwright/test';
import { loginWithCredentials } from './helpers/auth';

test.describe('@auth Dashboard', () => {
  test.beforeEach(async ({ page }) => {
    test.skip(!process.env.MAEMO_TEST_EMAIL, 'MAEMO_TEST_EMAIL not set (run locally with real Azure AD test user)');
    await loginWithCredentials(page, process.env.MAEMO_TEST_EMAIL!, process.env.MAEMO_TEST_PASSWORD ?? '');
  });

  test('dashboard shows summary counts', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.locator('[data-testid*="count"], mat-card')).toBeVisible();
  });

  test('sidebar navigation links are all present', async ({ page }) => {
    await page.goto('/dashboard');
    const links = ['documents', 'ncr', 'risk', 'audit'];
    for (const link of links) {
      await expect(page.locator(`[href*="${link}"]`).first()).toBeVisible();
    }
  });
});
