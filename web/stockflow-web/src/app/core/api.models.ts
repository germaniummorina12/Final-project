export interface LoginResponse {
  token: string;
  expiresAtUtc: string;
  userName: string;
  roles: string[];
}

export interface Category {
  id: number;
  name: string;
}

export interface Customer {
  id: number;
  name: string;
  email: string;
}

export interface Product {
  id: number;
  sku: string;
  name: string;
  description: string | null;
  categoryId: number;
  categoryName: string;
  unitPrice: number;
  stockOnHand: number;
  reorderLevel: number;
  isLowStock: boolean;
  isActive: boolean;
  concurrencyStamp: string;
}

export interface CreateProduct {
  sku: string;
  name: string;
  description: string | null;
  categoryId: number;
  unitPrice: number;
  initialStock: number;
  reorderLevel: number;
}

export interface UpdateProduct {
  name: string;
  description: string | null;
  categoryId: number;
  unitPrice: number;
  reorderLevel: number;
  concurrencyStamp: string;
}

export interface AdjustStock {
  delta: number;
  reason: 'Received' | 'Adjustment';
  note: string | null;
}

export interface ProductQuery {
  page: number;
  pageSize: number;
  search?: string;
  categoryId?: number | null;
  lowStockOnly?: boolean;
  sort?: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface OrderLine {
  productId: number;
  productSku: string;
  productName: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface Order {
  id: number;
  number: string;
  customerId: number;
  customerName: string;
  status: 'Placed' | 'Cancelled';
  placedAtUtc: string;
  cancelledAtUtc: string | null;
  placedBy: string;
  total: number;
  lines: OrderLine[];
}

export interface PlaceOrder {
  customerId: number;
  lines: { productId: number; quantity: number }[];
}

export interface StockChanged {
  productId: number;
  sku: string;
  stockOnHand: number;
  isLowStock: boolean;
}