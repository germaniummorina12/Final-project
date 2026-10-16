import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('starts signed out', () => {
    expect(service.isAuthenticated()).toBeFalse();
  });

  it('stores the session and exposes the roles after login', () => {
    service.login('manager@stockflow.local', 'secret').subscribe();

    const req = http.expectOne(`${environment.apiUrl}/auth/login`);
    expect(req.request.method).toBe('POST');
    req.flush({
      token: 'abc',
      expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString(),
      userName: 'manager@stockflow.local',
      roles: ['Manager'],
    });

    expect(service.isAuthenticated()).toBeTrue();
    expect(service.hasAnyRole('Admin', 'Manager')).toBeTrue();
    expect(service.hasAnyRole('Admin')).toBeFalse();
  });

  it('treats an expired session as signed out', () => {
    service.login('a@b.c', 'x').subscribe();
    http.expectOne(`${environment.apiUrl}/auth/login`).flush({
      token: 'abc',
      expiresAtUtc: new Date(Date.now() - 1000).toISOString(),
      userName: 'a@b.c',
      roles: [],
    });

    expect(service.isAuthenticated()).toBeFalse();
  });
});