import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { Category } from '../../core/api.models';
import { messageFrom } from '../../core/http-errors';
import { CatalogApiService } from '../catalog/catalog-api.service';
import { ProductsApiService } from './products-api.service';

@Component({
  selector: 'app-product-form',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <section class="page">
      <h1>{{ isEdit() ? 'Edit product' : 'New product' }}</h1>

      <form class="stack" [formGroup]="form" (ngSubmit)="save()">
        <label for="sku">SKU</label>
        <input id="sku" formControlName="sku" maxlength="40" />

        <label for="name">Name</label>
        <input id="name" formControlName="name" maxlength="200" />

        <label for="description">Description</label>
        <input id="description" formControlName="description" maxlength="2000" />

        <label for="categoryId">Category</label>
        <select id="categoryId" formControlName="categoryId">
          <option [ngValue]="0" disabled>Choose a category</option>
          @for (c of categories(); track c.id) {
            <option [ngValue]="c.id">{{ c.name }}</option>
          }
        </select>

        <label for="unitPrice">Unit price</label>
        <input id="unitPrice" type="number" step="0.01" formControlName="unitPrice" />

        @if (!isEdit()) {
          <label for="initialStock">Initial stock</label>
          <input id="initialStock" type="number" formControlName="initialStock" />
        }

        <label for="reorderLevel">Reorder level</label>
        <input id="reorderLevel" type="number" formControlName="reorderLevel" />

        @if (error(); as message) {
          <p class="error" role="alert">{{ message }}</p>
        }
        @if (stale()) {
          <p>
            <button type="button" (click)="load()">Reload the latest version</button>
          </p>
        }

        <div class="actions">
          <a class="btn" routerLink="/products">Cancel</a>
          <button type="submit" class="primary" [disabled]="form.invalid || saving()">Save</button>
        </div>
      </form>
    </section>
  `,
})
export class ProductFormComponent implements OnInit {
  readonly id = input<string>();

  private readonly api = inject(ProductsApiService);
  private readonly catalog = inject(CatalogApiService);
  private readonly router = inject(Router);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly categories = signal<Category[]>([]);
  protected readonly error = signal<string | null>(null);
  protected readonly saving = signal(false);
  protected readonly stale = signal(false);
  protected readonly isEdit = computed(() => this.id() !== undefined);

  protected readonly form = this.fb.group({
    sku: ['', [Validators.required, Validators.maxLength(40)]],
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: [''],
    categoryId: [0, [Validators.min(1)]],
    unitPrice: [0, [Validators.min(0.01)]],
    initialStock: [0, [Validators.min(0)]],
    reorderLevel: [0, [Validators.min(0)]],
  });

  private stamp = '';

  ngOnInit(): void {
    this.catalog.categories().subscribe((c) => this.categories.set(c));

    if (this.isEdit()) {
      this.form.controls.sku.disable();
      this.form.controls.initialStock.disable();
      this.load();
    }
  }

  protected load(): void {
    this.api.get(Number(this.id())).subscribe({
      next: (product) => {
        this.stamp = product.concurrencyStamp;
        this.stale.set(false);
        this.error.set(null);
        this.form.patchValue({
          sku: product.sku,
          name: product.name,
          description: product.description ?? '',
          categoryId: product.categoryId,
          unitPrice: product.unitPrice,
          reorderLevel: product.reorderLevel,
        });
      },
      error: (err: unknown) => this.error.set(messageFrom(err)),
    });
  }

  protected save(): void {
    if (this.form.invalid) {
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    const v = this.form.getRawValue();

    const request$ = this.isEdit()
      ? this.api.update(Number(this.id()), {
          name: v.name,
          description: v.description || null,
          categoryId: v.categoryId,
          unitPrice: v.unitPrice,
          reorderLevel: v.reorderLevel,
          concurrencyStamp: this.stamp,
        })
      : this.api.create({
          sku: v.sku,
          name: v.name,
          description: v.description || null,
          categoryId: v.categoryId,
          unitPrice: v.unitPrice,
          initialStock: v.initialStock,
          reorderLevel: v.reorderLevel,
        });

    request$.subscribe({
      next: () => void this.router.navigateByUrl('/products'),
      error: (err: unknown) => {
        this.error.set(messageFrom(err));
        this.stale.set(this.isEdit() && err instanceof HttpErrorResponse && err.status === 409);
        this.saving.set(false);
      },
    });
  }
}