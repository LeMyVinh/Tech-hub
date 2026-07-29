import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

export interface LoginResponse {
  token: string;
  refreshToken: string;
  user: { id: number; fullName: string; role: string };
}

interface RegisterRequest { fullName: string; email: string; password: string; phone: string; }
interface LoginRequest { email: string; password: string; }
interface ForgotPasswordRequest { email: string; }
interface ResetPasswordRequest { token: string; newPassword: string; }
interface ChangePasswordRequest { oldPassword: string; newPassword: string; confirmPassword: string; }

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = 'http://localhost:5159/api/v1/auth';
  private readonly storageKey = 'techhub-auth-session';

  readonly currentUser = signal<LoginResponse['user'] | null>(null);

  constructor(private readonly http: HttpClient) {
    const session = this.restoreSession();
    if (session) {
      this.currentUser.set(session.user);
    }
  }

  register(request: RegisterRequest): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/register`, request);
  }

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, request).pipe(
      tap(session => {
        this.saveSession(session);
        this.currentUser.set(session.user);
      })
    );
  }

  refresh(refreshToken: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/refresh`, { refreshToken });
  }

  forgotPassword(request: ForgotPasswordRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/forgot-password`, request);
  }

  resetPassword(request: ResetPasswordRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/reset-password`, request);
  }

  changePassword(token: string, request: ChangePasswordRequest): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/change-password`, request, {
      headers: new HttpHeaders({ Authorization: `Bearer ${token}` }),
    });
  }

  logout(refreshToken: string | undefined): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/logout`, { refreshToken }).pipe(
      tap(() => {
        this.clearSession();
        this.currentUser.set(null);
      })
    );
  }

  saveSession(session: LoginResponse): void {
    localStorage.setItem(this.storageKey, JSON.stringify(session));
  }

  restoreSession(): LoginResponse | null {
    try {
      return JSON.parse(localStorage.getItem(this.storageKey) ?? 'null') as LoginResponse | null;
    } catch {
      this.clearSession();
      return null;
    }
  }

  clearSession(): void {
    localStorage.removeItem(this.storageKey);
  }

  forceLogout(): void {
    this.clearSession();
    this.currentUser.set(null);
  }
}