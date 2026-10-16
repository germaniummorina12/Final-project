import { expect, test } from '@playwright/test';
import { users } from './support';

// These tests start as an anonymous visitor: no saved session.
test.use({ storageState: { cookies: [], origins: [] } });

test('an anonymous visitor is sent to the login page', async ({ page }) => {
  await page.goto('/products');

  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByRole('heading', { name: 'StockFlow' })).toBeVisible();
});

test('the Sign in button is disabled until the form is valid', async ({ page }) => {
  await page.goto('/login');
  const signIn = page.getByRole('button', { name: 'Sign in' });

  await expect(signIn).toBeDisabled();
  await page.getByLabel('Email').fill(users.manager);
  await expect(signIn).toBeDisabled();
  await page.getByLabel('Password').fill('anything');
  await expect(signIn).toBeEnabled();
});

test('a wrong password shows the message and stays on the login page', async ({ page }) => {
  await page.goto('/login');
  await page.getByLabel('Email').fill(users.manager);
  await page.getByLabel('Password').fill('not-the-password');
  await page.getByRole('button', { name: 'Sign in' }).click();

  await expect(page.getByRole('alert')).toContainText('Invalid email or password');
  await expect(page).toHaveURL(/\/login$/);
});