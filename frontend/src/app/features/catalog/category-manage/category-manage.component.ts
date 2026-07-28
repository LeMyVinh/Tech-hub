import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../auth.service';
import {
  CatalogService,
  Category,
  CreateCategoryRequest,
  UpdateCategoryRequest,
} from '../../../catalog.service';

@Component({
  selector: 'app-category-manage',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './category-manage.component.html',
  styleUrl: './category-manage.component.scss',
})
export class CategoryManageComponent implements OnInit {
  readonly categories = signal<Category[]>([]);
  readonly loading = signal(false);
  readonly message = signal('');
  readonly error = signal('');

  showModal = false;
  editingId: number | null = null;
  form = { name: '', parentId: null as number | null };

  constructor(
    private readonly auth: AuthService,
    private readonly catalog: CatalogService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.loadCategories();
  }

  private getToken(): string | null {
    return this.auth.restoreSession()?.token ?? null;
  }

  loadCategories(): void {
    const token = this.getToken();
    if (!token) return;
    this.loading.set(true);
    this.catalog.getAdminCategories(token).subscribe({
      next: cats => { this.categories.set(cats); this.loading.set(false); },
      error: () => { this.error.set('Không thể tải danh sách danh mục.'); this.loading.set(false); },
    });
  }

  openCreate(): void {
    this.editingId = null;
    this.form = { name: '', parentId: null };
    this.showModal = true;
    this.clearFeedback();
  }

  openEdit(cat: Category): void {
    this.editingId = cat.id;
    this.form = { name: cat.name, parentId: cat.parentId ?? null };
    this.showModal = true;
    this.clearFeedback();
  }

  closeModal(): void { this.showModal = false; }

  submitForm(): void {
    const token = this.getToken();
    if (!token) return;
    this.loading.set(true);

    if (this.editingId) {
      const req: UpdateCategoryRequest = { name: this.form.name, parentId: this.form.parentId };
      this.catalog.updateCategory(token, this.editingId, req).subscribe({
        next: () => {
          this.message.set('Cập nhật danh mục thành công.');
          this.loading.set(false);
          this.closeModal();
          this.loadCategories();
        },
        error: (err) => { this.error.set(err.error?.message ?? 'Lỗi cập nhật.'); this.loading.set(false); },
      });
    } else {
      const req: CreateCategoryRequest = { name: this.form.name, parentId: this.form.parentId };
      this.catalog.createCategory(token, req).subscribe({
        next: () => {
          this.message.set('Tạo danh mục thành công.');
          this.loading.set(false);
          this.closeModal();
          this.loadCategories();
        },
        error: (err) => { this.error.set(err.error?.message ?? 'Lỗi tạo danh mục.'); this.loading.set(false); },
      });
    }
  }

  deleteCategory(id: number): void {
    const token = this.getToken();
    if (!token || !confirm('Bạn có chắc chắn muốn ngưng sử dụng danh mục này?')) return;
    this.loading.set(true);
    this.catalog.deleteCategory(token, id).subscribe({
      next: (res) => { this.message.set(res.message); this.loading.set(false); this.loadCategories(); },
      error: (err) => { this.error.set(err.error?.message ?? 'Lỗi xóa.'); this.loading.set(false); },
    });
  }

  goToAdminProducts(): void { this.router.navigate(['/admin/products']); }
  goToAdminBrands(): void { this.router.navigate(['/catalog/brands']); }

  clearFeedback(): void { this.message.set(''); this.error.set(''); }
}
