import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../auth.service';

@Component({
  selector: 'app-reset-password',
  imports: [CommonModule, FormsModule],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.scss',
})
export class ResetPasswordComponent implements OnInit {
  readonly loading = signal(false);
  readonly error = signal('');
  readonly message = signal('');

  token = '';
  newPassword = '';
  confirmPassword = '';

  // FIX: đồng bộ với rule validate phía backend (AuthService.ValidatePassword):
  // 6-100 ký tự, có ít nhất 1 chữ hoa và 1 chữ số. Trước đây trang này chỉ có
  // minlength="6" nên người dùng nhập sai chuẩn phải chờ submit fail mới biết,
  // không nhất quán với trang đăng ký (đã validate đầy đủ).
  readonly passwordPattern = '^(?=.*[A-Z])(?=.*\\d).{6,100}$';
  readonly passwordMaxLength = 100;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly auth: AuthService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
  }

  submit(): void {
    this.error.set('');

    if (!this.newPassword || this.newPassword.length < 6) {
      this.error.set('Mật khẩu phải có ít nhất 6 ký tự.');
      return;
    }
    if (this.newPassword.length > this.passwordMaxLength) {
      this.error.set(`Mật khẩu không được vượt quá ${this.passwordMaxLength} ký tự.`);
      return;
    }
    if (!/[A-Z]/.test(this.newPassword)) {
      this.error.set('Mật khẩu phải chứa ít nhất 1 chữ hoa.');
      return;
    }
    if (!/\d/.test(this.newPassword)) {
      this.error.set('Mật khẩu phải chứa ít nhất 1 chữ số.');
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.error.set('Xác nhận mật khẩu chưa khớp.');
      return;
    }

    this.loading.set(true);
    this.auth.resetPassword({ token: this.token, newPassword: this.newPassword }).subscribe({
      next: response => {
        this.message.set(response.message);
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