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

  register = {
    fullName: '',
    email: '',
    password: '',
    confirmPassword: '',
    phone: '',
  };

  // Đồng bộ với rule validate phía backend:
  // - Họ tên: chỉ chữ cái Unicode (bao gồm tiếng Việt) và khoảng trắng.
  // - Họ tên phải có ít nhất 2 từ.
  // - Tổng độ dài từ 2-150 ký tự.
  // - Không chứa số hoặc ký tự đặc biệt.
  // - Email: đúng định dạng, tối đa 254 ký tự.
  // - SĐT: không bắt buộc, nhưng nếu nhập phải đủ 10 số và bắt đầu bằng 0.
  // - Mật khẩu: 6-100 ký tự, có ít nhất 1 chữ hoa và 1 chữ số.

  readonly fullNamePattern = /^[\p{L}]+(?: [\p{L}]+)+$/u;
  readonly emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  readonly phonePattern = /^0[0-9]{9}$/;
  readonly passwordPattern = /^(?=.*[A-Z])(?=.*\d).{6,100}$/;

  readonly emailMaxLength = 254;
  readonly passwordMaxLength = 100;

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
  ) {}

  submit(): void {
    this.error.set('');

    // Chuẩn hóa khoảng trắng thừa
    // Ví dụ: "  Nguyễn   Văn   An  " -> "Nguyễn Văn An"
    const fullName = this.register.fullName.trim().replace(/\s+/g, ' ');
    const email = this.register.email.trim().toLowerCase();
    const phone = this.register.phone.trim();

    // =========================
    // VALIDATE HỌ VÀ TÊN
    // =========================

    if (!fullName) {
      this.error.set('Vui lòng nhập họ và tên.');
      return;
    }

    if (fullName.length < 2) {
      this.error.set('Họ và tên phải có ít nhất 2 ký tự.');
      return;
    }

    if (fullName.length > 150) {
      this.error.set('Họ và tên không được vượt quá 150 ký tự.');
      return;
    }

    if (!this.fullNamePattern.test(fullName)) {
      this.error.set(
        'Họ và tên phải gồm ít nhất 2 từ, chỉ được chứa chữ cái và khoảng trắng, không chứa số hoặc ký tự đặc biệt.'
      );
      return;
    }

    // =========================
    // VALIDATE EMAIL
    // =========================

    if (!email) {
      this.error.set('Vui lòng nhập email.');
      return;
    }

    if (email.length > this.emailMaxLength) {
      this.error.set(
        `Email không được vượt quá ${this.emailMaxLength} ký tự.`,
      );
      return;
    }

    if (!this.emailPattern.test(email)) {
      this.error.set('Email không đúng định dạng.');
      return;
    }

    // =========================
    // VALIDATE SỐ ĐIỆN THOẠI
    // =========================

    if (phone && !this.phonePattern.test(phone)) {
      this.error.set(
        'Số điện thoại không hợp lệ (phải gồm 10 số, bắt đầu bằng 0).',
      );
      return;
    }

    // =========================
    // VALIDATE MẬT KHẨU
    // =========================

    if (!this.register.password || this.register.password.length < 6) {
      this.error.set('Mật khẩu phải có ít nhất 6 ký tự.');
      return;
    }

    if (this.register.password.length > this.passwordMaxLength) {
      this.error.set(
        `Mật khẩu không được vượt quá ${this.passwordMaxLength} ký tự.`,
      );
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

    // =========================
    // GỬI REQUEST ĐĂNG KÝ
    // =========================

    this.loading.set(true);

    const { confirmPassword, ...request } = this.register;

    request.fullName = fullName;
    request.email = email;
    request.phone = phone;

    this.auth.register(request).subscribe({
      next: () => {
        // Đăng ký thành công -> chuyển sang trang nhập OTP
        this.loading.set(false);

        this.router.navigate(['/auth/verify-email'], {
          queryParams: { email },
        });
      },

      error: err => {
        this.error.set(
          err.error?.message ?? 'Không thể kết nối API.',
        );

        this.loading.set(false);
      },
    });
  }
}

