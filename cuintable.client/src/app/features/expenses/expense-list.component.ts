import { Component, OnInit } from '@angular/core';
import { Expense, ExpenseService } from '../../core/services/expense.service';
import { CreditCard, CreditCardService } from '../../core/services/credit-card.service';

const CATEGORY_KEYS = ['EXPENSE.PAGO_TARJETA', 'EXPENSE.TRANSFERENCIA', 'EXPENSE.PAGO_COCHE',
  'EXPENSE.RETIRO_EFECTIVO', 'EXPENSE.HONORARIOS', 'EXPENSE.OTRO'];

@Component({
  selector: 'app-expense-list',
  standalone: false,
  template: `
    <div class="max-w-7xl mx-auto">
      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-white">{{ 'NAV.EXPENSES' | translate }}</h1>
          <p class="text-sm text-slate-500 mt-1">{{ expenses.length }} {{ 'COMMON.RECORDS' | translate }}</p>
        </div>
        <button class="btn btn-primary btn-sm" (click)="showForm = true; editing = null; resetForm()">
          <svg class="w-4 h-4 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
          </svg>
          {{ 'COMMON.ADD' | translate }}
        </button>
      </div>

      <div *ngIf="loading" class="flex items-center justify-center py-20">
        <div class="w-8 h-8 border-2 border-cyan-500/30 border-t-cyan-500 rounded-full animate-spin"></div>
      </div>

      <div *ngIf="!loading && expenses.length === 0" class="glass-card p-12 text-center">
        <svg class="w-12 h-12 mx-auto text-slate-600 mb-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1">
          <path stroke-linecap="round" stroke-linejoin="round" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
        </svg>
        <p class="text-slate-500">{{ 'COMMON.NO_DATA' | translate }}</p>
      </div>

      <div *ngIf="!loading && expenses.length > 0" class="glass-card overflow-hidden">
        <div class="overflow-x-auto">
          <table class="table w-full">
            <thead>
              <tr>
                <th>{{ 'COMMON.DATE' | translate }}</th>
                <th>{{ 'EXPENSE.CATEGORY' | translate }}</th>
                <th>{{ 'EXPENSE.CREDIT_CARD' | translate }}</th>
                <th class="text-right">{{ 'COMMON.AMOUNT' | translate }} (MXN)</th>
                <th>{{ 'COMMON.DESCRIPTION' | translate }}</th>
                <th class="text-right">{{ 'COMMON.ACTIONS' | translate }}</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let expense of expenses">
                <td class="text-slate-400 text-sm">{{ expense.date }}</td>
                <td>
                  <span class="text-xs font-medium px-2 py-1 rounded-md badge-glow-info">{{ categoryKeys[expense.category] | translate }}</span>
                </td>
                <td class="text-slate-400 text-sm">{{ expense.creditCardLabel || '&mdash;' }}</td>
                <td class="text-right font-mono text-sm text-red-400 font-medium">$ {{ expense.amountMXN | number:'1.2-2' }}</td>
                <td class="text-slate-400 text-sm max-w-[200px] truncate">{{ expense.description || '&mdash;' }}</td>
                <td class="text-right">
                  <div class="flex justify-end gap-1">
                    <button class="btn btn-ghost btn-xs text-slate-400 hover:text-cyan-400" (click)="edit(expense)">
                      <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0115.75 21H5.25A2.25 2.25 0 013 18.75V8.25A2.25 2.25 0 015.25 6H10" />
                      </svg>
                    </button>
                    <button class="btn btn-ghost btn-xs text-slate-400 hover:text-red-400" (click)="confirmDelete(expense)">
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

      <app-expense-form
        *ngIf="showForm"
        [expense]="editing"
        [creditCards]="creditCards"
        (saved)="onSaved()"
        (cancelled)="showForm = false">
      </app-expense-form>

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
            <button class="btn btn-error flex-1" (click)="deleteExpense()">{{ 'COMMON.DELETE' | translate }}</button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class ExpenseListComponent implements OnInit {
  expenses: Expense[] = [];
  creditCards: CreditCard[] = [];
  loading = true;
  showForm = false;
  editing: Expense | null = null;
  deleting: Expense | null = null;
  categoryKeys = CATEGORY_KEYS;

  constructor(
    private expenseService: ExpenseService,
    private creditCardService: CreditCardService
  ) {}

  ngOnInit() {
    this.loadAll();
  }

  loadAll() {
    this.loading = true;
    this.creditCardService.getAll().subscribe(cards => {
      this.creditCards = cards;
      this.expenseService.getAll().subscribe({
        next: (data) => { this.expenses = data; this.loading = false; },
        error: () => { this.loading = false; }
      });
    });
  }

  edit(expense: Expense) {
    this.editing = expense;
    this.showForm = true;
  }

  resetForm() {
    this.editing = null;
  }

  confirmDelete(expense: Expense) { this.deleting = expense; }

  deleteExpense() {
    if (!this.deleting) return;
    this.expenseService.delete(this.deleting.id).subscribe({
      next: () => { this.deleting = null; this.loadAll(); }
    });
  }

  onSaved() {
    this.showForm = false;
    this.editing = null;
    this.loadAll();
  }
}
