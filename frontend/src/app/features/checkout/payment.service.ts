import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface CreatePaymentRequest {
  orderId: number;
  method: string;
}

export interface PaymentResponse {
  id: number;
  method: string;
  amount: number;
  status: string;
  transactionCode: string | null;
  paidAt: string | null;
  paymentUrl: string | null;
}

export interface CreditCardPaymentResponse {
  paymentId: number;
  paymentIntentId: string;
  clientSecret: string;
  publishableKey: string;
}

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly apiUrl = 'http://localhost:5159/api/v1/payments';

  constructor(private readonly http: HttpClient) {}

  createPayment(token: string, request: CreatePaymentRequest): Observable<PaymentResponse> {
    return this.http.post<PaymentResponse>(this.apiUrl, request, {
      headers: new HttpHeaders({ Authorization: `Bearer ${token}` }),
    });
  }

  createCreditCardPayment(token: string, orderId: number): Observable<CreditCardPaymentResponse> {
    return this.http.post<CreditCardPaymentResponse>(
      `${this.apiUrl}/credit-card`,
      { orderId },
      { headers: new HttpHeaders({ Authorization: `Bearer ${token}` }) }
    );
  }

  // Được gọi từ trang payment-result (sau khi VNPay redirect trình duyệt về);
  // truyền lại nguyên vẹn các tham số vnp_* để backend xác thực chữ ký (HMAC-SHA512) và cập nhật trạng thái.
  processVnpayCallback(params: Record<string, string>): Observable<PaymentResponse> {
    return this.http.post<PaymentResponse>(`${this.apiUrl}/vnpay/callback`, null, { params });
  }

  getPaymentByOrder(token: string, orderId: number): Observable<PaymentResponse> {
    return this.http.get<PaymentResponse>(`${this.apiUrl}/order/${orderId}`, {
      headers: new HttpHeaders({ Authorization: `Bearer ${token}` }),
    });
  }
}