import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../auth.service';

@Component({
  selector: 'app-forgot-password',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss',
})
export class ForgotPasswordComponent {
  readonly loading = signal(false);
  readonly error = signal('');
  readonly message = signal('');

  email = '';

  constructor(private readonly auth: AuthService) {}

  submit(): void {
    this.loading.set(true);
    this.error.set('');
    this.auth.forgotPassword({ email: this.email }).subscribe({
      next: response => {
        this.message.set(response.message);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Không thể kết nối API.');
        this.loading.set(false);
      },
    });
  }
}
