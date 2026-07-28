import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  Brand,
  CatalogService,
  Category,
  PagedResult,
  ProductSummary,
} from '../../../catalog.service';

@Component({
  selector: 'app-product-list',
  imports: [CommonModule, FormsModule],
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.scss',
})
export class ProductListComponent implements OnInit {
  readonly Math = Math;
  readonly products = signal<PagedResult<ProductSummary> | null>(null);
  readonly categories = signal<Category[]>([]);
  readonly brands = signal<Brand[]>([]);
  readonly loading = signal(false);

  searchKeyword = '';
  selectedCategoryId: number | null = null;
  selectedBrandId: number | null = null;
  minPrice: number | null = null;
  maxPrice: number | null = null;
  sortOption = 'newest';
  currentPage = 1;
  readonly pageSize = 12;

  constructor(
    private readonly catalog: CatalogService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.loadFilters();
    this.loadProducts();
  }

  private loadFilters(): void {
    this.catalog.getCategories().subscribe({
      next: cats => this.categories.set(cats.filter(c => c.isActive)),
      error: () => {},
    });
    this.catalog.getBrands().subscribe({
      next: b => this.brands.set(b.filter(br => br.isActive)),
      error: () => {},
    });
  }

  loadProducts(): void {
    this.loading.set(true);
    this.catalog
      .searchProducts({
        keyword: this.searchKeyword || undefined,
        categoryId: this.selectedCategoryId || undefined,
        brandId: this.selectedBrandId || undefined,
        minPrice: this.minPrice || undefined,
        maxPrice: this.maxPrice || undefined,
        sort: this.sortOption,
        page: this.currentPage,
        pageSize: this.pageSize,
      })
      .subscribe({
        next: res => {
          this.products.set(res);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  applyFilter(): void {
    this.currentPage = 1;
    this.loadProducts();
  }

  resetFilter(): void {
    this.searchKeyword = '';
    this.selectedCategoryId = null;
    this.selectedBrandId = null;
    this.minPrice = null;
    this.maxPrice = null;
    this.sortOption = 'newest';
    this.currentPage = 1;
    this.loadProducts();
  }

  goToPage(page: number): void {
    if (page < 1) return;
    const maxPage = Math.ceil((this.products()?.totalCount ?? 0) / this.pageSize);
    if (page > maxPage) return;
    this.currentPage = page;
    this.loadProducts();
  }

  viewDetail(id: number): void {
    this.router.navigate(['/catalog/products', id]);
  }

  formatVnd(amount: number): string {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
  }
}
