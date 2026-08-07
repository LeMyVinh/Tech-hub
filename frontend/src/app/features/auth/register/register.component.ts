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

  // Đồng bộ với rule validate phía backend (AuthService.ValidatePassword):
  // tối thiểu 6 ký tự, tối đa 100 ký tự, có ít nhất 1 chữ hoa và 1 chữ số.
  readonly emailPattern = '^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$';
  readonly phonePattern = '^0[0-9]{9}$';
  readonly passwordPattern = '^(?=.*[A-Z])(?=.*\\d).{6,100}$';

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
  ) {}

  submit(): void {
    this.error.set('');

    const fullName = this.register.fullName.trim();
    const email = this.register.email.trim();
    const phone = this.register.phone.trim();

    if (!fullName) {
      this.error.set('Vui lòng nhập họ và tên.');
      return;
    }
    if (!email || !new RegExp(this.emailPattern).test(email)) {
      this.error.set('Email không đúng định dạng.');
      return;
    }
    if (phone && !new RegExp(this.phonePattern).test(phone)) {
      this.error.set('Số điện thoại không hợp lệ (phải gồm 10 số, bắt đầu bằng 0).');
      return;
    }
    if (!this.register.password || this.register.password.length < 6) {
      this.error.set('Mật khẩu phải có ít nhất 6 ký tự.');
      return;
    }
    if (this.register.password.length > 100) {
      this.error.set('Mật khẩu không được vượt quá 100 ký tự.');
      return;
    }
    if (!/[A-Z]/.test(this.register.password)) {
      this.error.set('Mật khẩu phải chứa ít nhất 1 chữ hoa.');
      return;
    }
    if (!/\d/.test(this.register.password)) {
      this.error.set('Mật khẩu phải chứa ít nhất 1 chữ số.');
      return;
    }
    if (this.register.password !== this.register.confirmPassword) {
      this.error.set('Xác nhận mật khẩu chưa khớp.');
      return;
    }

    this.loading.set(true);
    const { confirmPassword, ...request } = this.register;
    request.fullName = fullName;
    request.email = email;
    request.phone = phone;

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