import { mkdirSync } from 'node:fs';
import { expect, test as setup } from '@playwright/test';
import { password, users } from './support';

mkdirSync('e2e/.auth', { recursive: true });

for (const role of ['manager', 'clerk'] as const) {
  setup(`sign in as ${role}`, async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Email').fill(users[role]);
    await page.getByLabel('Password').fill(password);
    await page.getByRole('button', { name: 'Sign in' }).click();

    await expect(page.getByRole('navigation', { name: 'Main' })).toBeVisible();
    await page.context().storageState({ path: `e2e/.auth/${role}.json` });
  });
}