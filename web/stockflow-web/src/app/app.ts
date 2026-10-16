import { Component, effect, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { StockHubService } from './core/stock-hub.service';
import { AuthService } from './core/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    @if (auth.isAuthenticated()) {
      <header class="bar">
        <strong class="brand">StockFlow</strong>
        <nav aria-label="Main">
          <a routerLink="/dashboard" routerLinkActive="active">Dashboard</a>
          <a routerLink="/products" routerLinkActive="active">Products</a>
          <a routerLink="/orders" routerLinkActive="active">Orders</a>
        </nav>
        <span class="spacer"></span>
        <span class="who">{{ auth.userName() }} ({{ auth.roles().join(', ') }})</span>
        <button type="button" (click)="auth.logout()">Sign out</button>
      </header>
    }
    <router-outlet />
  `,
})
export class App {
  protected readonly auth = inject(AuthService);
  private readonly hub = inject(StockHubService);

  constructor() {
    effect(() => {
      if (this.auth.isAuthenticated()) {
        void this.hub.connect();
      } else {
        void this.hub.disconnect();
      }
    });
  }
}