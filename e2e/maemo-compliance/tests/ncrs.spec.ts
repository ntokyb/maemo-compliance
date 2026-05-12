import { test, expect } from '@playwright/test';
import { loginWithCredentials } from './helpers/auth';

test.describe('@auth NCR management', () => {
  test.beforeEach(async ({ page }) => {
    test.skip(!process.env.MAEMO_TEST_EMAIL, 'MAEMO_TEST_EMAIL not set (run locally with real Azure AD test user)');
    await loginWithCredentials(page, process.env.MAEMO_TEST_EMAIL!, process.env.MAEMO_TEST_PASSWORD ?? '');
  });

  test('can navigate to NCRs', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByRole('link', { name: 'NCRs', exact: true }).click();
    await expect(page).toHaveURL(/.*ncr.*/);
  });

  test('can create a new NCR', async ({ page }) => {
    await page.goto('/ncrs');
    await page.locator('button:has-text("New"), button:has-text("Add")').first().click();
    await page.fill('#title', 'E2E Test NCR');
    await page.fill('#description', 'Test nonconformance for E2E');
    await page.locator('button:has-text("Save"), button:has-text("Submit"), button[type="submit"]:has-text("Create")').first().click();
    await expect(page.locator('.snackbar, .toast, [role="alert"]')).toContainText(/success|saved|created/i, {
      timeout: 8000,
    });
  });
});
