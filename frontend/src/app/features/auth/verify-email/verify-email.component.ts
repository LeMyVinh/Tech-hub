import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../../auth.service';

@Component({
  selector: 'app-verify-email',
  imports: [CommonModule, RouterLink],
  templateUrl: './verify-email.component.html',
  styleUrl: './verify-email.component.scss',
})
export class VerifyEmailComponent implements OnInit {
  readonly loading = signal(true);
  readonly error = signal('');
  readonly message = signal('');

  constructor(
    private readonly route: ActivatedRoute,
    private readonly auth: AuthService,
  ) {}

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!token) {
      this.loading.set(false);
      this.error.set('Liên kết xác thực không hợp lệ.');
      return;
    }

    this.auth.verifyEmail(token).subscribe({
      next: res => {
        this.message.set(res.message);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Xác thực email thất bại.');
        this.loading.set(false);
      },
    });
  }
}