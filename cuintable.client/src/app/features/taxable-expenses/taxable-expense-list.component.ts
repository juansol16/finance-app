import { Component, OnInit } from '@angular/core';
import { TaxableExpense, TaxableExpenseService } from '../../core/services/taxable-expense.service';
import { CreditCard, CreditCardService } from '../../core/services/credit-card.service';
import { Expense, ExpenseService } from '../../core/services/expense.service';

const CATEGORY_KEYS = ['TAXABLE.LUZ', 'TAXABLE.INTERNET', 'TAXABLE.CELULAR',
  'TAXABLE.EQUIPO', 'TAXABLE.SOFTWARE', 'EXPENSE.OTRO'];

@Component({
  selector: 'app-taxable-expense-list',
  standalone: false,
  template: `
    <div class="max-w-7xl mx-auto">
      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-white">{{ 'NAV.DEDUCTIBLE_EXPENSES' | translate }}</h1>
          <p class="text-sm text-slate-500 mt-1">{{ items.length }} {{ 'COMMON.RECORDS' | translate }}</p>
        </div>
        <button class="btn btn-primary btn-sm" (click)="showForm = true; editing = null">
          <svg class="w-4 h-4 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
          </svg>
          {{ 'COMMON.ADD' | translate }}
        </button>
      </div>

      <div *ngIf="loading" class="flex items-center justify-center py-20">
        <div class="w-8 h-8 border-2 border-cyan-500/30 border-t-cyan-500 rounded-full animate-spin"></div>
      </div>

      <div *ngIf="!loading && items.length === 0" class="glass-card p-12 text-center">
        <svg class="w-12 h-12 mx-auto text-slate-600 mb-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1">
          <path stroke-linecap="round" stroke-linejoin="round" d="M9 14l6-6m-5.5.5h.01m4.99 5h.01M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16l3.5-2 3.5 2 3.5-2 3.5 2z" />
        </svg>
        <p class="text-slate-500">{{ 'COMMON.NO_DATA' | translate }}</p>
      </div>

      <div *ngIf="!loading && items.length > 0" class="glass-card overflow-hidden">
        <div class="overflow-x-auto">
          <table class="table w-full">
            <thead>
              <tr>
                <th>{{ 'COMMON.DATE' | translate }}</th>
                <th>{{ 'EXPENSE.CATEGORY' | translate }}</th>
                <th>{{ 'TAXABLE.VENDOR' | translate }}</th>
                <th class="text-right">{{ 'COMMON.AMOUNT' | translate }} (MXN)</th>
                <th>{{ 'TAXABLE.LINKED_PAYMENT' | translate }}</th>
                <th>{{ 'INCOME.INVOICES' | translate }}</th>
                <th class="text-right">{{ 'COMMON.ACTIONS' | translate }}</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let item of items">
                <td class="text-slate-400 text-sm">{{ item.date }}</td>
                <td>
                  <span class="text-xs font-medium px-2 py-1 rounded-md badge-glow-secondary">{{ categoryKeys[item.category] | translate }}</span>
                </td>
                <td class="text-slate-300 text-sm">{{ item.vendor }}</td>
                <td class="text-right font-mono text-sm text-red-400 font-medium">$ {{ item.amountMXN | number:'1.2-2' }}</td>
                <td class="text-sm">
                  <span *ngIf="item.linkedExpenseLabel" class="text-slate-400">{{ item.linkedExpenseLabel }}</span>
                  <span *ngIf="!item.linkedExpenseLabel" class="text-slate-600">&mdash;</span>
                </td>
                <td>
                  <div class="flex gap-1">
                    <span *ngIf="item.invoicePdfUrl" class="text-xs font-medium px-1.5 py-0.5 rounded badge-glow-info">PDF</span>
                    <span *ngIf="item.invoiceXmlUrl" class="text-xs font-medium px-1.5 py-0.5 rounded badge-glow-warning">XML</span>
                    <button *ngIf="item.xmlMetadata" class="text-xs font-medium px-1.5 py-0.5 rounded badge-glow-primary cursor-pointer"
                            (click)="showMetadata(item)">CFDI</button>
                  </div>
                </td>
                <td class="text-right">
                  <div class="flex justify-end gap-1">
                    <button class="btn btn-ghost btn-xs text-slate-400 hover:text-cyan-400" (click)="edit(item)">
                      <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0115.75 21H5.25A2.25 2.25 0 013 18.75V8.25A2.25 2.25 0 015.25 6H10" />
                      </svg>
                    </button>
                    <button class="btn btn-ghost btn-xs text-slate-400 hover:text-red-400" (click)="confirmDelete(item)">
                      <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
                      </svg>
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Form Modal -->
      <app-taxable-expense-form
        *ngIf="showForm"
        [item]="editing"
        [creditCards]="creditCards"
        [cardPayments]="cardPayments"
        (saved)="onSaved()"
        (cancelled)="showForm = false">
      </app-taxable-expense-form>

      <!-- XML Metadata Modal -->
      <div *ngIf="metadataItem" class="modal modal-open">
        <div class="modal-overlay" (click)="metadataItem = null"></div>
        <div class="modal-content w-full max-w-lg p-6">
          <h3 class="text-lg font-semibold text-white mb-4">{{ 'TAXABLE.CFDI_DATA' | translate }}</h3>
          <div class="rounded-lg p-4 overflow-auto max-h-80" style="background: rgba(0,0,0,0.3);">
            <pre class="text-sm text-slate-300 whitespace-pre-wrap font-mono">{{ parsedMetadata }}</pre>
          </div>
          <div class="mt-4 text-right">
            <button class="btn btn-ghost btn-sm" (click)="metadataItem = null">{{ 'COMMON.CANCEL' | translate }}</button>
          </div>
        </div>
      </div>

      <!-- Delete Confirmation -->
      <div *ngIf="deleting" class="modal modal-open">
        <div class="modal-overlay" (click)="deleting = null"></div>
        <div class="modal-content w-full max-w-sm p-6">
          <div class="text-center">
            <div class="w-12 h-12 rounded-full mx-auto mb-4 flex items-center justify-center" style="background: rgba(239,68,68,0.12);">
              <svg class="w-6 h-6 text-red-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
              </svg>
            </div>
            <h3 class="text-lg font-semibold text-white mb-2">{{ 'COMMON.DELETE' | translate }}</h3>
            <p class="text-sm text-slate-400 mb-6">{{ 'COMMON.CONFIRM_DELETE' | translate }}</p>
          </div>
          <div class="flex gap-3">
            <button class="btn btn-ghost flex-1" (click)="deleting = null">{{ 'COMMON.CANCEL' | translate }}</button>
            <button class="btn btn-error flex-1" (click)="deleteItem()">{{ 'COMMON.DELETE' | translate }}</button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class TaxableExpenseListComponent implements OnInit {
  items: TaxableExpense[] = [];
  creditCards: CreditCard[] = [];
  cardPayments: Expense[] = [];
  loading = true;
  showForm = false;
  editing: TaxableExpense | null = null;
  deleting: TaxableExpense | null = null;
  metadataItem: TaxableExpense | null = null;
  parsedMetadata = '';
  categoryKeys = CATEGORY_KEYS;

  constructor(
    private service: TaxableExpenseService,
    private creditCardService: CreditCardService,
    private expenseService: ExpenseService
  ) {}

  ngOnInit() { this.loadAll(); }

  loadAll() {
    this.loading = true;
    this.creditCardService.getAll().subscribe(cards => {
      this.creditCards = cards;
      this.expenseService.getAll().subscribe(expenses => {
        this.cardPayments = expenses.filter(e => e.category === 0);
        this.service.getAll().subscribe({
          next: (data) => { this.items = data; this.loading = false; },
          error: () => { this.loading = false; }
        });
      });
    });
  }

  edit(item: TaxableExpense) {
    this.editing = item;
    this.showForm = true;
  }

  showMetadata(item: TaxableExpense) {
    this.metadataItem = item;
    try {
      this.parsedMetadata = JSON.stringify(JSON.parse(item.xmlMetadata!), null, 2);
    } catch {
      this.parsedMetadata = item.xmlMetadata || '';
    }
  }

  confirmDelete(item: TaxableExpense) { this.deleting = item; }

  deleteItem() {
    if (!this.deleting) return;
    this.service.delete(this.deleting.id).subscribe({
      next: () => { this.deleting = null; this.loadAll(); }
    });
  }

  onSaved() {
    this.showForm = false;
    this.editing = null;
    this.loadAll();
  }
}
