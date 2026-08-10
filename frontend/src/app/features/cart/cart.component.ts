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

    const cart = this.cart();
    if (!cart || cart.items.length === 0) return;

    // FIX (#4 - Clear cart thiếu chi tiết trong lời xác nhận): trước đây confirm() chỉ
    // hỏi chung chung "Xóa toàn bộ sản phẩm khỏi giỏ hàng?" mà không cho người dùng
    // biết đang xóa bao nhiêu sản phẩm / tổng giá trị bao nhiêu, dễ khiến người dùng
    // bấm nhầm mà không lường được hậu quả. Giờ liệt kê rõ số lượng sản phẩm, số loại
    // và tổng giá trị trước khi xác nhận.
    const totalQuantity = cart.items.reduce((sum, item) => sum + item.quantity, 0);
    const confirmMessage =
      `Xóa toàn bộ ${totalQuantity} sản phẩm (${cart.items.length} loại) khỏi giỏ hàng?\n` +
      `Tổng giá trị: ${this.formatVnd(cart.totalAmount)}\n` +
      `Hành động này không thể hoàn tác.`;

    if (!confirm(confirmMessage)) return;

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