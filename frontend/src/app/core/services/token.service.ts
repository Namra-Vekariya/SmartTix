import { Injectable, signal } from '@angular/core';

//  after a page reload the auth interceptor silently re-obtains
//  an access token via POST /api/auth/refresh.
 
@Injectable({ providedIn: 'root' })
export class TokenService {
  private readonly _token = signal<string | null>(null);

  /** Reactive access token; null when logged out or after reload (until refresh). */
  readonly token = this._token.asReadonly();

  set(token: string): void {
    this._token.set(token);
  }

  clear(): void {
    this._token.set(null);
  }
}
