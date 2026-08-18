import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { loadStripe, Stripe, StripeElements } from '@stripe/stripe-js';
import { PaymentService } from '../payment.service';
import { AuthService } from '../../../auth.service';

@Component({
  selector: 'app-credit-card-payment',
  imports: [CommonModule],
  templateUrl: './credit-card-payment.component.html',
  styleUrl: './credit-card-payment.component.scss',
})
export class CreditCardPaymentComponent implements OnInit {
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly error = signal('');
  readonly ready = signal(false);

  private orderId = 0;
  private stripe: Stripe | null = null;
  private elements: StripeElements | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly paymentService: PaymentService,
    private readonly auth: AuthService,
  ) {}

  ngOnInit(): void {
    this.orderId = Number(this.route.snapshot.paramMap.get('orderId'));
    if (!this.orderId) {
      this.error.set('Mã đơn hàng không hợp lệ.');
      this.loading.set(false);
      return;
    }

    const session = this.auth.restoreSession();
    if (!session) {
      this.router.navigate(['/auth/login']);
      return;
    }

    this.paymentService.createCreditCardPayment(session.token, this.orderId).subscribe({
      next: (res) => {
        console.log('[Stripe] API response', res);

        if (!res?.publishableKey || !res?.clientSecret) {
          this.error.set('Server không trả publishableKey/clientSecret. Kiểm tra appsettings Stripe.');
          this.loading.set(false);
          return;
        }

        // Hiện #payment-element trước, rồi mới mount
        this.loading.set(false);

        setTimeout(() => this.mountStripe(res.publishableKey, res.clientSecret), 50);
      },
      error: (err) => {
        console.error('[Stripe] API error', err);
        this.error.set(err.error?.message ?? 'Không thể khởi tạo thanh toán.');
        this.loading.set(false);
      },
    });
  }

  private async mountStripe(publishableKey: string, clientSecret: string): Promise<void> {
    try {
      this.stripe = await loadStripe(publishableKey);
      if (!this.stripe) {
        this.error.set('loadStripe thất bại — PublishableKey sai hoặc bị chặn mạng.');
        return;
      }

      const host = document.getElementById('payment-element');
      if (!host) {
        this.error.set('Không tìm thấy #payment-element trong DOM.');
        return;
      }

      this.elements = this.stripe.elements({ clientSecret });
      const paymentElement = this.elements.create('payment');
      paymentElement.mount('#payment-element');

      paymentElement.on('ready', () => {
        console.log('[Stripe] Payment Element ready');
        this.ready.set(true);
      });
    } catch (e: any) {
      console.error('[Stripe] mount error', e);
      this.error.set(e?.message ?? 'Lỗi mount form Stripe.');
    }
  }

  async pay(): Promise<void> {
    if (!this.stripe || !this.elements || !this.ready()) {
      this.error.set('Form thẻ chưa sẵn sàng.');
      return;
    }

    this.submitting.set(true);
    this.error.set('');

    try {
      const { error } = await this.stripe.confirmPayment({
        elements: this.elements,
        confirmParams: {
          return_url: `${window.location.origin}/payment-result?orderId=${this.orderId}`,
        },
      });

      if (error) {
        this.error.set(error.message ?? 'Thanh toán thất bại.');
        this.submitting.set(false);
      }
    } catch (e: any) {
      this.error.set(e?.message ?? 'Lỗi thanh toán.');
      this.submitting.set(false);
    }
  }
}