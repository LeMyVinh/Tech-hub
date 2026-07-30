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
      const txnRef = params['vnp_TxnRef'];
      if (txnRef) this.orderId.set(Number(txnRef));

      if (Object.keys(params).length === 0) {
        this.loading.set(false);
        this.error.set('Không tìm thấy thông tin thanh toán.');
        return;
      }

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
    });
  }

  retryPayment(): void {
    const orderId = this.orderId();
    const session = this.auth.restoreSession();
    if (!orderId || !session) {
      this.router.navigate(['/auth/login']);
      return;
    }

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
