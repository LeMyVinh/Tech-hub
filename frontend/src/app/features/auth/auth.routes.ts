import { Routes } from '@angular/router';
import { guestGuard } from '../../core/auth.guard';

const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./login/login.component').then(m => m.LoginComponent),
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./register/register.component').then(m => m.RegisterComponent),
  },
  {
    path: 'forgot-password',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent),
  },
  {
    // FIX: KHÔNG gắn guestGuard ở đây. Trang này được mở từ link trong email
    // (dựa vào ?token=... trên URL), hoàn toàn độc lập với việc trình duyệt hiện
    // tại có đang đăng nhập tài khoản khác hay không. Trước đây gắn guestGuard
    // khiến người dùng đang có sẵn phiên đăng nhập (vd tài khoản A) bấm link reset
    // mật khẩu của tài khoản B từ email sẽ bị đá thẳng về trang chủ, không bao giờ
    // đặt lại được mật khẩu.
    path: 'reset-password',
    loadComponent: () =>
      import('./reset-password/reset-password.component').then(m => m.ResetPasswordComponent),
  },
  {
    // EMAIL VERIFICATION: cũng KHÔNG gắn guestGuard, cùng lý do như reset-password —
    // được mở từ link trong email, độc lập với phiên đăng nhập hiện tại của trình duyệt.
    path: 'verify-email',
    loadComponent: () =>
      import('./verify-email/verify-email.component').then(m => m.VerifyEmailComponent),
  },
];

export default routes;