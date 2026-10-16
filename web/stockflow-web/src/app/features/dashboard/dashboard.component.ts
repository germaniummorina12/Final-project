import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';

import { Product, StockChanged } from '../../core/api.models';
import { AuthService } from '../../core/auth.service';
import { messageFrom } from '../../core/http-errors';
import { StockHubService } from '../../core/stock-hub.service';
import { ProductsApiService } from '../products/products-api.service';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink],
  template: `
    <section class="page">
      <div class="toolbar">
        <h1>Dashboard</h1>
        <span class="hint">Live updates: {{ hub.state() }}</span>
      </div>

      <p>Signed in as {{ auth.userName() }}.</p>

      @if (error(); as message) {
        <p class="error" role="alert">{{ message }}</p>
      }

      <h2>Low stock ({{ lowStock().length }})</h2>
      <table>
        <thead><tr><th>SKU</th><th>Name</th><th class="num">On hand</th><th class="num">Reorder at</th></tr></thead>
        <tbody>
          @for (p of lowStock(); track p.id) {
            <tr class="low">
              <td>{{ p.sku }}</td><td>{{ p.name }}</td>
              <td class="num">{{ p.stockOnHand }}</td><td class="num">{{ p.reorderLevel }}</td>
            </tr>
          } @empty {
            <tr><td colspan="4" class="empty">Everything is above its reorder level.</td></tr>
          }
        </tbody>
      </table>

      <h2>Recent stock changes</h2>
      <ul>
        @for (e of events(); track $index) {
          <li>{{ e.sku }} is now {{ e.stockOnHand }} @if (e.isLowStock) { (low) }</li>
        } @empty {
          <li class="hint">Nothing yet. Place an order from another browser window.</li>
        }
      </ul>

      <p><a routerLink="/products">Go to products</a></p>
    </section>
  `,
})
export class DashboardComponent {
  private readonly products = inject(ProductsApiService);
  protected readonly auth = inject(AuthService);
  protected readonly hub = inject(StockHubService);

  protected readonly lowStock = signal<Product[]>([]);
  protected readonly events = signal<StockChanged[]>([]);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.reload();

    this.hub.stockChanged$.pipe(takeUntilDestroyed()).subscribe((change) => {
      this.events.update((list) => [change, ...list].slice(0, 10));
      this.reload();                    // simplest correct approach: fetch the list again
    });
  }

  private reload(): void {
    this.products.lowStock().subscribe({
      next: (list) => this.lowStock.set(list),
      error: (err: unknown) => this.error.set(messageFrom(err)),
    });
  }
}