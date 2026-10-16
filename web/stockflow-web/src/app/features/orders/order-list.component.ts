import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { Order, PagedResult } from '../../core/api.models';
import { AuthService } from '../../core/auth.service';
import { messageFrom } from '../../core/http-errors';
import { OrdersApiService } from './orders-api.service';

@Component({
  selector: 'app-order-list',
  imports: [ReactiveFormsModule, RouterLink, CurrencyPipe, DatePipe],
  template: `
    <section class="page">
      <div class="toolbar">
        <h1>Orders</h1>
        <a class="btn primary" routerLink="/orders/new">New order</a>
      </div>

      <div class="filters">
        <label for="status">Status</label>
        <select id="status" [formControl]="status">
          <option value="">All</option>
          <option value="Placed">Placed</option>
          <option value="Cancelled">Cancelled</option>
        </select>
      </div>

      @if (error(); as message) {
        <p class="error" role="alert">{{ message }}</p>
      }

      <table>
        <thead>
          <tr><th>Number</th><th>Customer</th><th>Placed</th><th>By</th><th>Status</th><th class="num">Total</th><th></th></tr>
        </thead>
        <tbody>
          @for (o of result()?.items ?? []; track o.id) {
            <tr [class.inactive]="o.status === 'Cancelled'">
              <td>{{ o.number }}</td>
              <td>{{ o.customerName }}</td>
              <td>{{ o.placedAtUtc | date: 'medium' }}</td>
              <td>{{ o.placedBy }}</td>
              <td>{{ o.status }}</td>
              <td class="num">{{ o.total | currency }}</td>
              <td class="actions">
                @if (canCancel() && o.status === 'Placed') {
                  <button type="button" (click)="cancel(o)">Cancel order</button>
                }
              </td>
            </tr>
          } @empty {
            <tr><td colspan="7" class="empty">No orders yet.</td></tr>
          }
        </tbody>
      </table>

      <nav class="pager" aria-label="Pages">
        <button type="button" (click)="go(page() - 1)" [disabled]="page() <= 1">Previous</button>
        <span>Page {{ page() }} of {{ result()?.totalPages || 1 }}</span>
        <button type="button" (click)="go(page() + 1)" [disabled]="page() >= (result()?.totalPages || 1)">Next</button>
      </nav>
    </section>
  `,
})
export class OrderListComponent {
  private readonly api = inject(OrdersApiService);
  private readonly auth = inject(AuthService);

  protected readonly status = new FormControl('', { nonNullable: true });
  protected readonly result = signal<PagedResult<Order> | null>(null);
  protected readonly page = signal(1);
  protected readonly error = signal<string | null>(null);
  protected readonly canCancel = computed(() => this.auth.hasAnyRole('Admin', 'Manager'));

  constructor() {
    this.status.valueChanges.subscribe(() => this.go(1));
    this.load();
  }

  protected go(page: number): void {
    this.page.set(page);
    this.load();
  }

  protected cancel(order: Order): void {
    if (!confirm(`Cancel order ${order.number}? The stock will be returned.`)) {
      return;
    }
    this.api.cancel(order.id).subscribe({
      next: () => this.load(),
      error: (err: unknown) => this.error.set(messageFrom(err)),
    });
  }

  private load(): void {
    this.error.set(null);
    this.api.search(this.status.value || null, this.page(), 10).subscribe({
      next: (r) => this.result.set(r),
      error: (err: unknown) => this.error.set(messageFrom(err)),
    });
  }
}