import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import {
  FinancialAdvisorService,
  StatementAdvice,
  StatementDetail,
  StatementStatus,
  StatementTransactionItem,
} from '../../core/services/financial-advisor.service';
import { categoryKey, reasonKeys, statusKey, typeKey } from './advisor-labels';

type TransactionFilter = 'all' | 'ant' | 'suspicious' | 'msi' | 'payments';

@Component({
  selector: 'app-statement-detail',
  templateUrl: './statement-detail.component.html',
  standalone: false,
})
export class StatementDetailComponent implements OnInit {
  readonly StatementStatus = StatementStatus;
  readonly categoryKey = categoryKey;
  readonly typeKey = typeKey;
  readonly statusKey = statusKey;
  readonly reasonKeys = reasonKeys;

  statement: StatementDetail | null = null;
  advice: StatementAdvice | null = null;
  loading = true;
  reprocessing = false;
  filter: TransactionFilter = 'all';
  reviewingId: string | null = null;

  readonly filters: TransactionFilter[] = ['all', 'ant', 'suspicious', 'msi', 'payments'];

  constructor(
    private route: ActivatedRoute,
    private service: FinancialAdvisorService,
    private translate: TranslateService,
  ) {}

  ngOnInit(): void {
    this.load();
  }

  private get id(): string {
    return this.route.snapshot.paramMap.get('id')!;
  }

  load(): void {
    this.loading = true;
    this.service.getStatement(this.id).subscribe({
      next: (data) => {
        this.statement = data;
        this.advice = this.service.parseAdvice(data.adviceJson);
        this.loading = false;
      },
      error: () => {
        this.statement = null;
        this.loading = false;
      },
    });
  }

  get filteredTransactions(): StatementTransactionItem[] {
    const txns = this.statement?.transactions ?? [];
    switch (this.filter) {
      case 'ant':
        return txns.filter((t) => t.isAntExpense);
      case 'suspicious':
        return txns.filter((t) => t.isSuspicious);
      case 'msi':
        return txns.filter((t) => t.isMsi);
      case 'payments':
        return txns.filter((t) => t.type === 1);
      default:
        return txns;
    }
  }

  filterCount(filter: TransactionFilter): number {
    const txns = this.statement?.transactions ?? [];
    switch (filter) {
      case 'ant':
        return txns.filter((t) => t.isAntExpense).length;
      case 'suspicious':
        return txns.filter((t) => t.isSuspicious).length;
      case 'msi':
        return txns.filter((t) => t.isMsi).length;
      case 'payments':
        return txns.filter((t) => t.type === 1).length;
      default:
        return txns.length;
    }
  }

  filterKey(filter: TransactionFilter): string {
    return `ADVISOR.FILTER_${filter.toUpperCase()}`;
  }

  monthLabel(): string {
    if (!this.statement) return '';
    const month = this.translate.instant(`DASHBOARD.MONTH_${this.statement.periodMonth}`);
    return `${month} ${this.statement.periodYear}`;
  }

  review(t: StatementTransactionItem, status: 'Recognized' | 'NotMine'): void {
    this.reviewingId = t.id;
    this.service.review(t.id, status).subscribe({
      next: (updated) => {
        t.reviewStatus = updated.reviewStatus;
        this.reviewingId = null;
      },
      error: () => (this.reviewingId = null),
    });
  }

  reprocess(): void {
    this.reprocessing = true;
    this.service.reprocess(this.id).subscribe({
      next: () => {
        this.reprocessing = false;
        this.load();
      },
      error: () => {
        this.reprocessing = false;
        this.load();
      },
    });
  }

  viewPdf(): void {
    this.service.getFileBlob(this.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        window.open(url, '_blank');
        setTimeout(() => URL.revokeObjectURL(url), 60_000);
      },
    });
  }
}
