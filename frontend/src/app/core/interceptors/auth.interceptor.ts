import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { TokenService } from '../services/token.service';
import { AuthService } from '../../features/auth/data/auth.service';

/** On a 401 (expired access token), performs a single-flight silent refresh
 *    via the cookie and retries the original request once.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenService = inject(TokenService);
  const token = tokenService.token();

  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  const withCredentials = authReq.clone({ withCredentials: true });

  return next(withCredentials).pipe(
    catchError((error: HttpErrorResponse) => {
      const isAuthEndpoint = req.url.includes('/api/auth/');
      if (error.status !== 401 || isAuthEndpoint) {
        return throwError(() => error);
      }

      // Single-flight: concurrent 401s share one refresh call.
      return inject(AuthService).refresh().pipe(
        switchMap(() =>
          next(
            withCredentials.clone({
              setHeaders: {
                Authorization: `Bearer ${tokenService.token()}`,
              },
            }),
          ),
        ),
      );
    }),
  );
};
