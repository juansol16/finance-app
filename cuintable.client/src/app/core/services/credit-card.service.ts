import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface CreditCard {
  id: string;
  bank: string;
  nickname: string;
  lastFourDigits: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreateCreditCardRequest {
  bank: string;
  nickname: string;
  lastFourDigits: string;
}

export interface UpdateCreditCardRequest extends CreateCreditCardRequest {
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class CreditCardService {
  private readonly API = '/api/credit-cards';

  constructor(private http: HttpClient) {}

  getAll(): Observable<CreditCard[]> {
    return this.http.get<CreditCard[]>(this.API);
  }

  create(data: CreateCreditCardRequest): Observable<CreditCard> {
    return this.http.post<CreditCard>(this.API, data);
  }

  update(id: string, data: UpdateCreditCardRequest): Observable<CreditCard> {
    return this.http.put<CreditCard>(`${this.API}/${id}`, data);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.API}/${id}`);
  }
}
