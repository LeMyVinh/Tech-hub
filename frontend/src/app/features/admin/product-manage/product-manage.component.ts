import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../auth.service';
import {
  Brand,
  CatalogService,
  Category,
  CreateImageDto,
  CreateProductRequest,
  CreateVariantDto,
  PagedResult,
  ProductSummary,
  UpdateImageDto,
  UpdateProductRequest,
  UpdateVariantDto,
} from '../../../catalog.service';

@Component({
  selector: 'app-product-manage',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './product-manage.component.html',
  styleUrl: './product-manage.component.scss',
})
export class ProductManageComponent implements OnInit {
  readonly products = signal<PagedResult<ProductSummary> | null>(null);
  readonly categories = signal<Category[]>([]);
  readonly brands = signal<Brand[]>([]);
  readonly loading = signal(false);
  readonly message = signal('');
  readonly error = signal('');

  showModal = false;
  editingProductId: number | null = null;
  productForm = {
    name: '',
    description: '',
    categoryId: 0,
    brandId: 0,
    status: 'Active',
    variants: [{ id: undefined as number | undefined, variantName: '', sku: '', price: 0, stockQuantity: 0 }],
    images: [{ id: undefined as number | undefined, imageUrl: '', isPrimary: true }],
  };

  constructor(
    private readonly auth: AuthService,
    private readonly catalog: CatalogService,
  ) {}

  ngOnInit(): void {
    this.loadCategoriesAndBrands();
    this.loadProducts();
  }

  private getToken(): string | null {
    return this.auth.restoreSession()?.token ?? null;
  }

  private loadCategoriesAndBrands(): void {
    this.catalog.getCategories().subscribe({
      next: cats => this.categories.set(cats.filter(c => c.isActive)),
    });
    this.catalog.getBrands().subscribe({
      next: b => this.brands.set(b.filter(br => br.isActive)),
    });
  }

  loadProducts(): void {
    const token = this.getToken();
    if (!token) return;
    this.loading.set(true);
    this.catalog.getAdminProducts(token, { page: 1, pageSize: 50 }).subscribe({
      next: res => { this.products.set(res); this.loading.set(false); },
      error: () => { this.error.set('Không thể tải danh sách sản phẩm.'); this.loading.set(false); },
    });
  }

  openCreate(): void {
    this.editingProductId = null;
    this.productForm = {
      name: '',
      description: '',
      categoryId: this.categories()[0]?.id ?? 0,
      brandId: this.brands()[0]?.id ?? 0,
      status: 'Active',
      variants: [{ id: undefined, variantName: '', sku: '', price: 0, stockQuantity: 0 }],
      images: [{ id: undefined, imageUrl: '', isPrimary: true }],
    };
    this.showModal = true;
    this.clearFeedback();
  }

  openEdit(productId: number): void {
    const token = this.getToken();
    if (!token) return;
    this.loading.set(true);
    this.catalog.getAdminProductDetail(token, productId).subscribe({
      next: detail => {
        this.editingProductId = detail.id;
        this.productForm = {
          name: detail.name,
          description: detail.description ?? '',
          categoryId: detail.categoryId,
          brandId: detail.brandId,
          status: detail.status,
          variants: detail.variants.map(v => ({
            id: v.id, variantName: v.variantName, sku: v.sku, price: v.price, stockQuantity: v.stockQuantity,
          })),
          images: detail.images.map(img => ({
            id: img.id, imageUrl: img.imageUrl, isPrimary: img.isPrimary,
          })),
        };
        if (this.productForm.variants.length === 0) {
          this.productForm.variants.push({ id: undefined, variantName: '', sku: '', price: 0, stockQuantity: 0 });
        }
        if (this.productForm.images.length === 0) {
          this.productForm.images.push({ id: undefined, imageUrl: '', isPrimary: true });
        }
        this.showModal = true;
        this.loading.set(false);
      },
      error: () => { this.error.set('Không thể lấy thông tin sản phẩm.'); this.loading.set(false); },
    });
  }

  closeModal(): void { this.showModal = false; }

  addVariantRow(): void {
    this.productForm.variants.push({ id: undefined, variantName: '', sku: '', price: 0, stockQuantity: 0 });
  }

  removeVariantRow(index: number): void {
    if (this.productForm.variants.length > 1) {
      this.productForm.variants.splice(index, 1);
    }
  }

  addImageRow(): void {
    this.productForm.images.push({ id: undefined, imageUrl: '', isPrimary: this.productForm.images.length === 0 });
  }

  removeImageRow(index: number): void {
    this.productForm.images.splice(index, 1);
  }

  setPrimaryImage(index: number): void {
    this.productForm.images.forEach((img, i) => (img.isPrimary = i === index));
  }

  submitForm(): void {
    const token = this.getToken();
    if (!token) return;
    this.loading.set(true);

    if (this.editingProductId) {
      const req: UpdateProductRequest = {
        name: this.productForm.name,
        description: this.productForm.description,
        categoryId: Number(this.productForm.categoryId),
        brandId: Number(this.productForm.brandId),
        status: this.productForm.status,
        variants: this.productForm.variants.map(v => ({
          id: v.id, variantName: v.variantName, sku: v.sku, price: Number(v.price), stockQuantity: Number(v.stockQuantity),
        })),
        images: this.productForm.images.map(img => ({
          id: img.id, imageUrl: img.imageUrl, isPrimary: img.isPrimary,
        })),
      };
      this.catalog.updateProduct(token, this.editingProductId, req).subscribe({
        next: () => {
          this.message.set('Cập nhật sản phẩm thành công.');
          this.loading.set(false);
          this.closeModal();
          this.loadProducts();
        },
        error: (err) => { this.error.set(err.error?.message ?? 'Lỗi cập nhật.'); this.loading.set(false); },
      });
    } else {
      const req: CreateProductRequest = {
        name: this.productForm.name,
        description: this.productForm.description,
        categoryId: Number(this.productForm.categoryId),
        brandId: Number(this.productForm.brandId),
        status: this.productForm.status,
        variants: this.productForm.variants.map(v => ({
          variantName: v.variantName, sku: v.sku, price: Number(v.price), stockQuantity: Number(v.stockQuantity),
        })),
        images: this.productForm.images.map(img => ({
          imageUrl: img.imageUrl, isPrimary: img.isPrimary,
        })),
      };
      this.catalog.createProduct(token, req).subscribe({
        next: () => {
          this.message.set('Tạo mới sản phẩm thành công.');
          this.loading.set(false);
          this.closeModal();
          this.loadProducts();
        },
        error: (err) => { this.error.set(err.error?.message ?? 'Lỗi tạo sản phẩm.'); this.loading.set(false); },
      });
    }
  }

  deleteProduct(productId: number): void {
    const token = this.getToken();
    if (!token || !confirm('Bạn có chắc chắn muốn ngưng kinh doanh/ẩn sản phẩm này?')) return;
    this.loading.set(true);
    this.catalog.deleteProduct(token, productId).subscribe({
      next: (res) => { this.message.set(res.message); this.loading.set(false); this.loadProducts(); },
      error: (err) => { this.error.set(err.error?.message ?? 'Lỗi xóa.'); this.loading.set(false); },
    });
  }

  clearFeedback(): void { this.message.set(''); this.error.set(''); }
}
