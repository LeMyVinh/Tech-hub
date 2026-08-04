import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface AdminUserProfile {
  id: number;
  fullName: string;
  email: string;
  phone: string | null;
  role: string;
  isActive: boolean;
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

  lockUser(token: string, id: number): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(
      `${this.apiUrl}/${id}/lock`,
      {},
      { headers: this.headers(token) }
    );
  }

  unlockUser(token: string, id: number): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(
      `${this.apiUrl}/${id}/unlock`,
      {},
      { headers: this.headers(token) }
    );
  }
}