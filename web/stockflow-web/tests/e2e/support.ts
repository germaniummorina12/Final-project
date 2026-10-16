import { APIRequestContext, expect, request } from '@playwright/test';

export const apiUrl = 'http://localhost:5182/api/v1';
export const password = 'StockFlow#2026';
export const users = {
  manager: 'manager@stockflow.local',
  clerk: 'clerk@stockflow.local',
} as const;

export type Role = keyof typeof users;

/** An HTTP client that is already signed in as the given role. */
export async function apiAs(role: Role): Promise<APIRequestContext> {
  const anonymous = await request.newContext();
  const response = await anonymous.post(`${apiUrl}/auth/login`, {
    data: { email: users[role], password },
  });
  expect(response.ok()).toBeTruthy();
  const { token } = await response.json();
  await anonymous.dispose();

  return request.newContext({ extraHTTPHeaders: { Authorization: `Bearer ${token}` } });
}

export function uniqueSku(): string {
  const random = Math.random().toString(36).slice(2, 6);
  return `E2E-${Date.now().toString(36)}-${random}`.toUpperCase();
}

export interface TestProduct {
  id: number;
  sku: string;
  name: string;
}

/** Creates a product with a known stock, through the API. Every test makes its own. */
export async function createProduct(api: APIRequestContext, initialStock: number): Promise<TestProduct> {
  const categories = await (await api.get(`${apiUrl}/categories`)).json();
  const sku = uniqueSku();
  const name = `Test item ${sku}`;

  const response = await api.post(`${apiUrl}/products`, {
    data: {
      sku,
      name,
      categoryId: categories[0].id,
      unitPrice: 10,
      initialStock,
      reorderLevel: 1,
    },
  });
  expect(response.status()).toBe(201);

  const created = await response.json();
  return { id: created.id, sku: created.sku, name: created.name };
}