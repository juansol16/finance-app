import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface TaxPaymentResponse {
    id: string;
    periodMonth: number;
    periodYear: number;
    amountDue: number;
    dueDate: string; // DateOnly string format yyyy-MM-dd
    status: TaxPaymentStatus;
    paymentDate?: string; // DateOnly string format
    determinationPdfUrl?: string;
    paymentReceiptUrl?: string;
    createdAt: string;
}

export enum TaxPaymentStatus {
    Pendiente = 0,
    Pagado = 1
}

export interface CreateTaxPaymentRequest {
    periodMonth: number;
    periodYear: number;
    amountDue: number;
    dueDate: string;
}

export interface UpdateTaxPaymentRequest {
    amountDue: number;
    dueDate: string;
}

@Injectable({
    providedIn: 'root'
})
export class TaxPaymentService {
    private apiUrl = `/api/tax-payments`;

    constructor(private http: HttpClient) { }

    getAll(startDate?: string, endDate?: string): Observable<TaxPaymentResponse[]> {
        let params: any = {};
        if (startDate) params.startDate = startDate;
        if (endDate) params.endDate = endDate;
        return this.http.get<TaxPaymentResponse[]>(this.apiUrl, { params });
    }

    create(request: CreateTaxPaymentRequest): Observable<TaxPaymentResponse> {
        return this.http.post<TaxPaymentResponse>(this.apiUrl, request);
    }

    update(id: string, request: UpdateTaxPaymentRequest): Observable<TaxPaymentResponse> {
        return this.http.put<TaxPaymentResponse>(`${this.apiUrl}/${id}`, request);
    }

    delete(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }

    uploadDetermination(id: string, file: File): Observable<TaxPaymentResponse> {
        const formData = new FormData();
        formData.append('file', file);
        return this.http.post<TaxPaymentResponse>(`${this.apiUrl}/${id}/determination`, formData);
    }

    markAsPaid(id: string, paymentDate: string, receiptFile?: File): Observable<TaxPaymentResponse> {
        const formData = new FormData();
        formData.append('paymentDate', paymentDate);
        if (receiptFile) {
            formData.append('receipt', receiptFile);
        }
        return this.http.put<TaxPaymentResponse>(`${this.apiUrl}/${id}/mark-paid`, formData);
    }
}
