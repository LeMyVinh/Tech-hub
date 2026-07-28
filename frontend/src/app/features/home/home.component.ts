import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../auth.service';
import { CatalogService, Category, ProductSummary } from '../../catalog.service';

@Component({
  selector: 'app-home',
  imports: [CommonModule, RouterLink],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
})
export class HomeComponent implements OnInit {
  readonly categories = signal<Category[]>([]);
  readonly featuredProducts = signal<ProductSummary[]>([]);
  readonly loading = signal(false);

  constructor(
    private readonly auth: AuthService,
    private readonly catalog: CatalogService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.catalog.getCategories().subscribe({
      next: cats => this.categories.set(cats.filter(c => c.isActive).slice(0, 6)),
      error: () => {},
    });

    this.loading.set(true);
    this.catalog.searchProducts({ sort: 'newest', page: 1, pageSize: 8 }).subscribe({
      next: res => {
        this.featuredProducts.set(res.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  cartLink(): string {
    return this.auth.restoreSession() ? '/cart' : '/auth/login';
  }

  viewProduct(id: number): void {
    this.router.navigate(['/catalog/products', id]);
  }

  formatVnd(amount: number): string {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
  }
}