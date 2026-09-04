import { Routes } from '@angular/router';

import { UserRole } from '../../core/enums';
import { authGuard } from '../../core/guards/auth.guard';
import { roleGuard } from '../../core/guards/role.guard';

export const CUSTOMER_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./layout/shop-layout/shop-layout').then((m) => m.ShopLayoutComponent),
    children: [
      {
        path: '',
        loadComponent: () => import('./pages/home/home').then((m) => m.HomeComponent)
      },
      {
        path: 'search',
        loadComponent: () => import('./pages/search/search').then((m) => m.SearchComponent)
      },
      {
        path: 'offers/:id',
        loadComponent: () =>
          import('./pages/offer-detail/offer-detail').then((m) => m.OfferDetailComponent)
      },
      {
        path: 'merchants',
        loadComponent: () => import('./pages/merchants/merchants').then((m) => m.MerchantsComponent)
      },
      {
        path: 'merchants/:id',
        loadComponent: () =>
          import('./pages/merchant-detail/merchant-detail').then((m) => m.MerchantDetailComponent)
      },
      {
        path: 'favorites',
        canActivate: [authGuard, roleGuard(UserRole.Customer)],
        loadComponent: () =>
          import('./pages/favorites/favorites').then((m) => m.FavoritesComponent)
      },
      {
        path: 'following',
        canActivate: [authGuard, roleGuard(UserRole.Customer)],
        loadComponent: () =>
          import('./pages/following/following').then((m) => m.FollowingComponent)
      },
      {
        path: 'cart',
        canActivate: [authGuard, roleGuard(UserRole.Customer)],
        loadComponent: () => import('./pages/cart/cart').then((m) => m.CartComponent)
      },
      {
        path: 'addresses',
        canActivate: [authGuard, roleGuard(UserRole.Customer)],
        loadComponent: () =>
          import('./pages/addresses/addresses').then((m) => m.AddressesComponent)
      },
      {
        path: 'checkout',
        canActivate: [authGuard, roleGuard(UserRole.Customer)],
        loadComponent: () => import('./pages/checkout/checkout').then((m) => m.CheckoutComponent)
      },
      {
        path: 'orders',
        canActivate: [authGuard, roleGuard(UserRole.Customer)],
        loadComponent: () => import('./pages/orders/orders').then((m) => m.OrdersComponent)
      },
      {
        path: 'orders/:id',
        canActivate: [authGuard, roleGuard(UserRole.Customer)],
        loadComponent: () =>
          import('./pages/order-detail/order-detail').then((m) => m.OrderDetailComponent)
      },
      { path: '**', redirectTo: '' }
    ]
  }
];
