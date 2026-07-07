import { Component, OnInit } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import {
  TaxService,
  TaxSummaryResponse,
  AnnualTaxSummaryResponse,
  DashboardChartsResponse,
  OperationsItem,
  VolatilitySummary,
  LastUsdIncomeResponse,
} from '../../core/services/tax.service';
import { AuthService } from '../../core/services/auth.service';
import { ExchangeRateService } from '../../core/services/exchange-rate.service';
import { CreditCard, CreditCardService } from '../../core/services/credit-card.service';
import {
  buildMonthCells,
  rankCardsByFloat,
  CalendarCell,
  CardScheduleEntry,
} from './card-schedule';
import { ChartConfiguration, ChartData, ChartType } from 'chart.js';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: [],
  standalone: false,
})
export class DashboardComponent implements OnInit {
  currentMonth: number;
  currentYear: number;

  summary: TaxSummaryResponse | null = null;
  annualSummary: AnnualTaxSummaryResponse | null = null;
  loading = false;
  showAnnual = false;

  months = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
  years = [2023, 2024, 2025, 2026];
  chartPeriod: number = 12;

  // RESICO annual limit
  readonly RESICO_LIMIT = 3_500_000;
  resicoPercent = 0;

  // Operations table
  operations: OperationsItem[] = [];
  volatilitySummary: VolatilitySummary | null = null;

  // Live FX banner indicator
  currentFx: { rate: number; date: string } | null = null;
  lastUsdIncome: LastUsdIncomeResponse | null = null;

  // Card statement calendar (always the real current month, independent of the period filters)
  calendarCells: CalendarCell[] = [];
  cardSchedule: CardScheduleEntry[] = [];
  calMonth = 0; // 1-12
  calYear = 0;
  readonly weekdayKeys = [1, 2, 3, 4, 5, 6, 7].map((d) => `DASHBOARD.WD_${d}`);
  // Validated for dark surfaces (dataviz palette checks): amber = corte, cyan = pago
  readonly CUTOFF_COLOR = '#d97706';
  readonly PAYMENT_COLOR = '#0891b2';

