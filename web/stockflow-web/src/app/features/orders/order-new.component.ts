import { CurrencyPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { startWith } from 'rxjs';

import { Customer, Product } from '../../core/api.models';
import { messageFrom } from '../../core/http-errors';
import { CatalogApiService } from '../catalog/catalog-api.service';
import { ProductsApiService } from '../products/products-api.service';
import { OrdersApiService } from './orders-api.service';

@Component({
  selector: 'app-order-new',
  imports: [ReactiveFormsModule, RouterLink, CurrencyPipe],
  template: `
    <section class="page">
      <h1>New order</h1>

      <form class="stack" [formGroup]="form" (ngSubmit)="submit()">
        <label for="customer">Customer</label>
        <select id="customer" formControlName="customerId">
          <option [ngValue]="0" disabled>Choose a customer</option>
          @for (c of customers(); track c.id) {
            <option [ngValue]="c.id">{{ c.name }}</option>
          }
        </select>

        <h2>Lines</h2>
        <div formArrayName="lines">
          @for (line of lines.controls; track line; let i = $index) {
            <div class="filters" [formGroupName]="i">
              <select formControlName="productId" [attr.aria-label]="'Product for line ' + (i + 1)">
                <option [ngValue]="0" disabled>Choose a product</option>
                @for (p of products(); track p.id) {
                  <option [ngValue]="p.id">{{ p.sku }}: {{ p.name }} ({{ p.stockOnHand }} in stock)</option>
                }
              </select>
              <input type="number" formControlName="quantity" min="1" [attr.aria-label]="'Quantity for line ' + (i + 1)" />
              <button type="button" (click)="removeLine(i)" [disabled]="lines.length === 1">Remove</button>
            </div>
          }
        </div>
        <div><button type="button" (click)="addLine()">Add a line</button></div>

        <p><strong>Total: {{ total() | currency }}</strong></p>

        @if (error(); as message) {
          <p class="error" role="alert">{{ message }}</p>
        }

        <div class="actions">
          <a class="btn" routerLink="/orders">Cancel</a>
          <button type="submit" class="primary" [disabled]="form.invalid || saving()">Place order</button>
        </div>
      </form>
    </section>
  `,
})
export class OrderNewComponent {
  private readonly ordersApi = inject(OrdersApiService);
  private readonly productsApi = inject(ProductsApiService);
  private readonly catalog = inject(CatalogApiService);
  private readonly router = inject(Router);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly customers = signal<Customer[]>([]);
  protected readonly products = signal<Product[]>([]);
  protected readonly error = signal<string | null>(null);
  protected readonly saving = signal(false);

  protected readonly form = this.fb.group({
    customerId: [0, [Validators.min(1)]],
    lines: this.fb.array([this.newLine()]),
  });

  private readonly value = toSignal(this.form.valueChanges.pipe(startWith(this.form.getRawValue())), {
    initialValue: this.form.getRawValue(),
  });

  protected readonly total = computed(() =>
    (this.value().lines ?? []).reduce((sum, line) => sum + this.priceOf(line.productId ?? 0) * (line.quantity ?? 0), 0),
  );

  constructor() {
    this.catalog.customers().subscribe((c) => this.customers.set(c));
    this.productsApi.search({ page: 1, pageSize: 100 }).subscribe((r) => this.products.set(r.items));
  }

  protected get lines() {
    return this.form.controls.lines;
  }

  protected addLine(): void {
    this.lines.push(this.newLine());
  }

  protected removeLine(index: number): void {
    if (this.lines.length > 1) {
      this.lines.removeAt(index);
    }
  }

  protected submit(): void {
    if (this.form.invalid) {
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    const { customerId, lines } = this.form.getRawValue();

    this.ordersApi.place({ customerId, lines }).subscribe({
      next: () => void this.router.navigateByUrl('/orders'),
      error: (err: unknown) => {
        this.error.set(messageFrom(err));
        this.saving.set(false);
      },
    });
  }

  private newLine() {
    return this.fb.group({
      productId: [0, [Validators.min(1)]],
      quantity: [1, [Validators.required, Validators.min(1), Validators.max(10_000)]],
    });
  }

  private priceOf(productId: number): number {
    return this.products().find((p) => p.id === productId)?.unitPrice ?? 0;
  }
}