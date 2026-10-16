import { CurrencyPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, NonNullableFormBuilder, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject, catchError, debounceTime, distinctUntilChanged, merge, of, switchMap, tap } from 'rxjs';

import { Category, PagedResult, Product } from '../../core/api.models';
import { AuthService } from '../../core/auth.service';
import { messageFrom } from '../../core/http-errors';
import { StockHubService } from '../../core/stock-hub.service';
import { CatalogApiService } from '../catalog/catalog-api.service';
import { ProductsApiService } from './products-api.service';

const nonZero: ValidatorFn = (control) => (control.value === 0 ? { zero: true } : null);

@Component({
    selector: 'app-product-list',
    imports: [ReactiveFormsModule, RouterLink, CurrencyPipe],
    template: `
    <section class="page">
      <div class="toolbar">
        <h1>Products</h1>
        @if (canManage()) {
          <a class="btn primary" routerLink="/products/new">New product</a>
        }
      </div>

      <div class="filters">
        <input type="search" placeholder="Search name or SKU" aria-label="Search" [formControl]="search" />
        <select aria-label="Category" [formControl]="category">
          <option [ngValue]="null">All categories</option>
          @for (c of categories(); track c.id) {
            <option [ngValue]="c.id">{{ c.name }}</option>
          }
        </select>
        <label><input type="checkbox" [formControl]="lowStockOnly" /> Low stock only</label>
      </div>

      @if (error(); as message) {
        <p class="error" role="alert">{{ message }}</p>
      }

      <table>
        <thead>
          <tr>
            <th>SKU</th><th>Name</th><th>Category</th>
            <th class="num">Price</th><th class="num">Stock</th><th></th>
          </tr>
        </thead>
        <tbody>
          @for (p of result()?.items ?? []; track p.id) {
            <tr [class.low]="p.isLowStock" [class.inactive]="!p.isActive">
              <td>{{ p.sku }}</td>
              <td>{{ p.name }}</td>
              <td>{{ p.categoryName }}</td>
              <td class="num">{{ p.unitPrice | currency }}</td>
              <td class="num">
                {{ p.stockOnHand }}
                @if (p.isLowStock) {
                  <span class="badge">low</span>
                }
              </td>
              <td class="actions">
                @if (canManage()) {
                  <a [routerLink]="['/products', p.id]">Edit</a>
                  <button type="button" (click)="startAdjust(p)">Adjust stock</button>
                }
              </td>
            </tr>
          } @empty {
            <tr>
              <td colspan="6" class="empty">{{ loading() ? 'Loading…' : 'No products match.' }}</td>
            </tr>
          }
        </tbody>
      </table>

      <nav class="pager" aria-label="Pages">
        <button type="button" (click)="go(page() - 1)" [disabled]="page() <= 1">Previous</button>
        <span>Page {{ page() }} of {{ result()?.totalPages || 1 }} ({{ result()?.totalCount ?? 0 }} products)</span>
        <button type="button" (click)="go(page() + 1)" [disabled]="page() >= (result()?.totalPages || 1)">Next</button>
      </nav>

      @if (adjusting(); as product) {
        <form class="panel stack" [formGroup]="adjustForm" (ngSubmit)="applyAdjust()">
          <h2>Adjust stock: {{ product.sku }} (now {{ product.stockOnHand }})</h2>

          <label for="delta">Change (use a negative number to remove)</label>
          <input id="delta" type="number" formControlName="delta" />

          <label for="reason">Reason</label>
          <select id="reason" formControlName="reason">
            <option value="Received">Received a delivery</option>
            <option value="Adjustment">Stock count correction</option>
          </select>

          <label for="note">Note (optional)</label>
          <input id="note" type="text" formControlName="note" maxlength="200" />

          <div class="actions">
            <button type="button" (click)="cancelAdjust()">Cancel</button>
            <button type="submit" class="primary" [disabled]="adjustForm.invalid">Apply</button>
          </div>
        </form>
      }
    </section>
  `,
})
export class ProductListComponent {
    private readonly api = inject(ProductsApiService);
    private readonly catalog = inject(CatalogApiService);
    private readonly auth = inject(AuthService);
    private readonly hub = inject(StockHubService);
    private readonly fb = inject(NonNullableFormBuilder);
    private readonly refresh$ = new Subject<void>();

    protected readonly search = new FormControl('', { nonNullable: true });
    protected readonly category = new FormControl<number | null>(null);
    protected readonly lowStockOnly = new FormControl(false, { nonNullable: true });

    protected readonly result = signal<PagedResult<Product> | null>(null);
    protected readonly categories = signal<Category[]>([]);
    protected readonly page = signal(1);
    protected readonly loading = signal(false);
    protected readonly error = signal<string | null>(null);
    protected readonly adjusting = signal<Product | null>(null);
    protected readonly canManage = computed(() => this.auth.hasAnyRole('Admin', 'Manager'));

    protected readonly adjustForm = this.fb.group({
        delta: [0, [Validators.required, nonZero]],
        reason: this.fb.control<'Received' | 'Adjustment'>('Received'),
        note: [''],
    });

    constructor() {
        this.hub.stockChanged$.pipe(takeUntilDestroyed()).subscribe((change) =>
            this.result.update((current) =>
                current
                    ? {
                        ...current,
                        items: current.items.map((p) =>
                            p.id === change.productId
                                ? { ...p, stockOnHand: change.stockOnHand, isLowStock: change.isLowStock }
                                : p,
                        ),
                    }
                    : current,
            ),
        );
        this.catalog.categories().pipe(takeUntilDestroyed()).subscribe((c) => this.categories.set(c));

        this.refresh$
            .pipe(
                tap(() => {
                    this.loading.set(true);
                    this.error.set(null);
                }),
                switchMap(() =>
                    this.api
                        .search({
                            page: this.page(),
                            pageSize: 10,
                            search: this.search.value,
                            categoryId: this.category.value,
                            lowStockOnly: this.lowStockOnly.value,
                        })
                        .pipe(
                            catchError((err: unknown) => {
                                this.error.set(messageFrom(err));
                                return of(null);
                            }),
                        ),
                ),
                takeUntilDestroyed(),
            )
            .subscribe((result) => {
                if (result) {
                    this.result.set(result);
                }
                this.loading.set(false);
            });

        merge(
            this.search.valueChanges.pipe(debounceTime(300), distinctUntilChanged()),
            this.category.valueChanges,
            this.lowStockOnly.valueChanges,
        )
            .pipe(takeUntilDestroyed())
            .subscribe(() => {
                this.page.set(1);
                this.refresh$.next();
            });

        this.refresh$.next();
    }

    protected go(page: number): void {
        this.page.set(page);
        this.refresh$.next();
    }

    protected startAdjust(product: Product): void {
        this.adjustForm.reset({ delta: 0, reason: 'Received', note: '' });
        this.adjusting.set(product);
    }

    protected cancelAdjust(): void {
        this.adjusting.set(null);
    }

    protected applyAdjust(): void {
        const product = this.adjusting();
        if (!product || this.adjustForm.invalid) {
            return;
        }

        const { delta, reason, note } = this.adjustForm.getRawValue();
        this.api.adjustStock(product.id, { delta, reason, note: note || null }).subscribe({
            next: () => {
                this.adjusting.set(null);
                this.refresh$.next();
            },
            error: (err: unknown) => this.error.set(messageFrom(err)),
        });
    }
}