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
];

export default routes;
