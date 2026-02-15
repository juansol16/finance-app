import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { TaxableExpense, TaxableExpenseService } from '../../core/services/taxable-expense.service';
import { CreditCard } from '../../core/services/credit-card.service';
import { Expense } from '../../core/services/expense.service';

@Component({
  selector: 'app-taxable-expense-form',
  standalone: false,
  template: `
    <div class="modal modal-open">
      <div class="modal-overlay" (click)="cancelled.emit()"></div>
      <div class="modal-content w-full max-w-lg">
        <h3 class="text-lg font-bold mb-4">
          {{ item ? ('COMMON.EDIT' | translate) : ('COMMON.ADD' | translate) }} {{ 'NAV.DEDUCTIBLE_EXPENSES' | translate }}
        </h3>

        <!-- Category -->
        <div class="form-group mb-3">
          <label class="form-label">{{ 'EXPENSE.CATEGORY' | translate }}</label>
          <select class="select select-bordered w-full" [(ngModel)]="form.category">
            <option [ngValue]="0">{{ 'TAXABLE.LUZ' | translate }}</option>
            <option [ngValue]="1">{{ 'TAXABLE.INTERNET' | translate }}</option>
            <option [ngValue]="2">{{ 'TAXABLE.CELULAR' | translate }}</option>
            <option [ngValue]="3">{{ 'TAXABLE.EQUIPO' | translate }}</option>
            <option [ngValue]="4">{{ 'TAXABLE.SOFTWARE' | translate }}</option>
            <option [ngValue]="5">{{ 'EXPENSE.OTRO' | translate }}</option>
          </select>
        </div>

        <!-- Vendor -->
        <div class="form-group mb-3">
          <label class="form-label">{{ 'TAXABLE.VENDOR' | translate }}</label>
          <input type="text" class="input input-bordered w-full" [(ngModel)]="form.vendor"
                 placeholder="CFE, Telmex, Amazon..." />
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

        <!-- Credit Card (optional) -->
        <div class="form-group mb-3">
          <label class="form-label">{{ 'TAXABLE.PAID_WITH' | translate }}</label>
          <select class="select select-bordered w-full" [(ngModel)]="form.creditCardId">
            <option [ngValue]="null">{{ 'TAXABLE.NO_CARD' | translate }}</option>
            <option *ngFor="let card of creditCards" [ngValue]="card.id">
              {{ card.nickname }} (****{{ card.lastFourDigits }})
            </option>
          </select>
        </div>

        <!-- Link to Card Payment (optional) -->
        <div *ngIf="cardPayments.length > 0" class="form-group mb-3">
          <label class="form-label">{{ 'TAXABLE.LINKED_PAYMENT' | translate }}</label>
          <select class="select select-bordered w-full" [(ngModel)]="form.expenseId">
            <option [ngValue]="null">{{ 'TAXABLE.NO_LINK' | translate }}</option>
            <option *ngFor="let payment of cardPayments" [ngValue]="payment.id">
              {{ payment.date }} — {{ payment.creditCardLabel }} — {{ '$' }}{{ payment.amountMXN | number:'1.2-2' }}
            </option>
          </select>
        </div>

        <!-- Description -->
        <div class="form-group mb-3">
          <label class="form-label">{{ 'COMMON.DESCRIPTION' | translate }}</label>
          <textarea class="textarea textarea-bordered w-full" [(ngModel)]="form.description" rows="2"></textarea>
        </div>

        <!-- File Upload (only on edit) -->
        <div *ngIf="item" class="form-group mb-3">
          <label class="form-label">{{ 'INCOME.INVOICES' | translate }}</label>
          <div class="flex gap-2">
            <div class="flex-1">
              <label class="btn btn-outline btn-sm w-full">
                PDF
                <input type="file" accept=".pdf" class="hidden" (change)="onPdfSelected($event)" />
              </label>
              <span *ngIf="pdfFile" class="text-xs text-success mt-1 block">{{ pdfFile.name }}</span>
            </div>
            <div class="flex-1">
              <label class="btn btn-outline btn-sm w-full">
                XML
                <input type="file" accept=".xml" class="hidden" (change)="onXmlSelected($event)" />
              </label>
              <span *ngIf="xmlFile" class="text-xs text-success mt-1 block">{{ xmlFile.name }}</span>
            </div>
          </div>
          <p class="text-xs text-base-content/50 mt-1">{{ 'TAXABLE.XML_HINT' | translate }}</p>
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
export class TaxableExpenseFormComponent implements OnInit {
  @Input() item: TaxableExpense | null = null;
  @Input() creditCards: CreditCard[] = [];
  @Input() cardPayments: Expense[] = [];
  @Output() saved = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  form = {
    category: 0,
    vendor: '',
    date: '',
    amountMXN: 0,
    creditCardId: null as string | null,
    expenseId: null as string | null,
    description: ''
  };

  pdfFile: File | null = null;
  xmlFile: File | null = null;
  saving = false;

  constructor(private service: TaxableExpenseService) {}

  ngOnInit() {
    if (this.item) {
      this.form = {
        category: this.item.category,
        vendor: this.item.vendor,
        date: this.item.date,
        amountMXN: this.item.amountMXN,
        creditCardId: this.item.creditCardId,
        expenseId: this.item.expenseId,
        description: this.item.description || ''
      };
    }
  }

  onPdfSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    this.pdfFile = input.files?.[0] || null;
  }

  onXmlSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    this.xmlFile = input.files?.[0] || null;
  }

  save() {
    this.saving = true;
    const request = {
      category: this.form.category,
      vendor: this.form.vendor,
      date: this.form.date,
      amountMXN: this.form.amountMXN,
      creditCardId: this.form.creditCardId,
      expenseId: this.form.expenseId,
      description: this.form.description || null
    };

    const save$ = this.item
      ? this.service.update(this.item.id, request)
      : this.service.create(request);

    save$.subscribe({
      next: (result) => {
        if (this.pdfFile || this.xmlFile) {
          this.service.uploadFiles(result.id, this.pdfFile || undefined, this.xmlFile || undefined)
            .subscribe({
              next: () => { this.saving = false; this.saved.emit(); },
              error: () => { this.saving = false; this.saved.emit(); }
            });
        } else {
          this.saving = false;
          this.saved.emit();
        }
      },
      error: () => { this.saving = false; }
    });
  }
}
