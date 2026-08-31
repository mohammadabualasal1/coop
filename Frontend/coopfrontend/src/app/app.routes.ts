import { Routes } from '@angular/router';

import { UserRole } from './core/enums';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login').then((m) => m.LoginComponent)
  },
  {
    path: 'forbidden',
    loadComponent: () =>
      import('./features/auth/forbidden/forbidden').then((m) => m.ForbiddenComponent)
  },
  {
    path: 'merchant',
    loadChildren: () =>
      import('./features/merchant/merchant.routes').then((m) => m.MERCHANT_ROUTES),
    canActivate: [authGuard, roleGuard(UserRole.Merchant)]
  },
  {
    path: 'admin',
    loadChildren: () => import('./features/admin/admin.routes').then((m) => m.ADMIN_ROUTES),
    canActivate: [authGuard, roleGuard(UserRole.Admin)]
  },
  { path: '**', redirectTo: '/login' }
];
