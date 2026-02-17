import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Income, IncomeService } from '../../core/services/income.service';

@Component({
  selector: 'app-income-form',
  standalone: false,
  template: `
    <div class="modal modal-open">
      <div class="modal-overlay" (click)="cancelled.emit()"></div>
      <div class="modal-content w-full max-w-lg p-6">
        <h3 class="text-lg font-bold mb-4">
          {{ income ? ('COMMON.EDIT' | translate) : ('COMMON.ADD' | translate) }} {{ 'NAV.INCOMES' | translate }}
        </h3>

        <!-- Type -->
        <div class="form-group mb-3">
          <label class="form-label">{{ 'INCOME.TYPE' | translate }}</label>
          <select class="select select-bordered w-full" [(ngModel)]="form.type">
            <option class="bg-slate-800" [ngValue]="0">{{ 'INCOME.NOMINA' | translate }}</option>
            <option class="bg-slate-800" [ngValue]="1">{{ 'INCOME.HONORARIOS' | translate }}</option>
          </select>
        </div>

        <!-- Source -->
        <div class="form-group mb-3">
          <label class="form-label">{{ 'INCOME.SOURCE' | translate }}</label>
          <input type="text" class="input input-bordered w-full" [(ngModel)]="form.source"
                 [placeholder]="form.type === 0 ? 'Company name' : 'Client name'" />
        </div>

        <!-- Date -->
        <div class="form-group mb-3">
          <label class="form-label">{{ 'COMMON.DATE' | translate }}</label>
          <input type="date" class="input input-bordered w-full" [(ngModel)]="form.date" />
        </div>

        <!-- Amount MXN -->
        <div class="form-group mb-3">
          <label class="form-label">{{ 'INCOME.AMOUNT_MXN' | translate }}</label>
          <input type="number" class="input input-bordered w-full" [(ngModel)]="form.amountMXN"
                 step="0.01" min="0" />
        </div>

        <!-- Nomina-only fields -->
        <div *ngIf="form.type === 0" class="grid grid-cols-2 gap-3 mb-3">
          <div class="form-group">
            <label class="form-label">{{ 'INCOME.EXCHANGE_RATE' | translate }}</label>
            <input type="number" class="input input-bordered w-full" [(ngModel)]="form.exchangeRate"
                   step="0.0001" min="0" (ngModelChange)="calculateUSD()" />
          </div>
          <div class="form-group">
            <label class="form-label">{{ 'INCOME.AMOUNT_USD' | translate }}</label>
            <input type="number" class="input input-bordered w-full" [(ngModel)]="form.amountUSD"
                   step="0.01" min="0" />
          </div>
        </div>

        <!-- Description -->
        <div class="form-group mb-3">
          <label class="form-label">{{ 'COMMON.DESCRIPTION' | translate }}</label>
          <textarea class="textarea textarea-bordered w-full" [(ngModel)]="form.description" rows="2"></textarea>
        </div>

        <!-- File Upload (only on edit) -->
        <div *ngIf="income" class="form-group mb-3">
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
        </div>

        <!-- Actions -->
        <div class="modal-action">
          <button class="btn btn-ghost" (click)="cancelled.emit()">{{ 'COMMON.CANCEL' | translate }}</button>
          <button class="btn btn-primary" [disabled]="saving" (click)="save()">
            <span *ngIf="saving" class="spinner-dot-pulse spinner-sm"><span></span></span>
            {{ 'COMMON.SAVE' | translate }}
          </button>
        </div>
      </div>
    </div>
  `
})
export class IncomeFormComponent implements OnInit {
  @Input() income: Income | null = null;
  @Output() saved = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  form = {
    type: 0,
    source: '',
    date: '',
    amountMXN: 0,
    exchangeRate: null as number | null,
    amountUSD: null as number | null,
    description: ''
  };

  pdfFile: File | null = null;
  xmlFile: File | null = null;
  saving = false;

  constructor(private incomeService: IncomeService) { }

  ngOnInit() {
    if (this.income) {
      this.form = {
        type: this.income.type,
        source: this.income.source,
        date: this.income.date,
        amountMXN: this.income.amountMXN,
        exchangeRate: this.income.exchangeRate,
        amountUSD: this.income.amountUSD,
        description: this.income.description || ''
      };
    }
  }

  calculateUSD() {
    if (this.form.exchangeRate && this.form.amountMXN) {
      this.form.amountUSD = Math.round((this.form.amountMXN / this.form.exchangeRate) * 100) / 100;
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
      type: this.form.type,
      source: this.form.source,
      date: this.form.date,
      amountMXN: this.form.amountMXN,
      exchangeRate: this.form.type === 0 ? this.form.exchangeRate : null,
      amountUSD: this.form.type === 0 ? this.form.amountUSD : null,
      description: this.form.description || null
    };

    const save$ = this.income
      ? this.incomeService.update(this.income.id, request)
      : this.incomeService.create(request);

    save$.subscribe({
      next: (result) => {
        if (this.pdfFile || this.xmlFile) {
          this.incomeService.uploadFiles(result.id, this.pdfFile || undefined, this.xmlFile || undefined)
            .subscribe({
              next: () => { this.saving = false; this.saved.emit(); },
              error: () => { this.saving = false; this.saved.emit(); }
            });
        } else {
          this.saving = false;
          this.saved.emit();
        }
      },
      error: () => {
        this.saving = false;
      }
    });
  }
}
