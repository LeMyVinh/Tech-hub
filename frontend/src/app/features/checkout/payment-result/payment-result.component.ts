import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../auth.service';
import { PaymentResponse, PaymentService } from '../payment.service';

@Component({
  selector: 'app-payment-result',
  imports: [CommonModule, RouterLink],
  templateUrl: './payment-result.component.html',
  styleUrl: './payment-result.component.scss',
})
export class PaymentResultComponent implements OnInit {
  readonly loading = signal(true);
  readonly error = signal('');
  readonly payment = signal<PaymentResponse | null>(null);
  readonly orderId = signal<number | null>(null);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly paymentService: PaymentService,
    private readonly auth: AuthService,
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      // --- Case 1: VNPay redirect về kèm các tham số vnp_* ---
      const txnRef = params['vnp_TxnRef'];
      if (txnRef) {
        this.orderId.set(Number(txnRef));
        this.handleVnpayReturn(params);
        return;
      }

      // --- Case 2: Stripe (Credit Card) redirect về sau khi confirmPayment() ---
      // Stripe tự thêm các tham số: payment_intent, payment_intent_client_secret, redirect_status.
      // orderId được mình tự gắn thêm vào return_url lúc gọi confirmPayment() (xem
      // credit-card-payment.component.ts), KHÔNG lấy trạng thái thành công/thất bại
      // trực tiếp từ URL — chỉ dùng nó để biết gọi API nào, trạng thái THẬT luôn lấy
      // từ backend (đã được webhook Stripe cập nhật trước đó).
      const paymentIntentId = params['payment_intent'];
      const orderIdParam = params['orderId'];
      if (paymentIntentId && orderIdParam) {
        this.orderId.set(Number(orderIdParam));
        this.handleStripeReturn(Number(orderIdParam));
        return;
      }

      // --- Case 3: Không có tham số nào hợp lệ ---
      this.loading.set(false);
      this.error.set('Không tìm thấy thông tin thanh toán.');
    });
  }

  private handleVnpayReturn(params: Record<string, string>): void {
    // VNPay redirect trình duyệt kèm các tham số vnp_*; gửi lại nguyên vẹn cho backend
    // để xác thực chữ ký (HMAC-SHA512) và cập nhật trạng thái Payment/Order.
    this.paymentService.processVnpayCallback(params).subscribe({
      next: (res) => {
        this.payment.set(res);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'Không thể xác nhận kết quả thanh toán.');
        this.loading.set(false);
      },
    });
  }

  private handleStripeReturn(orderId: number): void {
    const session = this.auth.restoreSession();
    if (!session) {
      this.router.navigate(['/auth/login']);
      return;
    }

    // Không tự suy luận thành công/thất bại từ query param redirect_status — luôn hỏi
    // lại backend, vì trạng thái thật đã (hoặc sắp) được Stripe webhook cập nhật.
    // Webhook có thể đến trễ vài giây so với lúc trình duyệt redirect về, nên poll
    // vài lần nếu vẫn đang Pending.
    this.pollPaymentStatus(session.token, orderId, 0);
  }

  private pollPaymentStatus(token: string, orderId: number, attempt: number): void {
    const maxAttempts = 5;
    const delayMs = 1500;

    this.paymentService.getPaymentByOrder(token, orderId).subscribe({
      next: (res) => {
        if (res.status === 'Pending' && attempt < maxAttempts) {
          setTimeout(() => this.pollPaymentStatus(token, orderId, attempt + 1), delayMs);
          return;
        }
        this.payment.set(res);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'Không thể xác nhận kết quả thanh toán.');
        this.loading.set(false);
      },
    });
  }

  retryPayment(): void {
    const orderId = this.orderId();
    const session = this.auth.restoreSession();
    if (!orderId || !session) {
      this.router.navigate(['/auth/login']);
      return;
    }

    const payment = this.payment();

    // Nếu là Credit Card thất bại -> quay lại trang nhập thẻ để thử lại (Stripe cho
    // phép tạo confirm lại trên cùng PaymentIntent nếu còn hiệu lực).
    if (payment?.method === 'CreditCard') {
      this.router.navigate(['/checkout/pay', orderId]);
      return;
    }

    // VNPay thất bại -> tạo lại payment VNPay như cũ
    this.loading.set(true);
    this.error.set('');
    this.paymentService.createPayment(session.token, { orderId, method: 'VNPay' }).subscribe({
      next: (res) => {
        if (res.paymentUrl) {
          window.location.href = res.paymentUrl;
        } else {
          this.loading.set(false);
        }
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'Không thể khởi tạo lại thanh toán.');
        this.loading.set(false);
      },
    });
  }
}