import { Routes } from '@angular/router';
import { adminGuard } from '../../core/auth.guard';

const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: 'dashboard',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./dashboard/dashboard.component').then(m => m.DashboardComponent),
  },
  {
    path: 'products',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./product-manage/product-manage.component').then(m => m.ProductManageComponent),
  },
  {
    path: 'orders',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./order-manage/order-manage.component').then(m => m.OrderManageComponent),
  },
  {
    path: 'reviews',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./review-manage/review-manage.component').then(m => m.ReviewManageComponent),
  },
  {
    path: 'users',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./user-manage/user-manage.component').then(m => m.UserManageComponent),
  },
];

export default routes;