import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

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

export interface CreateOrderRequest {
  addressId: number;
  shippingMethod: string;
  paymentMethod: string;
  note?: string;
}

export interface OrderResponse {
  id: number;
  orderCode: string;
  totalAmount: number;
  status: string;
  shippingMethod: string;
  cancelReason: string | null;
  items: OrderItem[];
  createdAt: string;
  paymentUrl: string | null;
}

export interface OrderItem {
  id: number;
  variantId: number;
  productName: string;
  variantName: string;
  quantity: number;
  unitPrice: number;
  subtotal: number;
}

@Injectable({ providedIn: 'root' })
export class CheckoutService {
  private readonly apiUrl = 'http://localhost:5159/api/v1';

  constructor(private readonly http: HttpClient) {}

  getAddresses(token: string): Observable<Address[]> {
    return this.http.get<Address[]>(`${this.apiUrl}/users/me/addresses`, {
      headers: new HttpHeaders({ Authorization: `Bearer ${token}` }),
    });
  }

  addAddress(token: string, request: AddAddressRequest): Observable<Address> {
    return this.http.post<Address>(`${this.apiUrl}/users/me/addresses`, request, {
      headers: new HttpHeaders({ Authorization: `Bearer ${token}` }),
    });
  }

  createOrder(token: string, request: CreateOrderRequest): Observable<OrderResponse> {
    return this.http.post<OrderResponse>(`${this.apiUrl}/orders`, request, {
      headers: new HttpHeaders({ Authorization: `Bearer ${token}` }),
    });
  }
}