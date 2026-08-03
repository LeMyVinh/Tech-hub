import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface DashboardSummary {
  totalRevenue: number;
  totalOrders: number;
  totalCustomers: number;
  totalProducts: number;
  pendingOrders: number;
  revenueGrowthPercent: number;
  orderGrowthPercent: number;
}

export interface RevenueByDate {
  date: string;
  revenue: number;
  orderCount: number;
}

export interface RevenueByCategory {
  categoryName: string;
  revenue: number;
  orderCount: number;
  percentage: number;
}

export interface RevenueReport {
  dailyRevenue: RevenueByDate[];
  categoryRevenue: RevenueByCategory[];
  totalRevenue: number;
  averageOrderValue: number;
}

export interface TopProduct {
  productId: number;
  productName: string;
  totalSold: number;
  revenue: number;
  imageUrl: string | null;
}

export interface TopProductsResponse {
  products: TopProduct[];
}

export interface LowStockProduct {
  variantId: number;
  productName: string;
  variantName: string;
  stockQuantity: number;
  sku: string;
}

export interface InventoryReport {
  totalVariants: number;
  inStock: number;
  lowStock: number;
  outOfStock: number;
  lowStockProducts: LowStockProduct[];
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly apiUrl = 'http://localhost:5159/api/v1/dashboard';

  constructor(private readonly http: HttpClient) {}

  private headers(token: string): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }

  private dateParams(startDate?: string, endDate?: string): HttpParams {
    let params = new HttpParams();
    if (startDate) params = params.set('startDate', startDate);
    if (endDate) params = params.set('endDate', endDate);
    return params;
  }

  getSummary(token: string, startDate?: string, endDate?: string): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.apiUrl}/summary`, {
      headers: this.headers(token),
      params: this.dateParams(startDate, endDate),
    });
  }

  getRevenueReport(token: string, startDate?: string, endDate?: string): Observable<RevenueReport> {
    return this.http.get<RevenueReport>(`${this.apiUrl}/revenue`, {
      headers: this.headers(token),
      params: this.dateParams(startDate, endDate),
    });
  }

  getTopProducts(
    token: string,
    limit = 10,
    startDate?: string,
    endDate?: string,
  ): Observable<TopProductsResponse> {
    const params = this.dateParams(startDate, endDate).set('limit', limit);
    return this.http.get<TopProductsResponse>(`${this.apiUrl}/top-products`, {
      headers: this.headers(token),
      params,
    });
  }

  getInventoryReport(token: string): Observable<InventoryReport> {
    return this.http.get<InventoryReport>(`${this.apiUrl}/inventory`, {
      headers: this.headers(token),
    });
  }
}