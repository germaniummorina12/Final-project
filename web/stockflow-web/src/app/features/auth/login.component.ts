import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { AuthService } from '../../core/auth.service';
import { messageFrom } from '../../core/http-errors';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  template: `
    <main class="login">
      <h1>StockFlow</h1>
      <form [formGroup]="form" (ngSubmit)="submit()">
        <label for="email">Email</label>
        <input id="email" type="email" formControlName="email" autocomplete="username" />

        <label for="password">Password</label>
        <input id="password" type="password" formControlName="password" autocomplete="current-password" />

        @if (error(); as message) {
          <p class="error" role="alert">{{ message }}</p>
        }

        <button type="submit" class="primary" [disabled]="form.invalid || busy()">Sign in</button>
      </form>
      <p class="hint">Demo accounts: admin, manager or clerk at stockflow.local. Password: StockFlow#2026</p>
    </main>
  `,
})
export class LoginComponent {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  protected submit(): void {
    if (this.form.invalid) {
      return;
    }

    this.busy.set(true);
    this.error.set(null);

    const { email, password } = this.form.getRawValue();
    this.auth.login(email, password).subscribe({
      next: () => void this.router.navigateByUrl('/dashboard'),
      error: (err: unknown) => {
        this.error.set(messageFrom(err));
        this.busy.set(false);
      },
    });
  }
}