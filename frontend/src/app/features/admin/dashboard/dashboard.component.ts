import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { ChartConfiguration, ChartData } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';
import { AuthService } from '../../../auth.service';
import {
  DashboardService,
  DashboardSummary,
  InventoryReport,
  RevenueReport,
  TopProduct,
} from './dashboard.service';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, FormsModule, RouterLink, RouterLinkActive, BaseChartDirective],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  readonly summary = signal<DashboardSummary | null>(null);
  readonly revenueReport = signal<RevenueReport | null>(null);
  readonly topProducts = signal<TopProduct[]>([]);
  readonly inventory = signal<InventoryReport | null>(null);

  readonly loading = signal(false);
  readonly error = signal('');

  startDate: string;
  endDate: string;

  readonly lineChartData: ChartData<'line'> = {
    labels: [],
    datasets: [
      {
        data: [],
        label: 'Doanh thu',
        borderColor: '#2563eb',
        backgroundColor: 'rgba(37, 99, 235, 0.12)',
        pointBackgroundColor: '#2563eb',
        fill: true,
        tension: 0.3,
      },
    ],
  };

  readonly lineChartOptions: ChartConfiguration<'line'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
      tooltip: {
        callbacks: {
          label: ctx => this.formatVnd(Number(ctx.parsed.y)),
        },
      },
    },
    scales: {
      y: {
        ticks: { callback: value => this.formatVndShort(Number(value)) },
      },
    },
  };

  constructor(
    private readonly auth: AuthService,
    private readonly dashboardService: DashboardService,
  ) {
    const now = new Date();
    const firstDay = new Date(now.getFullYear(), now.getMonth(), 1);
    this.startDate = this.toDateInput(firstDay);
    this.endDate = this.toDateInput(now);
  }

  ngOnInit(): void {
    this.loadAll();
  }

  private getToken(): string | null {
    return this.auth.restoreSession()?.token ?? null;
  }

  private toDateInput(d: Date): string {
    return d.toISOString().slice(0, 10);
  }

  loadAll(): void {
    const token = this.getToken();
    if (!token) return;

    this.loading.set(true);
    this.error.set('');

    this.dashboardService.getSummary(token, this.startDate, this.endDate).subscribe({
      next: res => this.summary.set(res),
      error: () => this.error.set('Không thể tải số liệu tổng quan.'),
    });

    this.dashboardService.getRevenueReport(token, this.startDate, this.endDate).subscribe({
      next: res => {
        this.revenueReport.set(res);
        this.updateChart(res);
      },
      error: () => this.error.set('Không thể tải báo cáo doanh thu.'),
    });

    this.dashboardService.getTopProducts(token, 10, this.startDate, this.endDate).subscribe({
      next: res => this.topProducts.set(res.products),
      error: () => {},
    });

    this.dashboardService.getInventoryReport(token).subscribe({
      next: res => {
        this.inventory.set(res);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Không thể tải báo cáo tồn kho.');
        this.loading.set(false);
      },
    });
  }

  private updateChart(report: RevenueReport): void {
    this.lineChartData.labels = report.dailyRevenue.map(d => this.formatDateShort(d.date));
    this.lineChartData.datasets[0].data = report.dailyRevenue.map(d => d.revenue);
  }

  applyFilter(): void {
    this.loadAll();
  }

  resetFilter(): void {
    const now = new Date();
    const firstDay = new Date(now.getFullYear(), now.getMonth(), 1);
    this.startDate = this.toDateInput(firstDay);
    this.endDate = this.toDateInput(now);
    this.loadAll();
  }

  formatVnd(amount: number): string {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
  }

  formatVndShort(amount: number): string {
    if (amount >= 1_000_000) return (amount / 1_000_000).toFixed(1) + 'tr';
    if (amount >= 1_000) return (amount / 1_000).toFixed(0) + 'k';
    return amount.toString();
  }

  formatDateShort(dateStr: string): string {
    const d = new Date(dateStr);
    return `${d.getDate().toString().padStart(2, '0')}/${(d.getMonth() + 1).toString().padStart(2, '0')}`;
  }

  growthClass(value: number): string {
    return value >= 0 ? 'positive' : 'negative';
  }
}