import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../auth.service';

@Component({
  selector: 'app-verify-email',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './verify-email.component.html',
  styleUrl: './verify-email.component.scss',
})
export class VerifyEmailComponent implements OnInit {
  readonly loading = signal(false);
  readonly resending = signal(false);
  readonly error = signal('');
  readonly message = signal('');

  email = '';
  otp = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly auth: AuthService,
  ) {}

  ngOnInit(): void {
    this.email = this.route.snapshot.queryParamMap.get('email') ?? '';
    if (!this.email) {
      this.error.set('Không tìm thấy email. Vui lòng đăng ký lại.');
    }
  }

  submit(): void {
    this.error.set('');
    this.message.set('');

    if (!this.email) {
      this.error.set('Không tìm thấy email. Vui lòng đăng ký lại.');
      return;
    }
    if (!/^\d{6}$/.test(this.otp)) {
      this.error.set('Mã OTP phải gồm đúng 6 chữ số.');
      return;
    }

    this.loading.set(true);
    this.auth.verifyEmail(this.email, this.otp).subscribe({
      next: res => {
        this.message.set(res.message);
        this.loading.set(false);
        setTimeout(() => this.router.navigate(['/auth/login']), 1500);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Xác thực OTP thất bại.');
        this.loading.set(false);
      },
    });
  }

  resend(): void {
    if (!this.email) return;
    this.resending.set(true);
    this.error.set('');
    this.message.set('');
    this.auth.resendVerification(this.email).subscribe({
      next: res => {
        this.message.set(res.message);
        this.resending.set(false);
      },
      error: () => {
        this.error.set('Không thể gửi lại mã OTP, vui lòng thử lại sau.');
        this.resending.set(false);
      },
    });
  }
}