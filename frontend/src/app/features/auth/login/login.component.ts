import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../auth.service';

@Component({
  selector: 'app-login',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  readonly loading = signal(false);
  readonly error = signal('');

  // EMAIL VERIFICATION: cho phép gửi lại email xác thực ngay tại trang đăng nhập
  // khi backend từ chối vì email chưa được xác thực.
  readonly showResend = signal(false);
  readonly resending = signal(false);
  readonly resendMessage = signal('');

  login = { email: '', password: '' };

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
  ) {}

  submit(): void {
    this.loading.set(true);
    this.error.set('');
    this.showResend.set(false);
    this.resendMessage.set('');

    this.auth.login(this.login).subscribe({
      next: response => {
        this.auth.saveSession(response);
        this.loading.set(false);
        if (response.user.role === 'Admin') {
          this.router.navigate(['/admin/products']);
        } else {
          this.router.navigate(['/catalog/products']);
        }
      },
      error: err => {
        const msg = err.error?.message ?? 'Không thể kết nối API.';
        this.error.set(msg);
        // EMAIL VERIFICATION: nhận diện đúng thông báo lỗi từ backend
        // (AuthService.LoginAsync) để hiện nút "Gửi lại email xác thực".
        this.showResend.set(msg.includes('chưa được xác thực'));
        this.loading.set(false);
      },
    });
  }

  resendVerification(): void {
    if (!this.login.email) return;
    this.resending.set(true);
    this.resendMessage.set('');
    this.auth.resendVerification(this.login.email).subscribe({
      next: res => {
        this.resendMessage.set(res.message);
        this.resending.set(false);
      },
      error: () => {
        this.resendMessage.set('Không thể gửi lại email xác thực, vui lòng thử lại sau.');
        this.resending.set(false);
      },
    });
  }
}