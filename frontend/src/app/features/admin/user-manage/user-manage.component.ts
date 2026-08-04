import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../auth.service';
import { AdminUserListResponse, AdminUserProfile, UserManageService } from './user.service';

@Component({
  selector: 'app-user-manage',
  imports: [CommonModule, RouterLink],
  templateUrl: './user-manage.component.html',
  styleUrl: './user-manage.component.scss',
})
export class UserManageComponent implements OnInit {
  readonly users = signal<AdminUserListResponse | null>(null);
  readonly loading = signal(false);
  readonly message = signal('');
  readonly error = signal('');
  readonly processingId = signal<number | null>(null);

  currentPage = 1;
  readonly pageSize = 10;
  readonly Math = Math;

  constructor(
    private readonly auth: AuthService,
    private readonly userService: UserManageService,
  ) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  private getToken(): string | null {
    return this.auth.restoreSession()?.token ?? null;
  }

  loadUsers(): void {
    const token = this.getToken();
    if (!token) return;
    this.loading.set(true);
    this.userService.getUsers(token, this.currentPage, this.pageSize).subscribe({
      next: res => {
        this.users.set(res);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Không thể tải danh sách người dùng.');
        this.loading.set(false);
      },
    });
  }

  toggleLock(u: AdminUserProfile): void {
    const token = this.getToken();
    if (!token) return;

    const actionLabel = u.isActive ? 'khóa' : 'mở khóa';
    if (!confirm(`Bạn có chắc chắn muốn ${actionLabel} tài khoản "${u.fullName}"?`)) return;

    this.processingId.set(u.id);
    const request$ = u.isActive
      ? this.userService.lockUser(token, u.id)
      : this.userService.unlockUser(token, u.id);

    request$.subscribe({
      next: res => {
        this.message.set(res.message);
        this.processingId.set(null);
        this.loadUsers();
      },
      error: err => {
        this.error.set(err.error?.message ?? `Lỗi khi ${actionLabel} tài khoản.`);
        this.processingId.set(null);
      },
    });
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadUsers();
    }
  }

  nextPage(): void {
    if (this.users() && this.currentPage * this.pageSize < this.users()!.totalCount) {
      this.currentPage++;
      this.loadUsers();
    }
  }

  clearFeedback(): void {
    this.message.set('');
    this.error.set('');
  }
}