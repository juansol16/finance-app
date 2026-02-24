import { Component, OnInit } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { TaxService, TaxSummaryResponse, AnnualTaxSummaryResponse, DashboardChartsResponse, OperationsItem, VolatilitySummary } from '../../core/services/tax.service';
import { ChartConfiguration, ChartData, ChartType } from 'chart.js';

@Component({
    selector: 'app-dashboard',
    templateUrl: './dashboard.component.html',
    styleUrls: [],
    standalone: false
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

    // Volatility mini chart
    public volatilityMiniData: ChartData<'line'> = { labels: [], datasets: [] };
    public volatilityMiniOptions: ChartConfiguration['options'] = {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
            x: { display: false },
            y: { display: false }
        },
        plugins: { legend: { display: false }, tooltip: { enabled: false } },
        elements: { point: { radius: 0 }, line: { tension: 0.4, borderWidth: 2 } }
    };

    constructor(private taxService: TaxService, private translate: TranslateService) {
        const now = new Date();
        this.currentMonth = now.getMonth();
        this.currentYear = now.getFullYear();

        if (this.currentMonth === 0) {
            this.currentMonth = 12;
            this.currentYear--;
        }
    }

    ngOnInit(): void {
        this.loadData();
        this.loadCharts();
    }

    getMonthKey(m: number): string {
        return `DASHBOARD.MONTH_${m}`;
    }

    loadData(): void {
        this.loading = true;
        this.taxService.getMonthlySummary(this.currentMonth, this.currentYear)
            .subscribe({
                next: (data) => {
                    this.summary = data;
                    this.resicoPercent = this.RESICO_LIMIT > 0
                        ? Math.min((data.annualAccumulatedIncome / this.RESICO_LIMIT) * 100, 100)
                        : 0;
                    this.loading = false;
                },
                error: () => { this.loading = false; }
            });
    }

    loadAnnualSummary(): void {
        this.showAnnual = !this.showAnnual;
        if (this.showAnnual && !this.annualSummary) {
            this.loading = true;
            this.taxService.getAnnualSummary(this.currentYear)
                .subscribe({
                    next: (data) => {
                        this.annualSummary = data;
                        this.loading = false;
                    },
                    error: () => { this.loading = false; }
                });
        }
    }

    // Cash Flow Chart
    public cashFlowChartData: ChartData<'bar' | 'line'> = { labels: [], datasets: [] };
    public cashFlowChartOptions: ChartConfiguration['options'] = {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
            x: { grid: { color: 'rgba(255,255,255,0.04)' }, ticks: { color: 'rgba(148,163,184,0.7)' } },
            y: { grid: { color: 'rgba(255,255,255,0.04)' }, ticks: { color: 'rgba(148,163,184,0.7)' } }
        },
        plugins: { legend: { display: true, labels: { color: 'rgba(148,163,184,0.9)' } } }
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
                            data: data.cashFlow.map(i => i.totalOutflow),
                            borderColor: '#ef4444',
                            borderWidth: 2,
                            borderDash: [5, 5],
                            pointBackgroundColor: '#ef4444',
                            fill: false,
                            tension: 0.4
                        },
                        {
                            type: 'bar',
                            label: this.translate.instant('DASHBOARD.TOTAL_INCOME_MIX'),
                            data: data.cashFlow.map(i => i.totalIncome),
                            backgroundColor: '#10b981',
                            borderRadius: 6
                        }
                    ]
                };

                // Operations table (show last 6 months, most recent first)
                this.operations = [...data.operations].reverse().slice(0, 6);

                // Volatility summary
                this.volatilitySummary = data.volatilitySummary;

                // Volatility mini sparkline (last 6 months)
                const recentVol = data.volatility.slice(-6);
                this.volatilityMiniData = {
                    labels: recentVol.map(() => ''),
                    datasets: [{
                        data: recentVol.map(v => v.averageExchangeRate),
                        borderColor: '#3b82f6',
                        backgroundColor: 'rgba(59,130,246,0.1)',
                        fill: true
                    }]
                };
            }
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
