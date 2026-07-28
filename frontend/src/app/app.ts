import { Component, computed } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService, LoginResponse } from './auth.service';
import { HeaderComponent } from './shared/header/header.component';
import { ToastComponent } from './shared/toast/toast.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, HeaderComponent, ToastComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  // Lấy session trực tiếp từ signal của AuthService, tự động cập nhật khi login/logout
  readonly session = computed<LoginResponse | null>(() => {
    const user = this.auth.currentUser();
    if (!user) return null;
    const stored = this.auth.restoreSession();
    return stored;
  });

  constructor(private readonly auth: AuthService) {}

  logout(): void {
    const refreshToken = this.auth.restoreSession()?.refreshToken;
    this.auth.logout(refreshToken).subscribe();
  }
}