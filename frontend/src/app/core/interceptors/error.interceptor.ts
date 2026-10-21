import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, throwError } from 'rxjs';
import { ApiResponse } from '../models/api-response';

/**
 * Surfaces API failures ({ message, errors } envelope) as a snackbar.
 * Auth endpoints are skipped — login/register/refresh show errors inline.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (!req.url.includes('/api/auth/')) {
        const body = error.error as ApiResponse<unknown> | null;
        const message =
          body?.message ||
          (error.status === 0
            ? 'Cannot reach the server. Is the API running?'
            : 'Something went wrong');
        inject(MatSnackBar).open(message, 'Close', { duration: 4000 });
      }
      return throwError(() => error);
    }),
  );
