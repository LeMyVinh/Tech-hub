import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface OrderItem {
  id: number;
  variantId: number;
  productId: number;
  productName: string;
  variantName: string;
  quantity: number;
  unitPrice: number;
  subtotal: number;
  // FIX: trạng thái đánh giá thật lấy từ backend (bảng Review), không còn để
  // frontend tự đoán bằng signal cục bộ (bug: mất trạng thái sau khi F5 trang).
  hasReviewed: boolean;
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
  customerName?: string | null;
}

export interface OrderListResponse {
  orders: OrderResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AddressResponse {
  id: number;
  recipientName: string;
  phone: string;
  detailAddress: string;
  ward: string;
  district: string;
  province: string;
}

export interface PaymentResponse {
  id: number;
  method: string;
  amount: number;
  status: string;
  transactionCode: string | null;
  paidAt: string | null;
}

export interface OrderStatusLogResponse {
  status: string;
  note: string | null;
  changedAt: string;
}

export interface OrderDetailResponse extends OrderResponse {
  address: AddressResponse;
  payment: PaymentResponse | null;
  statusHistory: OrderStatusLogResponse[];
  updatedAt: string;
}

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly apiUrl = 'http://localhost:5159/api/v1';

  constructor(private readonly http: HttpClient) {}

  getUserOrders(token: string, page: number = 1, pageSize: number = 10): Observable<OrderListResponse> {
    return this.http.get<OrderListResponse>(`${this.apiUrl}/orders?page=${page}&pageSize=${pageSize}`, {
      headers: new HttpHeaders({ Authorization: `Bearer ${token}` }),
    });
  }

  getOrderDetail(token: string, orderId: number): Observable<OrderDetailResponse> {
    return this.http.get<OrderDetailResponse>(`${this.apiUrl}/orders/${orderId}`, {
      headers: new HttpHeaders({ Authorization: `Bearer ${token}` }),
    });
  }

  cancelOrder(token: string, orderId: number, reason?: string): Observable<OrderResponse> {
    return this.http.put<OrderResponse>(
      `${this.apiUrl}/orders/${orderId}/cancel`,
      { reason },
      { headers: new HttpHeaders({ Authorization: `Bearer ${token}` }) }
    );
  }

  getAllOrders(token: string, page: number = 1, pageSize: number = 10, status?: string): Observable<OrderListResponse> {
    let url = `${this.apiUrl}/orders?page=${page}&pageSize=${pageSize}`;
    if (status) url += `&status=${status}`;
    return this.http.get<OrderListResponse>(url, {
      headers: new HttpHeaders({ Authorization: `Bearer ${token}` }),
    });
  }

  updateOrderStatus(token: string, orderId: number, status: string, note?: string): Observable<OrderResponse> {
    return this.http.put<OrderResponse>(
      `${this.apiUrl}/orders/${orderId}/status`,
      { status, note },
      { headers: new HttpHeaders({ Authorization: `Bearer ${token}` }) }
    );
  }
}