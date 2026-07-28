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

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly catalog: CatalogService,
    private readonly cartService: CartService,
    private readonly auth: AuthService,
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
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Không tìm thấy sản phẩm.');
        this.loading.set(false);
      },
    });
  }

  selectVariant(v: ProductVariant): void {
    this.selectedVariant.set(v);
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
    this.cartService.addToCart(session.token, variant.id, 1).subscribe({
      next: () => {
        this.addingToCart.set(false);
        alert('Đã thêm vào giỏ hàng!');
      },
      error: () => {
        this.addingToCart.set(false);
        alert('Thêm vào giỏ hàng thất bại, vui lòng thử lại.');
      },
    });
  }
}