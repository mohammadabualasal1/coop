import { Routes } from '@angular/router';

export const MERCHANT_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./layout/merchant-layout/merchant-layout').then((m) => m.MerchantLayoutComponent),
    children: [
      { path: '', redirectTo: 'overview', pathMatch: 'full' },
      {
        path: 'overview',
        data: { title: 'الرئيسية' },
        loadComponent: () => import('./pages/overview/overview').then((m) => m.OverviewComponent)
      },
      {
        path: 'profile',
        data: { title: 'ملف المتجر' },
        loadComponent: () => import('./pages/profile/profile').then((m) => m.ProfileComponent)
      },
      {
        path: 'branches',
        data: { title: 'الفروع' },
        loadComponent: () => import('./pages/branches/branches').then((m) => m.BranchesComponent)
      },
      {
        path: 'products',
        data: { title: 'المنتجات' },
        loadComponent: () => import('./pages/products/products').then((m) => m.ProductsComponent)
      },
      {
        path: 'offers',
        data: { title: 'العروض' },
        loadComponent: () => import('./pages/offers/offers').then((m) => m.OffersComponent)
      },
      {
        path: 'orders',
        data: { title: 'الطلبات' },
        loadComponent: () => import('./pages/orders/orders').then((m) => m.OrdersComponent)
      },
      {
        path: 'reviews',
        data: { title: 'التقييمات' },
        loadComponent: () => import('./pages/reviews/reviews').then((m) => m.ReviewsComponent)
      },
      {
        path: 'complaints',
        data: { title: 'الشكاوى' },
        loadComponent: () =>
          import('./pages/complaints/complaints').then((m) => m.ComplaintsComponent)
      },
      {
        path: 'notifications',
        data: { title: 'الإشعارات' },
        loadComponent: () =>
          import('./pages/notifications/notifications').then((m) => m.NotificationsComponent)
      }
    ]
  }
];
