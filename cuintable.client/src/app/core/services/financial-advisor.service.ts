import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

// Enum values mirror the backend (serialized as numbers)
export enum StatementStatus {
  Uploaded = 0,
  Processing = 1,
  Completed = 2,
  Failed = 3,
}

// Mirrors backend StatementAccountType
export enum StatementAccountType {
  CreditCard = 0,
  BankAccount = 1,
}

export interface StatementSummary {
  id: string;
  accountType: StatementAccountType;
  creditCardId: string | null;
  cardNickname: string | null;
  bankName: string | null;
  cardLastFour: string | null;
  periodYear: number;
  periodMonth: number;
  periodEnd: string | null;
  paymentDueDate: string | null;
  totalCharges: number | null;
  newBalance: number | null;
  minimumPayment: number | null;
  noInterestPayment: number | null;
  status: StatementStatus;
  errorMessage: string | null;
  transactionCount: number;
  suspiciousCount: number;
  antExpenseCount: number;
  createdAt: string;
}

export interface StatementTransactionItem {
  id: string;
  date: string;
  rawDescription: string;
  merchant: string;
  category: number;
  type: number;
  amountMXN: number;
  isMsi: boolean;
  msiCurrent: number | null;
  msiTotal: number | null;
  isForeign: boolean;
  isRecurring: boolean;
  isAntExpense: boolean;
  isSuspicious: boolean;
  suspiciousReason: string | null;
  reviewStatus: number;
  matchedExpenseId: string | null;
}

export interface AntCluster {
  merchant: string;
  category: number;
  count: number;
  totalMXN: number;
  annualProjectionMXN: number;
}

export interface ReconciliationSummary {
  matchedPayments: number;
  unmatchedStatementPayments: number;
  unmatchedPlatformPayments: number;
  matchedAmountMXN: number;
  unmatchedStatementAmountMXN: number;
  unmatchedPlatformAmountMXN: number;
}

export interface StatementDetail extends StatementSummary {
  periodStart: string | null;
  previousBalance: number | null;
  totalPayments: number | null;
  interestCharged: number | null;
  feesCharged: number | null;
  creditLimit: number | null;
  availableCredit: number | null;
  processedAt: string | null;
  adviceJson: string | null;
  transactions: StatementTransactionItem[];
  antClusters: AntCluster[];
  reconciliation: ReconciliationSummary | null;
}

export interface CategoryTotal {
  category: number;
  totalMXN: number;
  count: number;
}

export interface TrendPoint {
  year: number;
  month: number;
  totalChargesMXN: number;
}

export interface MonthlyAdvice {
  adviceJson: string;
  generatedAt: string;
  statementCount: number;
  isStale: boolean;
}

export interface AdvisorDashboard {
  year: number;
  month: number;
  statementCount: number;
  totalChargesMXN: number;
  antTotalMXN: number;
  antAnnualProjectionMXN: number;
  suspiciousCount: number;
  suspiciousPendingCount: number;
  subscriptionsMXN: number;
  msiLoadMXN: number;
  interestAndFeesMXN: number;
  categoryTotals: CategoryTotal[];
  trend: TrendPoint[];
  antClusters: AntCluster[];
  reconciliation: ReconciliationSummary | null;
  monthlyAdvice: MonthlyAdvice | null;
}

export interface StatementAdvice {
  summary: string;
  suggestions: { title: string; detail: string; impactMXN?: number | null }[];
}

@Injectable({ providedIn: 'root' })
export class FinancialAdvisorService {
  private readonly API = '/api/financial-advisor';

  constructor(private http: HttpClient) {}

  upload(file: File, creditCardId?: string | null): Observable<StatementDetail> {
    const form = new FormData();
    form.append('pdf', file);
    if (creditCardId) form.append('creditCardId', creditCardId);
    return this.http.post<StatementDetail>(`${this.API}/statements`, form);
  }

  getStatements(year?: number, creditCardId?: string): Observable<StatementSummary[]> {
    const params: any = {};
    if (year) params.year = year;
    if (creditCardId) params.creditCardId = creditCardId;
    return this.http.get<StatementSummary[]>(`${this.API}/statements`, { params });
  }

  getStatement(id: string): Observable<StatementDetail> {
    return this.http.get<StatementDetail>(`${this.API}/statements/${id}`);
  }

  reprocess(id: string): Observable<StatementDetail> {
    return this.http.post<StatementDetail>(`${this.API}/statements/${id}/reprocess`, {});
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.API}/statements/${id}`);
  }

  getFileBlob(id: string): Observable<Blob> {
    return this.http.get(`${this.API}/statements/${id}/file`, { responseType: 'blob' });
  }

  review(
    transactionId: string,
    status: 'Recognized' | 'NotMine',
  ): Observable<StatementTransactionItem> {
    return this.http.put<StatementTransactionItem>(
      `${this.API}/transactions/${transactionId}/review`,
      { status },
    );
  }

  getDashboard(year: number, month: number): Observable<AdvisorDashboard> {
    return this.http.get<AdvisorDashboard>(`${this.API}/dashboard`, { params: { year, month } });
  }

  generateMonthlyAdvice(year: number, month: number): Observable<MonthlyAdvice> {
    return this.http.post<MonthlyAdvice>(
      `${this.API}/monthly-advice/generate`,
      {},
      { params: { year, month } },
    );
  }

  /** Safely parses the advice JSON produced by the model. */
  parseAdvice(adviceJson: string | null): StatementAdvice | null {
    if (!adviceJson) return null;
    try {
      const parsed = JSON.parse(adviceJson);
      if (parsed && typeof parsed.summary === 'string' && Array.isArray(parsed.suggestions)) {
        return parsed as StatementAdvice;
      }
      return null;
    } catch {
      return null;
    }
  }
}
