import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../auth.service';
import {
  Brand,
  CatalogService,
  CreateBrandRequest,
  UpdateBrandRequest,
} from '../../../catalog.service';

@Component({
  selector: 'app-brand-manage',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './brand-manage.component.html',
  styleUrl: './brand-manage.component.scss',
})
export class BrandManageComponent implements OnInit {
  readonly brands = signal<Brand[]>([]);
  readonly loading = signal(false);
  readonly message = signal('');
  readonly error = signal('');

  showModal = false;
  editingId: number | null = null;
  form = { name: '', logoUrl: '' };

  constructor(
    private readonly auth: AuthService,
    private readonly catalog: CatalogService,
  ) {}

  ngOnInit(): void {
    this.loadBrands();
  }

  private getToken(): string | null {
    return this.auth.restoreSession()?.token ?? null;
  }

  loadBrands(): void {
    const token = this.getToken();
    if (!token) return;
    this.loading.set(true);
    this.catalog.getAdminBrands(token).subscribe({
      next: b => { this.brands.set(b); this.loading.set(false); },
      error: () => { this.error.set('Không thể tải danh sách thương hiệu.'); this.loading.set(false); },
    });
  }

  openCreate(): void {
    this.editingId = null;
    this.form = { name: '', logoUrl: '' };
    this.showModal = true;
    this.clearFeedback();
  }

  openEdit(brand: Brand): void {
    this.editingId = brand.id;
    this.form = { name: brand.name, logoUrl: brand.logoUrl ?? '' };
    this.showModal = true;
    this.clearFeedback();
  }

  closeModal(): void { this.showModal = false; }

  submitForm(): void {
    const token = this.getToken();
    if (!token) return;
    this.loading.set(true);

    if (this.editingId) {
      const req: UpdateBrandRequest = { name: this.form.name, logoUrl: this.form.logoUrl || undefined };
      this.catalog.updateBrand(token, this.editingId, req).subscribe({
        next: () => {
          this.message.set('Cập nhật thương hiệu thành công.');
          this.loading.set(false);
          this.closeModal();
          this.loadBrands();
        },
        error: (err) => { this.error.set(err.error?.message ?? 'Lỗi cập nhật.'); this.loading.set(false); },
      });
    } else {
      const req: CreateBrandRequest = { name: this.form.name, logoUrl: this.form.logoUrl || undefined };
      this.catalog.createBrand(token, req).subscribe({
        next: () => {
          this.message.set('Tạo thương hiệu thành công.');
          this.loading.set(false);
          this.closeModal();
          this.loadBrands();
        },
        error: (err) => { this.error.set(err.error?.message ?? 'Lỗi tạo thương hiệu.'); this.loading.set(false); },
      });
    }
  }

  deleteBrand(id: number): void {
    const token = this.getToken();
    if (!token || !confirm('Bạn có chắc chắn muốn ngưng sử dụng thương hiệu này?')) return;
    this.loading.set(true);
    this.catalog.deleteBrand(token, id).subscribe({
      next: (res) => { this.message.set(res.message); this.loading.set(false); this.loadBrands(); },
      error: (err) => { this.error.set(err.error?.message ?? 'Lỗi xóa.'); this.loading.set(false); },
    });
  }

  clearFeedback(): void { this.message.set(''); this.error.set(''); }
}
