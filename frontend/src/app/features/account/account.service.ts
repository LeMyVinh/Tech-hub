import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface UserProfile {
  id: number;
  fullName: string;
  email: string;
  phone: string | null;
  role: string;
  isDeleted: boolean;
  deletedAt: string | null;
  createdAt: string;
}

export interface UpdateUserProfileRequest {
  fullName: string;
  phone: string;
}

export interface Address {
  id: number;
  recipientName: string;
  phone: string;
  detailAddress: string;
  ward: string;
  district: string;
  province: string;
  isDefault: boolean;
}

export interface AddAddressRequest {
  recipientName: string;
  phone: string;
  detailAddress: string;
  ward: string;
  district: string;
  province: string;
  isDefault: boolean;
}

export interface UpdateAddressRequest {
  recipientName: string;
  phone: string;
  detailAddress: string;
  ward: string;
  district: string;
  province: string;
  isDefault: boolean;
}

@Injectable({ providedIn: 'root' })
export class AccountService {
  private readonly apiUrl = 'http://localhost:5159/api/v1/users/me';

  constructor(private readonly http: HttpClient) {}

  private headers(token: string): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }

  getProfile(token: string): Observable<UserProfile> {
    return this.http.get<UserProfile>(this.apiUrl, { headers: this.headers(token) });
  }

  updateProfile(token: string, request: UpdateUserProfileRequest): Observable<UserProfile> {
    return this.http.put<UserProfile>(this.apiUrl, request, { headers: this.headers(token) });
  }

  getAddresses(token: string): Observable<Address[]> {
    return this.http.get<Address[]>(`${this.apiUrl}/addresses`, { headers: this.headers(token) });
  }

  addAddress(token: string, request: AddAddressRequest): Observable<Address> {
    return this.http.post<Address>(`${this.apiUrl}/addresses`, request, { headers: this.headers(token) });
  }

  updateAddress(token: string, id: number, request: UpdateAddressRequest): Observable<Address> {
    return this.http.put<Address>(`${this.apiUrl}/addresses/${id}`, request, { headers: this.headers(token) });
  }

  deleteAddress(token: string, id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/addresses/${id}`, {
      headers: this.headers(token),
    });
  }

  setDefaultAddress(token: string, id: number): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(
      `${this.apiUrl}/addresses/${id}/default`,
      {},
      { headers: this.headers(token) }
    );
  }
}