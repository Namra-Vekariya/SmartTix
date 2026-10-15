import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  Observable,
  catchError,
  finalize,
  firstValueFrom,
  map,
  of,
  shareReplay,
  throwError,
} from 'rxjs';
import { API_BASE_URL } from '../../../core/tokens';
import { TokenService } from '../../../core/services/token.service';
import { ApiResponse } from '../../../core/models/api-response';
import {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  User,
} from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenService = inject(TokenService);
  private readonly router = inject(Router);
  private readonly baseUrl = inject(API_BASE_URL);

  private readonly user = signal<User | null>(null);

  readonly currentUser = this.user.asReadonly();
  readonly isAuthenticated = computed(() => this.user() !== null);
  readonly isAdmin = computed(() => this.user()?.role === 'Admin');

  /** In-flight refresh shared by every caller (single-flight). */
  private refreshInFlight$: Observable<AuthResponse> | null = null;

  /** Called from provideAppInitializer — restores the session after reload. */
  init(): Promise<void> {
    return firstValueFrom(this.refresh().pipe(catchError(() => of(null)))).then(
      () => undefined,
    );
  }

  login(request: LoginRequest): Observable<User> {
    return this.http
      .post<ApiResponse<AuthResponse>>(
        `${this.baseUrl}/api/auth/login`,
        request,
        { withCredentials: true },
      )
      .pipe(map((response) => this.applyAuth(response.data!)));
  }

  register(request: RegisterRequest): Observable<User> {
    return this.http
      .post<ApiResponse<AuthResponse>>(
        `${this.baseUrl}/api/auth/register`,
        request,
        { withCredentials: true },
      )
      .pipe(map((response) => this.applyAuth(response.data!)));
  }

  /**
   * POST /api/auth/refresh — cookie carries the refresh token. Concurrent
   * calls share one HTTP request; on failure the session is cleared.
   */
  refresh(): Observable<AuthResponse> {
    if (this.refreshInFlight$) {
      return this.refreshInFlight$;
    }

    this.refreshInFlight$ = this.http
      .post<ApiResponse<AuthResponse>>(
        `${this.baseUrl}/api/auth/refresh`,
        {},
        { withCredentials: true },
      )
      .pipe(
        map((response) => {
          this.applyAuth(response.data!);
          return response.data!;
        }),
        catchError((error) => {
          this.clearSession();
          return throwError(() => error);
        }),
        finalize(() => (this.refreshInFlight$ = null)),
        shareReplay({ bufferSize: 1, refCount: false }),
      );

    return this.refreshInFlight$;
  }

  logout(): void {
    this.http
      .post(
        `${this.baseUrl}/api/auth/logout`,
        {},
        { withCredentials: true },
      )
      .subscribe({
        // Logout is idempotent server-side; clear locally regardless.
        next: () => this.finishLogout(),
        error: () => this.finishLogout(),
      });
  }

  private finishLogout(): void {
    this.clearSession();
    void this.router.navigate(['/login']);
  }

  private applyAuth(data: AuthResponse): User {
    this.tokenService.set(data.accessToken);
    this.user.set(data.user);
    return data.user;
  }

  private clearSession(): void {
    this.tokenService.clear();
    this.user.set(null);
  }
}
