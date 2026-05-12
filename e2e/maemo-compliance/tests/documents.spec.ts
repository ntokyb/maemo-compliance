import { test, expect } from '@playwright/test';
import { loginWithCredentials } from './helpers/auth';

test.describe('@auth Document management', () => {
  test.beforeEach(async ({ page }) => {
    test.skip(!process.env.MAEMO_TEST_EMAIL, 'MAEMO_TEST_EMAIL not set (run locally with real Azure AD test user)');
    await loginWithCredentials(page, process.env.MAEMO_TEST_EMAIL!, process.env.MAEMO_TEST_PASSWORD ?? '');
  });

  test('can navigate to documents', async ({ page }) => {
    await page.goto('/dashboard');
    await page.getByRole('link', { name: 'Documents', exact: true }).click();
    await expect(page).toHaveURL(/.*documents.*/);
    await expect(page.locator('h1,h2')).toContainText(/document/i);
  });

  test('can create a new document', async ({ page }) => {
    await page.goto('/documents');
    await page.locator('button:has-text("New"), button:has-text("Add"), button:has-text("Create Document")').first().click();
    await page.fill('#title', 'E2E Test Document');
    await page.locator('button:has-text("Save"), button:has-text("Submit"), button[type="submit"]:has-text("Create")').first().click();
    await expect(page.locator('.snackbar, .toast, [role="alert"]')).toContainText(/success|saved|created/i, {
      timeout: 8000,
    });
    await expect(page.locator('table, mat-table')).toContainText('E2E Test Document');
  });

  test('document shows Draft status after creation', async ({ page }) => {
    await page.goto('/documents');
    await expect(page.locator('table, mat-table')).toContainText(/draft/i);
  });
});
