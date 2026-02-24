import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface TaxSummaryResponse {
    month: number;
    year: number;
    totalIncome: number;
    totalDeductibleExpenses: number;
    taxableBase: number;
    estimatedISR: number;
    effectiveTaxRate: number;
    incomeChangePercent: number;
    deductiblePercent: number;
    profitMargin: number;
    estimatedIVA: number;
    annualAccumulatedIncome: number;
}

export interface AnnualTaxSummaryResponse {
    year: number;
    monthlySummaries: TaxSummaryResponse[];
    totalAnnualIncome: number;
    totalAnnualDeductible: number;
    totalAnnualISR: number;
    averageEffectiveTaxRate: number;
}

@Injectable({
    providedIn: 'root'
})
export class TaxService {
    private apiUrl = `/api/tax`;

    constructor(private http: HttpClient) { }

    getMonthlySummary(month: number, year: number): Observable<TaxSummaryResponse> {
        const params = new HttpParams()
            .set('month', month.toString())
            .set('year', year.toString());
        return this.http.get<TaxSummaryResponse>(`${this.apiUrl}/summary`, { params });
    }

    getAnnualSummary(year: number): Observable<AnnualTaxSummaryResponse> {
        const params = new HttpParams().set('year', year.toString());
        return this.http.get<AnnualTaxSummaryResponse>(`${this.apiUrl}/annual-summary`, { params });
    }

    getDashboardCharts(months: number = 12): Observable<DashboardChartsResponse> {
        const params = new HttpParams().set('months', months.toString());
        return this.http.get<DashboardChartsResponse>(`${this.apiUrl}/charts`, { params });
    }
}

export interface CashFlowItem {
    month: number;
    year: number;
    totalIncome: number;
    totalExpenses: number;
    totalTaxPayments: number;
    totalOutflow: number;
}

export interface VolatilityItem {
    month: number;
    year: number;
    averageExchangeRate: number;
}

export interface OperationsItem {
    month: number;
    year: number;
    income: number;
    deductibleExpenses: number;
    isr: number;
    ivaNet: number;
    profit: number;
}

export interface VolatilitySummary {
    currentRate: number;
    previousRate: number;
    changePercent: number;
    trend: string;
}

export interface DashboardChartsResponse {
    cashFlow: CashFlowItem[];
    volatility: VolatilityItem[];
    operations: OperationsItem[];
    volatilitySummary: VolatilitySummary;
}
