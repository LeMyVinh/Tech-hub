import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import {
  CatalogService,
  ProductDetail,
  ProductVariant,
} from '../../../catalog.service';
import { AuthService } from '../../../auth.service';
import { CartService } from '../../cart/cart.service';
import { WishlistService } from '../../wishlist/wishlist.service';
import { ToastService } from '../../../shared/toast/toast.service';

@Component({
  selector: 'app-product-detail',
  imports: [CommonModule],
  templateUrl: './product-detail.component.html',
  styleUrl: './product-detail.component.scss',
})
export class ProductDetailComponent implements OnInit {
  readonly product = signal<ProductDetail | null>(null);
  readonly selectedVariant = signal<ProductVariant | null>(null);
  readonly selectedImageIndex = signal(0);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly activeTab = signal<'description' | 'reviews'>('description');
  readonly addingToCart = signal(false);
  readonly addingToWishlist = signal(false);
  readonly isInWishlist = signal(false);

  readonly quantity = signal(1);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly catalog: CatalogService,
    private readonly cartService: CartService,
    private readonly wishlistService: WishlistService,
    private readonly auth: AuthService,
    private readonly toast: ToastService,
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (id) this.loadProduct(id);
  }

  private loadProduct(id: number): void {
    this.loading.set(true);
    this.catalog.getProductDetail(id).subscribe({
      next: detail => {
        this.product.set(detail);
        this.selectedVariant.set(detail.variants[0] ?? null);
        this.selectedImageIndex.set(0);
        this.quantity.set(1);
        this.loading.set(false);
        this.checkWishlistStatus(id);
      },
      error: () => {
        this.error.set('Không tìm thấy sản phẩm.');
        this.loading.set(false);
      },
    });
  }

  private checkWishlistStatus(productId: number): void {
    const session = this.auth.restoreSession();
    if (!session) {
      this.isInWishlist.set(false);
      return;
    }
    this.wishlistService.getWishlist(session.token).subscribe({
      next: wishlist => {
        this.isInWishlist.set(wishlist.items.some(item => item.productId === productId));
      },
      error: () => {},
    });
  }

  selectVariant(v: ProductVariant): void {
    this.selectedVariant.set(v);
    this.quantity.set(1);
  }

  increaseQuantity(): void {
    const v = this.selectedVariant();
    if (!v) return;
    if (this.quantity() < v.stockQuantity) {
      this.quantity.set(this.quantity() + 1);
    }
  }

  decreaseQuantity(): void {
    if (this.quantity() > 1) {
      this.quantity.set(this.quantity() - 1);
    }
  }

  selectImage(idx: number): void {
    this.selectedImageIndex.set(idx);
  }

  goBack(): void {
    this.router.navigate(['/catalog/products']);
  }

  formatVnd(amount: number): string {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
  }

  getStarArray(rating: number): number[] {
    return Array.from({ length: 5 }, (_, i) => i < Math.round(rating) ? 1 : 0);
  }

  addToCart(): void {
    const variant = this.selectedVariant();
    if (!variant || variant.stockQuantity <= 0) return;

    const session = this.auth.restoreSession();
    if (!session) {
      this.router.navigate(['/auth/login']);
      return;
    }

    this.addingToCart.set(true);
    this.cartService.addToCart(session.token, variant.id, this.quantity()).subscribe({
      next: () => {
        this.addingToCart.set(false);
        // FIX (bug report #8): dùng ToastService thay vì alert() để đồng nhất UX
        // với các trang khác (account, wishlist) và không chặn UI thread.
        this.toast.success(`Đã thêm ${this.quantity()} sản phẩm (${variant.variantName}) vào giỏ hàng!`);
      },
      error: () => {
        this.addingToCart.set(false);
        this.toast.error('Thêm vào giỏ hàng thất bại, vui lòng thử lại.');
      },
    });
  }

  toggleWishlist(): void {
    const p = this.product();
    if (!p) return;

    const session = this.auth.restoreSession();
    if (!session) {
      this.router.navigate(['/auth/login']);
      return;
    }

    this.addingToWishlist.set(true);

    if (this.isInWishlist()) {
      this.wishlistService.removeFromWishlist(session.token, p.id).subscribe({
        next: () => {
          this.isInWishlist.set(false);
          this.addingToWishlist.set(false);
        },
        error: () => {
          this.addingToWishlist.set(false);
          this.toast.error('Xóa khỏi danh sách yêu thích thất bại, vui lòng thử lại.');
        },
      });
    } else {
      this.wishlistService.addToWishlist(session.token, p.id).subscribe({
        next: () => {
          this.isInWishlist.set(true);
          this.addingToWishlist.set(false);
        },
        error: err => {
          this.addingToWishlist.set(false);
          this.toast.error(err.error?.message ?? 'Thêm vào danh sách yêu thích thất bại, vui lòng thử lại.');
        },
      });
    }
  }
}