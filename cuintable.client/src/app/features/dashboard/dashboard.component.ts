import { Component, OnInit } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { TaxService, TaxSummaryResponse, AnnualTaxSummaryResponse } from '../../core/services/tax.service';
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
            { data: [0, 0, 0, 0], label: '', backgroundColor: ['rgba(16,185,129,0.7)', 'rgba(239,68,68,0.7)', 'rgba(59,130,246,0.7)', 'rgba(245,158,11,0.7)'],
                    borderColor: ['#10b981', '#ef4444', '#3b82f6', '#f59e0b'],
                    borderWidth: 1,
                    borderRadius: 6 }
        ]
    };

    months = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
    years = [2023, 2024, 2025, 2026];

    constructor(private taxService: TaxService, private translate: TranslateService) {
        const now = new Date();
        this.currentMonth = now.getMonth() + 1;
        this.currentYear = now.getFullYear();
    }

    ngOnInit(): void {
        this.loadData();
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
