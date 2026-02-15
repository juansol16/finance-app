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
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-2xl font-bold">{{ 'NAV.DEDUCTIBLE_EXPENSES' | translate }}</h1>
      <button class="btn btn-primary" (click)="showForm = true; editing = null">
        + {{ 'COMMON.ADD' | translate }}
      </button>
    </div>

    <div *ngIf="loading" class="flex justify-center py-12">
      <span class="spinner-dot-pulse"><span></span></span>
    </div>

    <div *ngIf="!loading && items.length === 0" class="text-center py-12 text-base-content/60">
      <p class="text-lg">{{ 'COMMON.NO_DATA' | translate }}</p>
    </div>

    <div *ngIf="!loading && items.length > 0" class="overflow-x-auto">
      <table class="table table-zebra w-full">
        <thead>
          <tr>
            <th>{{ 'COMMON.DATE' | translate }}</th>
            <th>{{ 'EXPENSE.CATEGORY' | translate }}</th>
            <th>{{ 'TAXABLE.VENDOR' | translate }}</th>
            <th class="text-right">{{ 'COMMON.AMOUNT' | translate }} (MXN)</th>
            <th>{{ 'TAXABLE.LINKED_PAYMENT' | translate }}</th>
            <th>{{ 'INCOME.INVOICES' | translate }}</th>
            <th>{{ 'COMMON.ACTIONS' | translate }}</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let item of items">
            <td>{{ item.date }}</td>
            <td>
              <span class="badge badge-outline">{{ categoryKeys[item.category] | translate }}</span>
            </td>
            <td>{{ item.vendor }}</td>
            <td class="text-right font-mono">$ {{ item.amountMXN | number:'1.2-2' }}</td>
            <td>
              <span *ngIf="item.linkedExpenseLabel" class="text-sm">{{ item.linkedExpenseLabel }}</span>
              <span *ngIf="!item.linkedExpenseLabel" class="text-base-content/30">—</span>
            </td>
            <td>
              <div class="flex gap-1">
                <span *ngIf="item.invoicePdfUrl" class="badge badge-sm badge-outline">PDF</span>
                <span *ngIf="item.invoiceXmlUrl" class="badge badge-sm badge-outline">XML</span>
                <button *ngIf="item.xmlMetadata" class="badge badge-sm badge-info cursor-pointer"
                        (click)="showMetadata(item)">CFDI</button>
              </div>
            </td>
            <td>
              <div class="flex gap-1">
                <button class="btn btn-ghost btn-xs" (click)="edit(item)">{{ 'COMMON.EDIT' | translate }}</button>
                <button class="btn btn-ghost btn-xs text-error" (click)="confirmDelete(item)">{{ 'COMMON.DELETE' | translate }}</button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
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
      <div class="modal-content w-full max-w-lg">
        <h3 class="text-lg font-bold mb-4">{{ 'TAXABLE.CFDI_DATA' | translate }}</h3>
        <div class="bg-base-300 rounded-lg p-4 overflow-auto max-h-80">
          <pre class="text-sm whitespace-pre-wrap">{{ parsedMetadata }}</pre>
        </div>
        <div class="modal-action">
          <button class="btn btn-ghost" (click)="metadataItem = null">{{ 'COMMON.CANCEL' | translate }}</button>
        </div>
      </div>
    </div>

    <!-- Delete Confirmation -->
    <div *ngIf="deleting" class="modal modal-open">
      <div class="modal-overlay" (click)="deleting = null"></div>
      <div class="modal-content w-full max-w-md">
        <h3 class="text-lg font-bold">{{ 'COMMON.DELETE' | translate }}</h3>
        <p class="py-4">{{ 'COMMON.CONFIRM_DELETE' | translate }}</p>
        <div class="modal-action">
          <button class="btn btn-ghost" (click)="deleting = null">{{ 'COMMON.CANCEL' | translate }}</button>
          <button class="btn btn-error" (click)="deleteItem()">{{ 'COMMON.DELETE' | translate }}</button>
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
        // Only show PagoTarjeta expenses as linkable
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
