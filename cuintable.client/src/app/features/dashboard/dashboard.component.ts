import { Component, OnInit } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { TaxService, TaxSummaryResponse, AnnualTaxSummaryResponse, DashboardChartsResponse } from '../../core/services/tax.service';
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

    public barChartOptions: ChartConfiguration['options'] = {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
            x: {
                grid: { color: 'rgba(255,255,255,0.04)' },
                ticks: { color: 'rgba(148,163,184,0.7)' }
            },
            y: {
                min: 0,
                grid: { color: 'rgba(255,255,255,0.04)' },
                ticks: { color: 'rgba(148,163,184,0.7)' }
            }
        },
        plugins: {
            legend: { display: true, labels: { color: 'rgba(148,163,184,0.9)' } }
        }
    };
    public barChartType: ChartType = 'bar';
    public barChartData: ChartData<'bar'> = {
        labels: [],
        datasets: [
            {
                data: [0, 0, 0, 0], label: '', backgroundColor: ['rgba(16,185,129,0.7)', 'rgba(239,68,68,0.7)', 'rgba(59,130,246,0.7)', 'rgba(245,158,11,0.7)'],
                borderColor: ['#10b981', '#ef4444', '#3b82f6', '#f59e0b'],
                borderWidth: 1,
                borderRadius: 6
            }
        ]
    };

    months = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
    years = [2023, 2024, 2025, 2026];

    constructor(private taxService: TaxService, private translate: TranslateService) {
        const now = new Date();
        this.currentMonth = now.getMonth(); // Previous month (0-11, so getMonth() is actually previous month 1-12 index)
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
                    this.updateChart(data);
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

    updateChart(data: TaxSummaryResponse): void {
        this.barChartData = {
            labels: [
                this.translate.instant('DASHBOARD.INCOME_LABEL'),
                this.translate.instant('DASHBOARD.EXPENSES_LABEL'),
                this.translate.instant('DASHBOARD.BASE_LABEL'),
                this.translate.instant('DASHBOARD.ISR_LABEL')
            ],
            datasets: [
                {
                    data: [data.totalIncome, data.totalDeductibleExpenses, data.taxableBase, data.estimatedISR],
                    label: this.translate.instant('DASHBOARD.CHART_LABEL'),
                    backgroundColor: ['rgba(16,185,129,0.7)', 'rgba(239,68,68,0.7)', 'rgba(59,130,246,0.7)', 'rgba(245,158,11,0.7)'],
                    borderColor: ['#10b981', '#ef4444', '#3b82f6', '#f59e0b'],
                    borderWidth: 1,
                    borderRadius: 6
                }
            ]
        };
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

    // Volatility Chart
    public volatilityChartData: ChartData<'line'> = { labels: [], datasets: [] };
    public volatilityChartOptions: ChartConfiguration['options'] = {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
            x: { grid: { color: 'rgba(255,255,255,0.04)' }, ticks: { color: 'rgba(148,163,184,0.7)' } },
            y: { grid: { color: 'rgba(255,255,255,0.04)' }, ticks: { color: 'rgba(148,163,184,0.7)' } }
        },
        plugins: { legend: { display: false } },
        elements: { line: { tension: 0.4 } }
    };
    public volatilityChartType: ChartType = 'line';

    loadCharts(): void {
        this.taxService.getDashboardCharts().subscribe({
            next: (data) => {
                // Cash Flow Data
                const labels = data.cashFlow.map(i => this.translate.instant(this.getMonthKey(i.month)));
                this.cashFlowChartData = {
                    labels: labels,
                    datasets: [
                        {
                            type: 'line',
                            label: this.translate.instant('DASHBOARD.TOTAL_OUTFLOWS'), // Gastos Totales
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
                            label: this.translate.instant('DASHBOARD.TOTAL_INCOME_MIX'), // Ingreso Total (Mix)
                            data: data.cashFlow.map(i => i.totalIncome),
                            backgroundColor: '#10b981',
                            borderRadius: 6
                        }
                    ]
                };

                // Volatility Data
                const vLabels = data.volatility.map(i => this.translate.instant(this.getMonthKey(i.month)));
                this.volatilityChartData = {
                    labels: vLabels,
                    datasets: [
                        {
                            data: data.volatility.map(i => i.averageExchangeRate),
                            label: 'USD',
                            borderColor: '#3b82f6',
                            backgroundColor: 'rgba(59,130,246,0.2)',
                            fill: true,
                            pointBackgroundColor: '#3b82f6',
                            pointBorderColor: '#fff',
                            pointHoverBackgroundColor: '#fff',
                            pointHoverBorderColor: '#3b82f6'
                        }
                    ]
                };
            }
        });
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
}
