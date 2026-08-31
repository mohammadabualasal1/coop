import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { UserRole } from '../enums';
import { AuthService } from '../services/auth.service';

export function roleGuard(...allowed: UserRole[]): CanActivateFn {
  return (_route, state) => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.isAuthenticated()) {
      return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
    }

    const role = auth.role();
    if (role !== null && allowed.includes(role)) {
      return true;
    }

    return router.createUrlTree(['/forbidden']);
  };
}
