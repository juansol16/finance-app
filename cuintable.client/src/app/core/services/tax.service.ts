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
}
