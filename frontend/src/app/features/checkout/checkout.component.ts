import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../auth.service';
import { CartResponse, CartService } from '../cart/cart.service';
import { Address, CheckoutService, CreateOrderRequest } from './checkout.service';

@Component({
  selector: 'app-checkout',
  imports: [CommonModule, FormsModule],
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.scss',
})
export class CheckoutComponent implements OnInit {
  readonly cart = signal<CartResponse | null>(null);
  readonly addresses = signal<Address[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');

  selectedAddressId: number | null = null;
  shippingMethod = 'Standard';
  note = '';

  constructor(
    private readonly auth: AuthService,
    private readonly cartService: CartService,
    private readonly checkoutService: CheckoutService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  private getToken(): string | null {
    return this.auth.restoreSession()?.token ?? null;
  }

  private loadData(): void {
    const token = this.getToken();
    if (!token) {
      this.router.navigate(['/auth/login']);
      return;
    }

    this.loading.set(true);

    // Load cart
    this.cartService.getCart(token).subscribe({
      next: (cart) => {
        if (cart.items.length === 0) {
          this.router.navigate(['/cart']);
          return;
        }
        this.cart.set(cart);
      },
      error: () => {
        this.error.set('Không thể tải giỏ hàng.');
        this.loading.set(false);
      },
    });

    // Load addresses
    this.checkoutService.getAddresses(token).subscribe({
      next: (addresses) => {
        this.addresses.set(addresses);
        const defaultAddr = addresses.find(a => a.isDefault);
        if (defaultAddr) this.selectedAddressId = defaultAddr.id;
        else if (addresses.length > 0) this.selectedAddressId = addresses[0].id;
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Không thể tải địa chỉ.');
        this.loading.set(false);
      },
    });
  }

  submitOrder(): void {
    const token = this.getToken();
    if (!token || !this.selectedAddressId) return;

    this.loading.set(true);
    this.error.set('');

    const request: CreateOrderRequest = {
      addressId: this.selectedAddressId,
      shippingMethod: this.shippingMethod,
      note: this.note || undefined,
    };

    this.checkoutService.createOrder(token, request).subscribe({
      next: (order) => {
        this.router.navigate(['/orders', order.id]);
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'Lỗi tạo đơn hàng.');
        this.loading.set(false);
      },
    });
  }

  formatVnd(amount: number): string {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
  }
}
