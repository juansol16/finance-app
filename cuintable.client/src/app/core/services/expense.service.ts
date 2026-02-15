import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface Expense {
  id: string;
  category: number;
  creditCardId: string | null;
  creditCardLabel: string | null;
  date: string;
  amountMXN: number;
  description: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateExpenseRequest {
  category: number;
  creditCardId?: string | null;
  date: string;
  amountMXN: number;
  description?: string | null;
}

export type UpdateExpenseRequest = CreateExpenseRequest;

@Injectable({ providedIn: 'root' })
export class ExpenseService {
  private readonly API = '/api/expenses';

  constructor(private http: HttpClient) {}

  getAll(): Observable<Expense[]> {
    return this.http.get<Expense[]>(this.API);
  }

  create(data: CreateExpenseRequest): Observable<Expense> {
    return this.http.post<Expense>(this.API, data);
  }

  update(id: string, data: UpdateExpenseRequest): Observable<Expense> {
    return this.http.put<Expense>(`${this.API}/${id}`, data);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.API}/${id}`);
  }
}
