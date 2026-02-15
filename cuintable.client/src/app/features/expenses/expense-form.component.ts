import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Expense, ExpenseService } from '../../core/services/expense.service';
import { CreditCard } from '../../core/services/credit-card.service';

@Component({
  selector: 'app-expense-form',
  standalone: false,
  template: `
    <div class="modal modal-open">
      <div class="modal-overlay" (click)="cancelled.emit()"></div>
      <div class="modal-content w-full max-w-lg">
        <h3 class="text-lg font-bold mb-4">
          {{ expense ? ('COMMON.EDIT' | translate) : ('COMMON.ADD' | translate) }} {{ 'NAV.EXPENSES' | translate }}
        </h3>

        <!-- Category -->
        <div class="form-group mb-3">
          <label class="form-label">{{ 'EXPENSE.CATEGORY' | translate }}</label>
          <select class="select select-bordered w-full" [(ngModel)]="form.category">
            <option [ngValue]="0">{{ 'EXPENSE.PAGO_TARJETA' | translate }}</option>
            <option [ngValue]="1">{{ 'EXPENSE.TRANSFERENCIA' | translate }}</option>
            <option [ngValue]="2">{{ 'EXPENSE.PAGO_COCHE' | translate }}</option>
            <option [ngValue]="3">{{ 'EXPENSE.RETIRO_EFECTIVO' | translate }}</option>
            <option [ngValue]="4">{{ 'EXPENSE.HONORARIOS' | translate }}</option>
            <option [ngValue]="5">{{ 'EXPENSE.OTRO' | translate }}</option>
          </select>
        </div>

        <!-- Credit Card (only for PagoTarjeta) -->
        <div *ngIf="form.category === 0" class="form-group mb-3">
          <label class="form-label">{{ 'EXPENSE.CREDIT_CARD' | translate }}</label>
          <select class="select select-bordered w-full" [(ngModel)]="form.creditCardId">
            <option [ngValue]="null" disabled>{{ 'EXPENSE.SELECT_CARD' | translate }}</option>
            <option *ngFor="let card of creditCards" [ngValue]="card.id">
              {{ card.nickname }} (****{{ card.lastFourDigits }})
            </option>
          </select>
        </div>

        <!-- Date -->
        <div class="form-group mb-3">
          <label class="form-label">{{ 'COMMON.DATE' | translate }}</label>
          <input type="date" class="input input-bordered w-full" [(ngModel)]="form.date" />
        </div>

        <!-- Amount -->
        <div class="form-group mb-3">
          <label class="form-label">{{ 'COMMON.AMOUNT' | translate }} (MXN)</label>
          <input type="number" class="input input-bordered w-full" [(ngModel)]="form.amountMXN"
                 step="0.01" min="0" />
        </div>

        <!-- Description -->
        <div class="form-group mb-3">
          <label class="form-label">{{ 'COMMON.DESCRIPTION' | translate }}</label>
          <textarea class="textarea textarea-bordered w-full" [(ngModel)]="form.description" rows="2"></textarea>
        </div>

        <!-- Actions -->
        <div class="modal-action">
          <button class="btn btn-ghost" (click)="cancelled.emit()">{{ 'COMMON.CANCEL' | translate }}</button>
          <button class="btn btn-primary" [disabled]="saving" (click)="save()">
            {{ 'COMMON.SAVE' | translate }}
          </button>
        </div>
      </div>
    </div>
  `
})
export class ExpenseFormComponent implements OnInit {
  @Input() expense: Expense | null = null;
  @Input() creditCards: CreditCard[] = [];
  @Output() saved = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  form = {
    category: 0,
    creditCardId: null as string | null,
    date: '',
    amountMXN: 0,
    description: ''
  };
  saving = false;

  constructor(private expenseService: ExpenseService) {}

  ngOnInit() {
    if (this.expense) {
      this.form = {
        category: this.expense.category,
        creditCardId: this.expense.creditCardId,
        date: this.expense.date,
        amountMXN: this.expense.amountMXN,
        description: this.expense.description || ''
      };
    }
  }

  save() {
    this.saving = true;
    const request = {
      category: this.form.category,
      creditCardId: this.form.category === 0 ? this.form.creditCardId : null,
      date: this.form.date,
      amountMXN: this.form.amountMXN,
      description: this.form.description || null
    };

    const save$ = this.expense
      ? this.expenseService.update(this.expense.id, request)
      : this.expenseService.create(request);

    save$.subscribe({
      next: () => { this.saving = false; this.saved.emit(); },
      error: () => { this.saving = false; }
    });
  }
}
