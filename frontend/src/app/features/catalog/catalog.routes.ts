import { Routes } from '@angular/router';
import { adminGuard } from '../../core/auth.guard';

const routes: Routes = [
  { path: '', redirectTo: 'products', pathMatch: 'full' },
  {
    path: 'products',
    loadComponent: () =>
      import('./product-list/product-list.component').then(m => m.ProductListComponent),
  },
  {
    path: 'products/:id',
    loadComponent: () =>
      import('./product-detail/product-detail.component').then(m => m.ProductDetailComponent),
  },
  {
    // FIX: đây là giao diện quản trị (CRUD danh mục), trước đây không có canActivate nên
    // Customer gõ thẳng URL vẫn vào được UI quản trị (dù API bị chặn 403). Thêm adminGuard
    // để đồng bộ với admin.routes.ts.
    path: 'categories',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./category-manage/category-manage.component').then(m => m.CategoryManageComponent),
  },
  {
    // FIX: tương tự categories — đây là UI quản trị thương hiệu, cần adminGuard.
    path: 'brands',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./brand-manage/brand-manage.component').then(m => m.BrandManageComponent),
  },
];

export default routes;