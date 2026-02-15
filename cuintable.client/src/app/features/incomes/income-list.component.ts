import { Component, OnInit } from '@angular/core';
import { Income, IncomeService } from '../../core/services/income.service';
import { NotificationService } from '../../core/services/notification.service';

@Component({
  selector: 'app-income-list',
  standalone: false,
  template: `
    <div class="max-w-7xl mx-auto">
      <!-- Header -->
      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-white">{{ 'NAV.INCOMES' | translate }}</h1>
          <p class="text-sm text-slate-500 mt-1">{{ incomes.length }} {{ 'COMMON.RECORDS' | translate }}</p>
        </div>
        <button class="btn btn-primary btn-sm" (click)="showForm = true; editingIncome = null">
          <svg class="w-4 h-4 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
          </svg>
          {{ 'COMMON.ADD' | translate }}
        </button>
      </div>

      <!-- Loading -->
      <div *ngIf="loading" class="flex items-center justify-center py-20">
        <div class="w-8 h-8 border-2 border-cyan-500/30 border-t-cyan-500 rounded-full animate-spin"></div>
      </div>

      <!-- Empty State -->
      <div *ngIf="!loading && incomes.length === 0" class="glass-card p-12 text-center">
        <svg class="w-12 h-12 mx-auto text-slate-600 mb-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1">
          <path stroke-linecap="round" stroke-linejoin="round" d="M12 6v12m-3-2.818l.879.659c1.171.879 3.07.879 4.242 0 1.172-.879 1.172-2.303 0-3.182C13.536 12.219 12.768 12 12 12c-.725 0-1.45-.22-2.003-.659-1.106-.879-1.106-2.303 0-3.182s2.9-.879 4.006 0l.415.33M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <p class="text-slate-500">{{ 'COMMON.NO_DATA' | translate }}</p>
      </div>

      <!-- Table -->
      <div *ngIf="!loading && incomes.length > 0" class="glass-card overflow-hidden">
        <div class="overflow-x-auto">
          <table class="table w-full">
            <thead>
              <tr>
                <th>{{ 'COMMON.DATE' | translate }}</th>
                <th>{{ 'INCOME.TYPE' | translate }}</th>
                <th>{{ 'INCOME.SOURCE' | translate }}</th>
                <th class="text-right">{{ 'INCOME.AMOUNT_MXN' | translate }}</th>
                <th class="text-right">{{ 'INCOME.AMOUNT_USD' | translate }}</th>
                <th>{{ 'INCOME.INVOICES' | translate }}</th>
                <th class="text-right">{{ 'COMMON.ACTIONS' | translate }}</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let income of incomes">
                <td class="text-slate-400 text-sm">{{ income.date }}</td>
                <td>
                  <span class="text-xs font-medium px-2 py-1 rounded-md"
                        [class]="income.type === 0 ? 'badge-glow-primary' : 'badge-glow-secondary'">
                    {{ income.type === 0 ? ('INCOME.NOMINA' | translate) : ('INCOME.HONORARIOS' | translate) }}
                  </span>
                </td>
                <td class="text-slate-300 text-sm">{{ income.source }}</td>
                <td class="text-right font-mono text-sm text-emerald-400 font-medium">$ {{ income.amountMXN | number:'1.2-2' }}</td>
                <td class="text-right font-mono text-sm">
                  <span *ngIf="income.amountUSD" class="text-blue-400">$ {{ income.amountUSD | number:'1.2-2' }}</span>
                  <span *ngIf="!income.amountUSD" class="text-slate-600">&mdash;</span>
                </td>
                <td>
                  <div class="flex gap-1">
                    <span *ngIf="income.invoicePdfUrl" class="text-xs font-medium px-1.5 py-0.5 rounded badge-glow-info">PDF</span>
                    <span *ngIf="income.invoiceXmlUrl" class="text-xs font-medium px-1.5 py-0.5 rounded badge-glow-warning">XML</span>
                  </div>
                </td>
                <td class="text-right">
                  <div class="flex justify-end gap-1">
                    <button class="btn btn-ghost btn-xs text-slate-400 hover:text-cyan-400" (click)="edit(income)">
                      <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0115.75 21H5.25A2.25 2.25 0 013 18.75V8.25A2.25 2.25 0 015.25 6H10" />
                      </svg>
                    </button>
                    <button class="btn btn-ghost btn-xs text-slate-400 hover:text-red-400" (click)="confirmDelete(income)">
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
      <app-income-form
        *ngIf="showForm"
        [income]="editingIncome"
        (saved)="onSaved()"
        (cancelled)="showForm = false">
      </app-income-form>

      <!-- Delete Confirmation -->
      <div *ngIf="deletingIncome" class="modal modal-open">
        <div class="modal-overlay" (click)="deletingIncome = null"></div>
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
            <button class="btn btn-ghost flex-1" (click)="deletingIncome = null">{{ 'COMMON.CANCEL' | translate }}</button>
            <button class="btn btn-error flex-1" (click)="deleteIncome()">{{ 'COMMON.DELETE' | translate }}</button>
          </div>
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
