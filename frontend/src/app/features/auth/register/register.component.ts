import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../auth.service';

@Component({
  selector: 'app-register',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
})
export class RegisterComponent {
  readonly loading = signal(false);
  readonly error = signal('');
  readonly message = signal('');

  register = { fullName: '', email: '', password: '', confirmPassword: '', phone: '' };

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
  ) {}

  submit(): void {
    if (this.register.password !== this.register.confirmPassword) {
      this.error.set('Xác nhận mật khẩu chưa khớp.');
      return;
    }
    this.loading.set(true);
    this.error.set('');
    const { confirmPassword, ...request } = this.register;
    this.auth.register(request).subscribe({
      next: () => {
        this.message.set('Đăng ký thành công. Hãy đăng nhập để tiếp tục.');
        this.loading.set(false);
        setTimeout(() => this.router.navigate(['/auth/login']), 1500);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Không thể kết nối API.');
        this.loading.set(false);
      },
    });
  }
}
