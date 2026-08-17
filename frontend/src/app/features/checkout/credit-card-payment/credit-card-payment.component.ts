import { Component, Input, OnInit, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
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
  @Input({ required: true }) orderId!: number;
  @Output() paymentError = new EventEmitter<string>();

  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly error = signal('');

  private stripe: Stripe | null = null;
  private elements: StripeElements | null = null;

  constructor(
    private readonly paymentService: PaymentService,
    private readonly auth: AuthService,
  ) {}

  async ngOnInit(): Promise<void> {
    const session = this.auth.restoreSession();
    if (!session) return;

    this.paymentService.createCreditCardPayment(session.token, this.orderId).subscribe({
      next: async (res) => {
        this.stripe = await loadStripe(res.publishableKey);
        if (!this.stripe) {
          this.error.set('Không thể tải cổng thanh toán.');
          this.loading.set(false);
          return;
        }

        this.elements = this.stripe.elements({ clientSecret: res.clientSecret });
        this.elements.create('payment').mount('#payment-element');
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'Không thể khởi tạo thanh toán.');
        this.loading.set(false);
      },
    });
  }

  async pay(): Promise<void> {
    if (!this.stripe || !this.elements) return;
    this.submitting.set(true);
    this.error.set('');

    const { error } = await this.stripe.confirmPayment({
      elements: this.elements,
      confirmParams: {
        return_url: `${window.location.origin}/payment-result?orderId=${this.orderId}`,
      },
    });

    if (error) {
      this.error.set(error.message ?? 'Thanh toán thất bại.');
      this.paymentError.emit(error.message ?? 'Thanh toán thất bại.');
      this.submitting.set(false);
    }
    // Thành công: trình duyệt tự redirect sang return_url, không cần xử lý thêm ở đây.
  }
}