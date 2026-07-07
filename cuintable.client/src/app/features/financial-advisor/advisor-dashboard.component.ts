import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { ChartConfiguration, ChartData } from 'chart.js';
import { CreditCard, CreditCardService } from '../../core/services/credit-card.service';
import {
  AdvisorDashboard,
  FinancialAdvisorService,
  StatementDetail,
  StatementStatus,
  StatementSummary,
} from '../../core/services/financial-advisor.service';
import { categoryKey, statusKey } from './advisor-labels';

@Component({
  selector: 'app-advisor-dashboard',
  templateUrl: './advisor-dashboard.component.html',
  standalone: false,
})
export class AdvisorDashboardComponent implements OnInit {
  readonly StatementStatus = StatementStatus;
  readonly categoryKey = categoryKey;
  readonly statusKey = statusKey;

  months = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
  years = [2023, 2024, 2025, 2026];
  year: number;
  month: number;

  dashboard: AdvisorDashboard | null = null;
  statements: StatementSummary[] = [];
  cards: CreditCard[] = [];
  loading = true;
  showUpload = false;
  deleting: StatementSummary | null = null;
  reprocessingId: string | null = null;

  // Single-series magnitude charts: one hue each, no legend (the title names the series)
  public categoryChartData: ChartData<'bar'> = { labels: [], datasets: [] };
  public categoryChartOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    indexAxis: 'y',
    scales: {
      x: {
        grid: { color: 'rgba(124,139,161,0.14)' },
        ticks: { color: 'rgba(124,139,161,0.9)', font: { size: 10 } },
      },
      y: {
        grid: { display: false },
        ticks: { color: 'rgba(124,139,161,0.9)', font: { size: 11 } },
      },
    },
    plugins: {
      legend: { display: false },
      tooltip: {
        callbacks: {
          label: (ctx) =>
            ` $${Number(ctx.parsed.x).toLocaleString('es-MX', { maximumFractionDigits: 0 })} MXN`,
        },
      },
    },
  };

  public trendChartData: ChartData<'line'> = { labels: [], datasets: [] };
  public trendChartOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      x: {
        grid: { display: false },
        ticks: { color: 'rgba(124,139,161,0.9)', font: { size: 10 } },
      },
      y: {
        beginAtZero: true,
        grid: { color: 'rgba(124,139,161,0.14)' },
        ticks: { color: 'rgba(124,139,161,0.9)', font: { size: 10 }, maxTicksLimit: 4 },
      },
    },
    plugins: {
      legend: { display: false },
      tooltip: {
        callbacks: {
          label: (ctx) =>
            ` $${Number(ctx.parsed.y).toLocaleString('es-MX', { maximumFractionDigits: 0 })} MXN`,
        },
      },
    },
    elements: { point: { radius: 3, hoverRadius: 5 }, line: { tension: 0.35, borderWidth: 2 } },
  };

  constructor(
    private service: FinancialAdvisorService,
    private creditCardService: CreditCardService,
    private translate: TranslateService,
    private router: Router,
  ) {
    const now = new Date();
    this.year = now.getFullYear();
    this.month = now.getMonth() + 1;
  }

  ngOnInit(): void {
    this.creditCardService.getAll().subscribe({
      next: (cards) => (this.cards = cards),
      error: () => (this.cards = []),
    });
    this.load();
  }

  load(): void {
    this.loading = true;
    this.service.getDashboard(this.year, this.month).subscribe({
      next: (data) => {
        this.dashboard = data;
        this.buildCharts(data);
        this.loading = false;
      },
      error: () => {
        this.dashboard = null;
        this.loading = false;
      },
    });
    this.service.getStatements().subscribe({
      next: (data) => (this.statements = data),
      error: () => (this.statements = []),
    });
  }

  onPeriodChange(): void {
    this.load();
  }

  private buildCharts(data: AdvisorDashboard): void {
    this.categoryChartData = {
      labels: data.categoryTotals.map((c) => this.translate.instant(categoryKey(c.category))),
      datasets: [
        {
          data: data.categoryTotals.map((c) => c.totalMXN),
          backgroundColor: '#0891b2',
          borderRadius: 4,
          maxBarThickness: 18,
        },
      ],
    };

    this.trendChartData = {
      labels: data.trend.map(
        (p) => `${this.translate.instant(this.getMonthKey(p.month))} ${p.year}`,
      ),
      datasets: [
        {
          data: data.trend.map((p) => p.totalChargesMXN),
          borderColor: '#60a5fa',
          backgroundColor: 'rgba(96,165,250,0.14)',
          pointBackgroundColor: '#60a5fa',
          fill: true,
        },
      ],
    };
  }

  getMonthKey(m: number): string {
    return `DASHBOARD.MONTH_${m}`;
  }

  monthLabel(s: StatementSummary): string {
    return `${this.translate.instant(this.getMonthKey(s.periodMonth))} ${s.periodYear}`;
  }

  cardLabel(s: StatementSummary): string {
    const label = s.cardNickname
      ? s.cardNickname
      : s.cardLastFour
        ? `${s.bankName ?? ''} ****${s.cardLastFour}`
        : (s.bankName ?? '');
    return s.accountType === 1 ? `🏦 ${label}` : label;
  }

  openStatement(s: StatementSummary): void {
    this.router.navigate(['/financial-advisor/statements', s.id]);
  }

  onUploaded(detail: StatementDetail): void {
    this.showUpload = false;
    this.load();
    this.router.navigate(['/financial-advisor/statements', detail.id]);
  }

  reprocess(s: StatementSummary, event: Event): void {
    event.stopPropagation();
    this.reprocessingId = s.id;
    this.service.reprocess(s.id).subscribe({
      next: () => {
        this.reprocessingId = null;
        this.load();
      },
      error: () => {
        this.reprocessingId = null;
        this.load();
      },
    });
  }

  confirmDelete(s: StatementSummary, event: Event): void {
    event.stopPropagation();
    this.deleting = s;
  }

  deleteStatement(): void {
    if (!this.deleting) return;
    this.service.delete(this.deleting.id).subscribe({
      next: () => {
        this.deleting = null;
        this.load();
      },
      error: () => (this.deleting = null),
    });
  }
}
