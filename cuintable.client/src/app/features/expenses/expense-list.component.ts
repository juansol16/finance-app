import { Component, OnInit } from '@angular/core';
import { Expense, ExpenseService } from '../../core/services/expense.service';
import { CreditCard, CreditCardService } from '../../core/services/credit-card.service';

const CATEGORY_KEYS = ['EXPENSE.PAGO_TARJETA', 'EXPENSE.TRANSFERENCIA', 'EXPENSE.PAGO_COCHE',
  'EXPENSE.RETIRO_EFECTIVO', 'EXPENSE.HONORARIOS', 'EXPENSE.OTRO'];

@Component({
  selector: 'app-expense-list',
  standalone: false,
  template: `
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-2xl font-bold">{{ 'NAV.EXPENSES' | translate }}</h1>
      <button class="btn btn-primary" (click)="showForm = true; editing = null; resetForm()">
        + {{ 'COMMON.ADD' | translate }}
      </button>
    </div>

    <div *ngIf="loading" class="flex justify-center py-12">
      <span class="spinner-dot-pulse"><span></span></span>
    </div>

    <div *ngIf="!loading && expenses.length === 0" class="text-center py-12 text-base-content/60">
      <p class="text-lg">{{ 'COMMON.NO_DATA' | translate }}</p>
    </div>

    <div *ngIf="!loading && expenses.length > 0" class="overflow-x-auto">
      <table class="table table-zebra w-full">
        <thead>
          <tr>
            <th>{{ 'COMMON.DATE' | translate }}</th>
            <th>{{ 'EXPENSE.CATEGORY' | translate }}</th>
            <th>{{ 'EXPENSE.CREDIT_CARD' | translate }}</th>
            <th class="text-right">{{ 'COMMON.AMOUNT' | translate }} (MXN)</th>
            <th>{{ 'COMMON.DESCRIPTION' | translate }}</th>
            <th>{{ 'COMMON.ACTIONS' | translate }}</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let expense of expenses">
            <td>{{ expense.date }}</td>
            <td>
              <span class="badge badge-outline">{{ categoryKeys[expense.category] | translate }}</span>
            </td>
            <td>{{ expense.creditCardLabel || '—' }}</td>
            <td class="text-right font-mono">$ {{ expense.amountMXN | number:'1.2-2' }}</td>
            <td>{{ expense.description || '—' }}</td>
            <td>
              <div class="flex gap-1">
                <button class="btn btn-ghost btn-xs" (click)="edit(expense)">{{ 'COMMON.EDIT' | translate }}</button>
                <button class="btn btn-ghost btn-xs text-error" (click)="confirmDelete(expense)">{{ 'COMMON.DELETE' | translate }}</button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Form Modal -->
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
      <div class="modal-content w-full max-w-md">
        <h3 class="text-lg font-bold">{{ 'COMMON.DELETE' | translate }}</h3>
        <p class="py-4">{{ 'COMMON.CONFIRM_DELETE' | translate }}</p>
        <div class="modal-action">
          <button class="btn btn-ghost" (click)="deleting = null">{{ 'COMMON.CANCEL' | translate }}</button>
          <button class="btn btn-error" (click)="deleteExpense()">{{ 'COMMON.DELETE' | translate }}</button>
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
