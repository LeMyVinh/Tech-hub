import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../auth.service';
import { CartItem, CartResponse, CartService } from './cart.service';

@Component({
  selector: 'app-cart',
  imports: [CommonModule, RouterLink],
  templateUrl: './cart.component.html',
  styleUrl: './cart.component.scss',
})
export class CartComponent implements OnInit {
  readonly cart = signal<CartResponse | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');

  constructor(
    private readonly auth: AuthService,
    private readonly cartService: CartService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.loadCart();
  }

  private getToken(): string | null {
    return this.auth.restoreSession()?.token ?? null;
  }

  loadCart(): void {
    const token = this.getToken();
    if (!token) {
      this.router.navigate(['/auth/login']);
      return;
    }
    this.loading.set(true);
    this.cartService.getCart(token).subscribe({
      next: (cart) => {
        this.cart.set(cart);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Không thể tải giỏ hàng.');
        this.loading.set(false);
      },
    });
  }

  updateQuantity(item: CartItem, change: number): void {
    const token = this.getToken();
    if (!token) return;
    const newQuantity = item.quantity + change;
    if (newQuantity < 1) return;
    if (newQuantity > item.stockQuantity) {
      this.error.set('Số lượng tồn kho không đủ.');
      return;
    }
    this.loading.set(true);
    this.cartService.updateCartItem(token, item.id, newQuantity).subscribe({
      next: (cart) => {
        this.cart.set(cart);
        this.loading.set(false);
        this.error.set('');
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'Lỗi cập nhật.');
        this.loading.set(false);
      },
    });
  }

  removeItem(item: CartItem): void {
    const token = this.getToken();
    if (!token) return;
    if (!confirm(`Xóa "${item.productName}" khỏi giỏ hàng?`)) return;
    this.loading.set(true);
    this.cartService.removeFromCart(token, item.id).subscribe({
      next: (cart) => {
        this.cart.set(cart);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'Lỗi xóa.');
        this.loading.set(false);
      },
    });
  }

  clearCart(): void {
    const token = this.getToken();
    if (!token) return;
    if (!confirm('Xóa toàn bộ sản phẩm khỏi giỏ hàng?')) return;
    this.loading.set(true);
    this.cartService.clearCart(token).subscribe({
      next: (cart) => {
        this.cart.set(cart);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'Lỗi xóa.');
        this.loading.set(false);
      },
    });
  }

  checkout(): void {
    this.router.navigate(['/checkout']);
  }

  formatVnd(amount: number): string {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
  }
}
