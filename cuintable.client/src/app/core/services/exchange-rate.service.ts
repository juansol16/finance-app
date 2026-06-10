import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';

interface FrankfurterResponse {
  base: string;
  date: string;
  rates: { MXN: number };
}

/**
 * Current USD/MXN rate from the free, key-less frankfurter.dev API
 * (ECB reference rates, updated each working day ~16:00 CET).
 */
@Injectable({ providedIn: 'root' })
export class ExchangeRateService {
  private readonly API = 'https://api.frankfurter.dev/v1/latest?base=USD&symbols=MXN';

  constructor(private http: HttpClient) { }

  getCurrentUsdMxn(): Observable<{ rate: number; date: string }> {
    return this.http.get<FrankfurterResponse>(this.API).pipe(
      map(r => ({ rate: r.rates.MXN, date: r.date }))
    );
  }
}
