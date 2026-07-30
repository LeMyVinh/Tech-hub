import { Routes } from '@angular/router';
import { adminGuard } from '../../core/auth.guard';

const routes: Routes = [
  { path: '', redirectTo: 'products', pathMatch: 'full' },
  {
    path: 'products',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./product-manage/product-manage.component').then(m => m.ProductManageComponent),
  },
  {
    path: 'reviews',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./review-manage/review-manage.component').then(m => m.ReviewManageComponent),
  },
];

export default routes;