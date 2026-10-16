import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs';

import { environment } from '../../environments/environment';
import { LoginResponse } from './api.models';

const STORAGE_KEY = 'stockflow.session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly session = signal<LoginResponse | null>(this.restore());

  readonly userName = computed(() => this.session()?.userName ?? '');
  readonly roles = computed(() => this.session()?.roles ?? []);
  readonly token = computed(() => this.session()?.token ?? null);
  readonly isAuthenticated = computed(() => {
    const session = this.session();
    return session !== null && new Date(session.expiresAtUtc).getTime() > Date.now();
  });

  login(email: string, password: string) {
    return this.http
      .post<LoginResponse>(`${environment.apiUrl}/auth/login`, { email, password })
      .pipe(tap((response) => this.startSession(response)));
  }

  logout(): void {
    this.session.set(null);
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {
    }
    void this.router.navigateByUrl('/login');
  }

  hasAnyRole(...roles: string[]): boolean {
    const mine = this.roles();
    return roles.some((role) => mine.includes(role));
  }

  private startSession(response: LoginResponse): void {
    this.session.set(response);
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(response));
    } catch {
    }
  }

  private restore(): LoginResponse | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw ? (JSON.parse(raw) as LoginResponse) : null;
    } catch {
      return null;
    }
  }
}