  // Volatility chart (readable: axes, labels, tooltips)
  public volatilityMiniData: ChartData<'line'> = { labels: [], datasets: [] };
  public volatilityMiniOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      x: {
        grid: { display: false },
        ticks: { color: 'rgba(124,139,161,0.9)', font: { size: 10 } },
      },
      y: {
        beginAtZero: false,
        grace: '10%',
        grid: { color: 'rgba(124,139,161,0.14)' },
        ticks: { color: 'rgba(124,139,161,0.9)', font: { size: 10 }, maxTicksLimit: 4 },
      },
    },
    plugins: {
      legend: { display: false },
      tooltip: {
        enabled: true,
        callbacks: {
          label: (ctx) => ` ${Number(ctx.parsed.y).toFixed(4)} MXN/USD`,
        },
      },
    },
    elements: { point: { radius: 3, hoverRadius: 5 }, line: { tension: 0.35, borderWidth: 2 } },
  };

  constructor(
    private taxService: TaxService,
    private translate: TranslateService,
    private authService: AuthService,
    private exchangeRateService: ExchangeRateService,
    private creditCardService: CreditCardService,
  ) {
    const now = new Date();
    this.currentMonth = now.getMonth();
    this.currentYear = now.getFullYear();

    if (this.currentMonth === 0) {
      this.currentMonth = 12;
      this.currentYear--;
    }
  }

  /** First name for a friendly, personal greeting. */
  get firstName(): string {
    const full = this.authService.user?.fullName?.trim();
    return full ? full.split(' ')[0] : '';
  }

  /** Time-of-day greeting translation key. */
  get greetingKey(): string {
    const h = new Date().getHours();
    if (h < 12) return 'DASHBOARD.GREETING_MORNING';
    if (h < 19) return 'DASHBOARD.GREETING_AFTERNOON';
    return 'DASHBOARD.GREETING_EVENING';
  }

  ngOnInit(): void {
    this.loadData();
    this.loadCharts();
    this.loadFxIndicator();
    this.loadCardCalendar();
  }

  loadCardCalendar(): void {
    const today = new Date();
    this.calMonth = today.getMonth() + 1;
    this.calYear = today.getFullYear();
    this.creditCardService.getAll().subscribe({
      next: (cards) => {
        this.calendarCells = buildMonthCells(today.getFullYear(), today.getMonth(), cards, today);
        this.cardSchedule = rankCardsByFloat(cards, today);
      },
      error: () => {
        this.calendarCells = [];
        this.cardSchedule = [];
      },
    });
  }

  /** Best card for a purchase made today: the ranked leader that has a full schedule. */
  get bestCard(): CardScheduleEntry | null {
    const best = this.cardSchedule[0];
    return best && best.floatDays !== null ? best : null;
  }

  /** "6 Jul" style label using the app's translated month names. */
  formatCalDate(d: Date): string {
    const month = this.translate.instant(this.getMonthKey(d.getMonth() + 1));
    return `${d.getDate()} ${month}`;
  }

  calCellTitle(cell: CalendarCell): string {
    const parts: string[] = [];
    if (cell.cutoffCards.length) {
      parts.push(`${this.translate.instant('DASHBOARD.CUTOFF')}: ${cell.cutoffCards.join(', ')}`);
    }
    if (cell.paymentCards.length) {
      parts.push(`${this.translate.instant('DASHBOARD.PAYMENT')}: ${cell.paymentCards.join(', ')}`);
    }
    return parts.join(' · ');
  }

  loadFxIndicator(): void {
    this.exchangeRateService.getCurrentUsdMxn().subscribe({
      next: (fx) => {
        this.currentFx = fx;
      },
      error: () => {
        this.currentFx = null;
      },
    });
    this.taxService.getLastUsdIncome().subscribe({
      next: (income) => {
        this.lastUsdIncome = income;
      },
      error: () => {
        this.lastUsdIncome = null;
      },
    });
  }

  /**
   * What today's rate would mean for the last USD payment:
   * gross = USD x rate, then the standard withholding math forward
   * (honorario = gross / 1.16, net = honorario x 1.04084).
   */
  get paidTodayProjection(): { gross: number; net: number; rateChange: number } | null {
    if (!this.currentFx || !this.lastUsdIncome) return null;

    const round2 = (v: number) => Math.round(v * 100) / 100;
    const gross = round2(this.lastUsdIncome.takeHomePayUSD * this.currentFx.rate);
    const honorario = round2(gross / 1.16);
    const net = round2(honorario * 1.04084);
    const rateChange =
      this.lastUsdIncome.exchangeRate > 0
        ? (this.currentFx.rate - this.lastUsdIncome.exchangeRate) / this.lastUsdIncome.exchangeRate
        : 0;

    return { gross, net, rateChange };
  }

  getMonthKey(m: number): string {
    return `DASHBOARD.MONTH_${m}`;
  }

  loadData(): void {
    this.loading = true;
    this.taxService.getMonthlySummary(this.currentMonth, this.currentYear).subscribe({
      next: (data) => {
        this.summary = data;
        this.resicoPercent =
          this.RESICO_LIMIT > 0
            ? Math.min((data.annualAccumulatedIncome / this.RESICO_LIMIT) * 100, 100)
            : 0;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  loadAnnualSummary(): void {
    this.showAnnual = !this.showAnnual;
    if (this.showAnnual && !this.annualSummary) {
      this.loading = true;
      this.taxService.getAnnualSummary(this.currentYear).subscribe({
        next: (data) => {
          this.annualSummary = data;
          this.loading = false;
        },
        error: () => {
          this.loading = false;
        },
      });
    }
  }

  // Cash Flow Chart
  public cashFlowChartData: ChartData<'bar' | 'line'> = { labels: [], datasets: [] };
  // Chart colors are theme-neutral (readable on both dark and light backgrounds)
  public cashFlowChartOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      x: { grid: { color: 'rgba(124,139,161,0.14)' }, ticks: { color: 'rgba(124,139,161,0.9)' } },
      y: { grid: { color: 'rgba(124,139,161,0.14)' }, ticks: { color: 'rgba(124,139,161,0.9)' } },
    },
    plugins: { legend: { display: true, labels: { color: 'rgba(124,139,161,1)' } } },
  };

  loadCharts(): void {
    this.taxService.getDashboardCharts(this.chartPeriod).subscribe({
      next: (data) => {
        const formatLabel = (item: { month: number; year: number }) => {
          const monthName = this.translate.instant(this.getMonthKey(item.month));
          return this.chartPeriod > 12 ? `${monthName} ${item.year}` : monthName;
        };

        // Cash Flow Data
        const labels = data.cashFlow.map(formatLabel);
        this.cashFlowChartData = {
          labels: labels,
          datasets: [
            {
              type: 'line',
              label: this.translate.instant('DASHBOARD.TOTAL_OUTFLOWS'),
              data: data.cashFlow.map((i) => i.totalOutflow),
              borderColor: '#fb7185',
              borderWidth: 2,
              borderDash: [5, 5],
              pointBackgroundColor: '#fb7185',
              fill: false,
              tension: 0.4,
            },
            {
              type: 'bar',
              label: this.translate.instant('DASHBOARD.TOTAL_INCOME_MIX'),
              data: data.cashFlow.map((i) => i.totalIncome),
              backgroundColor: '#34d399',
              borderRadius: 10,
            },
          ],
        };

        // Operations table (show last 6 months, most recent first)
        this.operations = [...data.operations].reverse().slice(0, 6);

        // Volatility summary
        this.volatilitySummary = data.volatilitySummary;

        // Volatility chart: only months that actually had USD income
        // (zero months would flatten the scale), last 6 of them, with labels
        const recentVol = data.volatility.filter((v) => v.averageExchangeRate > 0).slice(-6);
        this.volatilityMiniData = {
          labels: recentVol.map(formatLabel),
          datasets: [
            {
              data: recentVol.map((v) => v.averageExchangeRate),
              borderColor: '#60a5fa',
              backgroundColor: 'rgba(96,165,250,0.14)',
              pointBackgroundColor: '#60a5fa',
              fill: true,
            },
          ],
        };
      },
    });
  }

  onChartPeriodChange(): void {
    this.loadCharts();
  }

  onMonthChange(event: any): void {
    this.currentMonth = +event.target.value;
    this.annualSummary = null;
    this.loadData();
  }

  onYearChange(event: any): void {
    this.currentYear = +event.target.value;
    this.annualSummary = null;
    this.loadData();
  }

  getVolatilityNote(): string {
    if (!this.volatilitySummary) return '';
    if (this.volatilitySummary.trend === 'down') {
      return this.translate.instant('DASHBOARD.VOLATILITY_NOTE_DOWN');
    }
    return this.translate.instant('DASHBOARD.VOLATILITY_NOTE_UP');
  }
}
