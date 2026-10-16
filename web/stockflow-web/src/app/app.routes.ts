import { Routes } from '@angular/router';

import { authGuard, roleGuard } from './core/auth.guards';
import { LoginComponent } from './features/auth/login.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { OrderListComponent } from './features/orders/order-list.component';
import { OrderNewComponent } from './features/orders/order-new.component';
import { ProductFormComponent } from './features/products/product-form.component';
import { ProductListComponent } from './features/products/product-list.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  {
    path: '',
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'products', component: ProductListComponent },
      { path: 'products/new', component: ProductFormComponent, canActivate: [roleGuard('Admin', 'Manager')] },
      { path: 'products/:id', component: ProductFormComponent, canActivate: [roleGuard('Admin', 'Manager')] },
      { path: 'orders', component: OrderListComponent },
      { path: 'orders/new', component: OrderNewComponent },
    ],
  },
  { path: '**', redirectTo: '' },
];