import { Routes } from '@angular/router';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./layout/admin-layout/admin-layout').then((m) => m.AdminLayoutComponent),
    children: [
      { path: '', redirectTo: 'overview', pathMatch: 'full' },
      {
        path: 'overview',
        data: { title: 'الرئيسية' },
        loadComponent: () => import('./pages/overview/overview').then((m) => m.OverviewComponent)
      },
      {
        path: 'users',
        data: { title: 'المستخدمون' },
        loadComponent: () => import('./pages/users/users').then((m) => m.UsersComponent)
      },
      {
        path: 'merchants',
        data: { title: 'التجار' },
        loadComponent: () => import('./pages/merchants/merchants').then((m) => m.MerchantsComponent)
      },
      {
        path: 'drivers',
        data: { title: 'السائقون' },
        loadComponent: () => import('./pages/drivers/drivers').then((m) => m.DriversComponent)
      },
      {
        path: 'offers',
        data: { title: 'مراجعة العروض' },
        loadComponent: () => import('./pages/offers/offers').then((m) => m.OffersComponent)
      },
      {
        path: 'categories',
        data: { title: 'التصنيفات' },
        loadComponent: () =>
          import('./pages/categories/categories').then((m) => m.CategoriesComponent)
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
