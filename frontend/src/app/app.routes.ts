import { Routes } from '@angular/router';
import { authGuard, adminGuard } from './core/auth.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent),
  },
  {
    path: 'catalog',
    loadChildren: () => import('./features/catalog/catalog.routes'),
  },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes'),
  },
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadChildren: () => import('./features/admin/admin.routes'),
  },
  {
    path: 'account',
    canActivate: [authGuard],
    loadComponent: () => import('./features/account/account.component').then(m => m.AccountComponent),
  },
  {
    // FIX: trước đây không có authGuard, chỉ dựa vào component tự kiểm tra rồi
    // redirect trong ngOnInit -> có khoảnh khắc route/component load trước khi
    // redirect (flash nội dung), không đồng nhất với /account, /orders.
    path: 'cart',
    canActivate: [authGuard],
    loadComponent: () => import('./features/cart/cart.component').then(m => m.CartComponent),
  },
  {
    // FIX: tương tự cart.
    path: 'wishlist',
    canActivate: [authGuard],
    loadComponent: () => import('./features/wishlist/wishlist.component').then(m => m.WishlistComponent),
  },
  {
    // FIX: tương tự cart — checkout càng cần bảo vệ vì có thao tác tạo đơn hàng/thanh toán.
    path: 'checkout',
    canActivate: [authGuard],
    loadComponent: () => import('./features/checkout/checkout.component').then(m => m.CheckoutComponent),
  },
  {
    path: 'payment-result',
    loadComponent: () => import('./features/checkout/payment-result/payment-result.component').then(m => m.PaymentResultComponent),
  },
  {
    path: 'orders',
    canActivate: [authGuard],
    loadComponent: () => import('./features/orders/order-list/order-list.component').then(m => m.OrderListComponent),
  },
  {
    path: 'orders/:id',
    canActivate: [authGuard],
    loadComponent: () => import('./features/orders/order-detail/order-detail.component').then(m => m.OrderDetailComponent),
  },
  { path: '**', redirectTo: 'catalog' },
];