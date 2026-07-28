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

  login = { email: '', password: '' };

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
  ) {}

  submit(): void {
    this.loading.set(true);
    this.error.set('');
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
        this.error.set(err.error?.message ?? 'Không thể kết nối API.');
        this.loading.set(false);
      },
    });
  }
}
