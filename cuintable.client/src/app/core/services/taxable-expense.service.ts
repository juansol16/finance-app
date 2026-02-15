import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface TaxableExpense {
  id: string;
  category: number;
  creditCardId: string | null;
  creditCardLabel: string | null;
  expenseId: string | null;
  linkedExpenseLabel: string | null;
  date: string;
  amountMXN: number;
  description: string | null;
  vendor: string;
  invoicePdfUrl: string | null;
  invoiceXmlUrl: string | null;
  xmlMetadata: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateTaxableExpenseRequest {
  category: number;
  creditCardId?: string | null;
  expenseId?: string | null;
  date: string;
  amountMXN: number;
  description?: string | null;
  vendor: string;
}

export type UpdateTaxableExpenseRequest = CreateTaxableExpenseRequest;

@Injectable({ providedIn: 'root' })
export class TaxableExpenseService {
  private readonly API = '/api/taxable-expenses';

  constructor(private http: HttpClient) {}

  getAll(): Observable<TaxableExpense[]> {
    return this.http.get<TaxableExpense[]>(this.API);
  }

  getById(id: string): Observable<TaxableExpense> {
    return this.http.get<TaxableExpense>(`${this.API}/${id}`);
  }

  create(data: CreateTaxableExpenseRequest): Observable<TaxableExpense> {
    return this.http.post<TaxableExpense>(this.API, data);
  }

  update(id: string, data: UpdateTaxableExpenseRequest): Observable<TaxableExpense> {
    return this.http.put<TaxableExpense>(`${this.API}/${id}`, data);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.API}/${id}`);
  }

  uploadFiles(id: string, pdf?: File, xml?: File): Observable<TaxableExpense> {
    const formData = new FormData();
    if (pdf) formData.append('pdf', pdf);
    if (xml) formData.append('xml', xml);
    return this.http.post<TaxableExpense>(`${this.API}/${id}/upload`, formData);
  }
}
