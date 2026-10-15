import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../../features/auth/data/auth.service';

/** Requires a logged-in user; otherwise redirects to /login with returnUrl. */
export const authGuard: CanActivateFn = (_route, state) => {
  if (inject(AuthService).isAuthenticated()) {
    return true;
  }
  return inject(Router).createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url },
  });
};

/** Requires the Admin role. */
export const adminGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAdmin()) {
    return true;
  }
  return auth.isAuthenticated()
    ? router.createUrlTree(['/'])
    : router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

/** For login/register only — sends already-authenticated users home. */
export const guestGuard: CanActivateFn = () => {
  if (inject(AuthService).isAuthenticated()) {
    return inject(Router).createUrlTree(['/']);
  }
  return true;
};
