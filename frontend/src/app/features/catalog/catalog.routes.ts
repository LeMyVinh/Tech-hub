import { Routes } from '@angular/router';

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
    path: 'categories',
    loadComponent: () =>
      import('./category-manage/category-manage.component').then(m => m.CategoryManageComponent),
  },
  {
    path: 'brands',
    loadComponent: () =>
      import('./brand-manage/brand-manage.component').then(m => m.BrandManageComponent),
  },
];

export default routes;
