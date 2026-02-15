import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface Income {
  id: string;
  type: number;
  source: string;
  date: string;
  amountMXN: number;
  exchangeRate: number | null;
  amountUSD: number | null;
  description: string | null;
  invoicePdfUrl: string | null;
  invoiceXmlUrl: string | null;
  xmlMetadata: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateIncomeRequest {
  type: number;
  source: string;
  date: string;
  amountMXN: number;
  exchangeRate?: number | null;
  amountUSD?: number | null;
  description?: string | null;
}

export type UpdateIncomeRequest = CreateIncomeRequest;

@Injectable({ providedIn: 'root' })
export class IncomeService {
  private readonly API = '/api/incomes';

  constructor(private http: HttpClient) {}

  getAll(): Observable<Income[]> {
    return this.http.get<Income[]>(this.API);
  }

  getById(id: string): Observable<Income> {
    return this.http.get<Income>(`${this.API}/${id}`);
  }

  create(data: CreateIncomeRequest): Observable<Income> {
    return this.http.post<Income>(this.API, data);
  }

  update(id: string, data: UpdateIncomeRequest): Observable<Income> {
    return this.http.put<Income>(`${this.API}/${id}`, data);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.API}/${id}`);
  }

  uploadFiles(id: string, pdf?: File, xml?: File): Observable<Income> {
    const formData = new FormData();
    if (pdf) formData.append('pdf', pdf);
    if (xml) formData.append('xml', xml);
    return this.http.post<Income>(`${this.API}/${id}/upload`, formData);
  }
}
