import { Component, OnInit } from '@angular/core';
import { Income, IncomeService } from '../../core/services/income.service';
import { NotificationService } from '../../core/services/notification.service';

@Component({
  selector: 'app-income-list',
  standalone: false,
  template: `
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-2xl font-bold">{{ 'NAV.INCOMES' | translate }}</h1>
      <button class="btn btn-primary" (click)="showForm = true; editingIncome = null">
        + {{ 'COMMON.ADD' | translate }}
      </button>
    </div>

    <!-- Loading -->
    <div *ngIf="loading" class="flex justify-center py-12">
      <span class="spinner-dot-pulse"><span></span></span>
    </div>

    <!-- Empty State -->
    <div *ngIf="!loading && incomes.length === 0" class="text-center py-12 text-base-content/60">
      <p class="text-lg">{{ 'COMMON.NO_DATA' | translate }}</p>
    </div>

    <!-- Income Table -->
    <div *ngIf="!loading && incomes.length > 0" class="overflow-x-auto">
      <table class="table table-zebra w-full">
        <thead>
          <tr>
            <th>{{ 'COMMON.DATE' | translate }}</th>
            <th>{{ 'INCOME.TYPE' | translate }}</th>
            <th>{{ 'INCOME.SOURCE' | translate }}</th>
            <th class="text-right">{{ 'INCOME.AMOUNT_MXN' | translate }}</th>
            <th class="text-right">{{ 'INCOME.AMOUNT_USD' | translate }}</th>
            <th>{{ 'INCOME.INVOICES' | translate }}</th>
            <th>{{ 'COMMON.ACTIONS' | translate }}</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let income of incomes">
            <td>{{ income.date }}</td>
            <td>
              <span class="badge" [class.badge-primary]="income.type === 0"
                    [class.badge-secondary]="income.type === 1">
                {{ income.type === 0 ? ('INCOME.NOMINA' | translate) : ('INCOME.HONORARIOS' | translate) }}
              </span>
            </td>
            <td>{{ income.source }}</td>
            <td class="text-right font-mono">$ {{ income.amountMXN | number:'1.2-2' }}</td>
            <td class="text-right font-mono">
              <span *ngIf="income.amountUSD">$ {{ income.amountUSD | number:'1.2-2' }}</span>
              <span *ngIf="!income.amountUSD" class="text-base-content/30">—</span>
            </td>
            <td>
              <div class="flex gap-1">
                <span *ngIf="income.invoicePdfUrl" class="badge badge-sm badge-outline">PDF</span>
                <span *ngIf="income.invoiceXmlUrl" class="badge badge-sm badge-outline">XML</span>
              </div>
            </td>
            <td>
              <div class="flex gap-1">
                <button class="btn btn-ghost btn-xs" (click)="edit(income)">{{ 'COMMON.EDIT' | translate }}</button>
                <button class="btn btn-ghost btn-xs text-error" (click)="confirmDelete(income)">{{ 'COMMON.DELETE' | translate }}</button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Form Modal -->
    <app-income-form
      *ngIf="showForm"
      [income]="editingIncome"
      (saved)="onSaved()"
      (cancelled)="showForm = false">
    </app-income-form>

    <!-- Delete Confirmation -->
    <div *ngIf="deletingIncome" class="modal modal-open">
      <div class="modal-overlay" (click)="deletingIncome = null"></div>
      <div class="modal-content w-full max-w-md">
        <h3 class="text-lg font-bold">{{ 'COMMON.DELETE' | translate }}</h3>
        <p class="py-4">{{ 'COMMON.CONFIRM_DELETE' | translate }}</p>
        <div class="modal-action">
          <button class="btn btn-ghost" (click)="deletingIncome = null">{{ 'COMMON.CANCEL' | translate }}</button>
          <button class="btn btn-error" (click)="deleteIncome()">{{ 'COMMON.DELETE' | translate }}</button>
        </div>
      </div>
    </div>
  `
})
export class IncomeListComponent implements OnInit {
  incomes: Income[] = [];
  loading = true;
  showForm = false;
  editingIncome: Income | null = null;
  deletingIncome: Income | null = null;

  constructor(
    private incomeService: IncomeService,
    private notificationService: NotificationService
  ) { }

  ngOnInit() {
    this.loadIncomes();
  }

  loadIncomes() {
    this.loading = true;
    this.incomeService.getAll().subscribe({
      next: (data) => {
        this.incomes = data;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  edit(income: Income) {
    this.editingIncome = income;
    this.showForm = true;
  }

  confirmDelete(income: Income) {
    this.deletingIncome = income;
  }

  deleteIncome() {
    if (!this.deletingIncome) return;
    this.incomeService.delete(this.deletingIncome.id).subscribe({
      next: () => {
        this.notificationService.success('Income deleted successfully');
        this.deletingIncome = null;
        this.loadIncomes();
      },
      error: () => {
        this.notificationService.error('Error deleting income');
      }
    });
  }

  onSaved() {
    this.notificationService.success('Income saved successfully');
    this.showForm = false;
    this.editingIncome = null;
    this.loadIncomes();
  }
}
