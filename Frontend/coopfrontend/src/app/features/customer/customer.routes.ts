import { Routes } from '@angular/router';

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
      { path: '**', redirectTo: '' }
    ]
  }
];
