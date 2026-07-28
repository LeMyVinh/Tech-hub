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

  constructor(
    private readonly route: ActivatedRoute,
    private readonly auth: AuthService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
  }

  submit(): void {
    if (this.newPassword !== this.confirmPassword) {
      this.error.set('Xác nhận mật khẩu chưa khớp.');
      return;
    }
    this.loading.set(true);
    this.error.set('');
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
