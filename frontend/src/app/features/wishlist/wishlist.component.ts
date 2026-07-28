import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../auth.service';
import { WishlistItem, WishlistResponse, WishlistService } from './wishlist.service';

@Component({
  selector: 'app-wishlist',
  imports: [CommonModule, RouterLink],
  templateUrl: './wishlist.component.html',
  styleUrl: './wishlist.component.scss',
})
export class WishlistComponent implements OnInit {
  readonly wishlist = signal<WishlistResponse | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly message = signal('');

  constructor(
    private readonly auth: AuthService,
    private readonly wishlistService: WishlistService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.loadWishlist();
  }

  private getToken(): string | null {
    return this.auth.restoreSession()?.token ?? null;
  }

  loadWishlist(): void {
    const token = this.getToken();
    if (!token) {
      this.router.navigate(['/auth/login']);
      return;
    }
    this.loading.set(true);
    this.wishlistService.getWishlist(token).subscribe({
      next: (wishlist) => {
        this.wishlist.set(wishlist);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Không thể tải danh sách yêu thích.');
        this.loading.set(false);
      },
    });
  }

  removeFromWishlist(item: WishlistItem): void {
    const token = this.getToken();
    if (!token) return;
    if (!confirm(`Xóa "${item.productName}" khỏi danh sách yêu thích?`)) return;
    this.loading.set(true);
    this.wishlistService.removeFromWishlist(token, item.productId).subscribe({
      next: (wishlist) => {
        this.wishlist.set(wishlist);
        this.loading.set(false);
        this.message.set('Đã xóa khỏi danh sách yêu thích.');
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'Lỗi xóa.');
        this.loading.set(false);
      },
    });
  }

  moveToCart(item: WishlistItem): void {
    const token = this.getToken();
    if (!token) return;
    this.loading.set(true);
    this.wishlistService.moveToCart(token, item.productId).subscribe({
      next: () => {
        this.loadWishlist();
        this.message.set('Đã chuyển sang giỏ hàng.');
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'Lỗi chuyển sang giỏ hàng.');
        this.loading.set(false);
      },
    });
  }

  viewProduct(productId: number): void {
    this.router.navigate(['/catalog/products', productId]);
  }

  formatVnd(amount: number): string {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
  }
}
