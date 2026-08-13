import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface Category {
  id: number;
  name: string;
  parentId?: number;
  parentName?: string;
  isActive: boolean;
}

export interface Brand {
  id: number;
  name: string;
  logoUrl?: string;
  isActive: boolean;
}

export interface ProductVariant {
  id: number;
  variantName: string;
  sku: string;
  price: number;
  stockQuantity: number;
}

export interface ProductImage {
  id: number;
  imageUrl: string;
  isPrimary: boolean;
}

export interface ProductSummary {
  id: number;
  name: string;
  categoryName: string;
  brandName: string;
  minPrice: number;
  maxPrice: number;
  primaryImageUrl?: string;
  status: string;
}

export interface ApprovedReview {
  id: number;
  userName: string;
  rating: number;
  comment?: string;
  createdAt: string;
}

export interface ProductDetail {
  id: number;
  name: string;
  description?: string;
  categoryId: number;
  categoryName: string;
  brandId: number;
  brandName: string;
  status: string;
  variants: ProductVariant[];
  images: ProductImage[];
  avgRating: number;
  reviewCount: number;
  reviews: ApprovedReview[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CreateCategoryRequest {
  name: string;
  parentId?: number | null;
}

export interface UpdateCategoryRequest {
  name: string;
  parentId?: number | null;
}

export interface CreateBrandRequest {
  name: string;
  logoUrl?: string;
}

export interface UpdateBrandRequest {
  name: string;
  logoUrl?: string;
}

export interface CreateVariantDto {
  variantName: string;
  sku: string;
  price: number;
  stockQuantity: number;
}

export interface UpdateVariantDto {
  id?: number;
  variantName: string;
  sku: string;
  price: number;
  stockQuantity: number;
}

export interface CreateImageDto {
  imageUrl: string;
  isPrimary: boolean;
}

export interface UpdateImageDto {
  id?: number;
  imageUrl: string;
  isPrimary: boolean;
}

export interface CreateProductRequest {
  name: string;
  description?: string;
  categoryId: number;
  brandId: number;
  variants: CreateVariantDto[];
  images?: CreateImageDto[];
  status?: string;
}

export interface UpdateProductRequest {
  name: string;
  description?: string;
  categoryId: number;
  brandId: number;
  variants: UpdateVariantDto[];
  images?: UpdateImageDto[];
  status?: string;
}

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly baseUrl = 'http://localhost:5159/api/v1';

  constructor(private readonly http: HttpClient) {}

  private getAuthHeaders(token?: string): HttpHeaders {
    let headers = new HttpHeaders();
    if (token) {
      headers = headers.set('Authorization', `Bearer ${token}`);
    }
    return headers;
  }

  // --- CATEGORIES ---
  getCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.baseUrl}/categories`);
  }

  getAdminCategories(token: string): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.baseUrl}/admin/categories`, {
      headers: this.getAuthHeaders(token),
    });
  }

  createCategory(token: string, req: CreateCategoryRequest): Observable<Category> {
    return this.http.post<Category>(`${this.baseUrl}/admin/categories`, req, {
      headers: this.getAuthHeaders(token),
    });
  }

  updateCategory(token: string, id: number, req: UpdateCategoryRequest): Observable<Category> {
    return this.http.put<Category>(`${this.baseUrl}/admin/categories/${id}`, req, {
      headers: this.getAuthHeaders(token),
    });
  }

  deleteCategory(token: string, id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.baseUrl}/admin/categories/${id}`, {
      headers: this.getAuthHeaders(token),
    });
  }

  // --- BRANDS ---
  getBrands(): Observable<Brand[]> {
    return this.http.get<Brand[]>(`${this.baseUrl}/brands`);
  }

  getAdminBrands(token: string): Observable<Brand[]> {
    return this.http.get<Brand[]>(`${this.baseUrl}/admin/brands`, {
      headers: this.getAuthHeaders(token),
    });
  }

  createBrand(token: string, req: CreateBrandRequest): Observable<Brand> {
    return this.http.post<Brand>(`${this.baseUrl}/admin/brands`, req, {
      headers: this.getAuthHeaders(token),
    });
  }

  updateBrand(token: string, id: number, req: UpdateBrandRequest): Observable<Brand> {
    return this.http.put<Brand>(`${this.baseUrl}/admin/brands/${id}`, req, {
      headers: this.getAuthHeaders(token),
    });
  }

  deleteBrand(token: string, id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.baseUrl}/admin/brands/${id}`, {
      headers: this.getAuthHeaders(token),
    });
  }

  // --- PRODUCTS ---
  searchProducts(paramsObj: {
    keyword?: string;
    categoryId?: number;
    brandId?: number;
    minPrice?: number;
    maxPrice?: number;
    sort?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PagedResult<ProductSummary>> {
    let params = new HttpParams();
    if (paramsObj.keyword) params = params.set('keyword', paramsObj.keyword);
    if (paramsObj.categoryId) params = params.set('categoryId', paramsObj.categoryId);
    if (paramsObj.brandId) params = params.set('brandId', paramsObj.brandId);
    if (paramsObj.minPrice != null) params = params.set('minPrice', paramsObj.minPrice);
    if (paramsObj.maxPrice != null) params = params.set('maxPrice', paramsObj.maxPrice);
    if (paramsObj.sort) params = params.set('sort', paramsObj.sort);
    if (paramsObj.page) params = params.set('page', paramsObj.page);
    if (paramsObj.pageSize) params = params.set('pageSize', paramsObj.pageSize);

    return this.http.get<PagedResult<ProductSummary>>(`${this.baseUrl}/products`, { params });
  }

  getProductDetail(id: number): Observable<ProductDetail> {
    return this.http.get<ProductDetail>(`${this.baseUrl}/products/${id}`);
  }

  getAdminProducts(token: string, paramsObj?: { keyword?: string; page?: number; pageSize?: number }): Observable<PagedResult<ProductSummary>> {
    let params = new HttpParams();
    if (paramsObj?.keyword) params = params.set('keyword', paramsObj.keyword);
    if (paramsObj?.page) params = params.set('page', paramsObj.page);
    if (paramsObj?.pageSize) params = params.set('pageSize', paramsObj.pageSize);

    return this.http.get<PagedResult<ProductSummary>>(`${this.baseUrl}/admin/products`, {
      headers: this.getAuthHeaders(token),
      params,
    });
  }

  getAdminProductDetail(token: string, id: number): Observable<ProductDetail> {
    return this.http.get<ProductDetail>(`${this.baseUrl}/admin/products/${id}`, {
      headers: this.getAuthHeaders(token),
    });
  }

  createProduct(token: string, req: CreateProductRequest): Observable<ProductDetail> {
    return this.http.post<ProductDetail>(`${this.baseUrl}/admin/products`, req, {
      headers: this.getAuthHeaders(token),
    });
  }

  updateProduct(token: string, id: number, req: UpdateProductRequest): Observable<ProductDetail> {
    return this.http.put<ProductDetail>(`${this.baseUrl}/admin/products/${id}`, req, {
      headers: this.getAuthHeaders(token),
    });
  }

  deleteProduct(token: string, id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.baseUrl}/admin/products/${id}`, {
      headers: this.getAuthHeaders(token),
    });
  }
}