import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface AdminUserProfile {
  id: number;
  fullName: string;
  email: string;
  phone: string | null;
  role: string;
  isDeleted: boolean;
  deletedAt: string | null;
  createdAt: string;
}

export interface AdminUserListResponse {
  users: AdminUserProfile[];
  totalCount: number;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class UserManageService {
  private readonly apiUrl = 'http://localhost:5159/api/v1/admin/users';

  constructor(private readonly http: HttpClient) {}

  private headers(token: string): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }

  getUsers(token: string, page = 1, pageSize = 10): Observable<AdminUserListResponse> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<AdminUserListResponse>(this.apiUrl, {
      headers: this.headers(token),
      params,
    });
  }

  // SOFT DELETE: đánh dấu IsDeleted=true; user vẫn còn trong DB và hiển thị mờ ở trang admin.
  deleteUser(token: string, id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${id}`, {
      headers: this.headers(token),
    });
  }

  // RESTORE: đảo ngược soft delete, user hoạt động lại bình thường.
  restoreUser(token: string, id: number): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(
      `${this.apiUrl}/${id}/restore`,
      {},
      { headers: this.headers(token) }
    );
  }
}
