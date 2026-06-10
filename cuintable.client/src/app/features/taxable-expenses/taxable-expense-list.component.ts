import { Component, OnInit } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
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

      <!-- Summary Cards -->
      <div *ngIf="!loading && items.length > 0" class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        <app-summary-card
          [title]="'DASHBOARD.DEDUCTIBLE_EXPENSES' | translate"
          [value]="(summaryMetrics.total | currency:'MXN':'symbol-narrow':'1.0-0') || ''"
          color="success">
          <svg icon class="w-4 h-4 text-emerald-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
             <path stroke-linecap="round" stroke-linejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
        </app-summary-card>

        <app-summary-card
          title="Avg. Ticket"
          [value]="(summaryMetrics.averageTicket | currency:'MXN':'symbol-narrow':'1.0-0') || ''"
          color="info">
          <svg icon class="w-4 h-4 text-blue-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
             <path stroke-linecap="round" stroke-linejoin="round" d="M16.5 6v.75m0 3v.75m0 3v.75m0 3V18m-9-5.25h5.25M7.5 15h3M3.375 5.25c-.621 0-1.125.504-1.125 1.125v3.026a2.999 2.999 0 010 5.198v3.026c0 .621.504 1.125 1.125 1.125h17.25c.621 0 1.125-.504 1.125-1.125v-3.026a2.999 2.999 0 010-5.198V6.375c0-.621-.504-1.125-1.125-1.125H3.375z" />
          </svg>
        </app-summary-card>

        <app-summary-card
          title="Top Vendor"
          [value]="summaryMetrics.topVendor || '-'"
           [subtext]="(summaryMetrics.topVendorAmount | currency:'MXN':'symbol-narrow':'1.0-0') || ''"
          color="primary">
          <svg icon class="w-4 h-4 text-cyan-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
             <path stroke-linecap="round" stroke-linejoin="round" d="M13.5 21v-7.5a.75.75 0 01.75-.75h3a.75.75 0 01.75.75V21m-4.5 0H2.36m11.14 0H18m0 0h3.64m-1.39 0V9.349m-16.5 11.65V9.35m0 0a3.001 3.001 0 003.75-.615A2.993 2.993 0 009.75 9.75c.896 0 1.7-.393 2.25-1.016a2.993 2.993 0 002.25 1.016c.896 0 1.7-.393 2.25-1.016a3.001 3.001 0 003.75.614m-16.5 0a3.004 3.004 0 01-.621-4.72L4.318 3.44A1.5 1.5 0 015.378 3h13.243a1.5 1.5 0 011.06.44l1.19 1.189a3 3 0 01-.621 4.72m-13.5 8.65h3.75a.75.75 0 00.75-.75V13.5a.75.75 0 00-.75-.75H6.75a.75.75 0 00-.75.75v3.75c0 .415.336.75.75.75z" />
          </svg>
        </app-summary-card>

         <app-summary-card
          title="Invoiced %"
          [value]="((summaryMetrics.invoicedCount / items.length) | percent:'1.0-0') || ''"
          color="warning">
          <svg icon class="w-4 h-4 text-amber-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
             <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" />
          </svg>
        </app-summary-card>
      </div>

      <!-- Filters -->
      <div class="flex items-center gap-2 mb-6">
        <div class="form-control">
          <label class="label py-1"><span class="label-text text-xs text-slate-400">{{ 'COMMON.START_DATE' | translate }}</span></label>
          <input type="date" class="input input-sm input-bordered bg-white/[0.04] border-white/[0.08] text-slate-300" 
                 [(ngModel)]="startDate" (change)="loadAll()">
        </div>
        <div class="form-control">
          <label class="label py-1"><span class="label-text text-xs text-slate-400">{{ 'COMMON.END_DATE' | translate }}</span></label>
          <input type="date" class="input input-sm input-bordered bg-white/[0.04] border-white/[0.08] text-slate-300" 
                 [(ngModel)]="endDate" (change)="loadAll()">
        </div>
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
                    <button *ngIf="item.invoicePdfUrl" class="text-xs font-medium px-1.5 py-0.5 rounded badge-glow-info cursor-pointer"
                            (click)="previewPdf(item)" title="{{ 'TAXABLE.PREVIEW_PDF' | translate }}">
                      <svg class="w-3 h-3 inline mr-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z" />
                        <path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                      </svg>PDF
                    </button>
                    <button *ngIf="item.invoiceXmlUrl" class="text-xs font-medium px-1.5 py-0.5 rounded badge-glow-warning cursor-pointer"
                            (click)="downloadXml(item)" title="{{ 'TAXABLE.DOWNLOAD_XML' | translate }}">
                      <svg class="w-3 h-3 inline mr-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5M16.5 12L12 16.5m0 0L7.5 12m4.5 4.5V3" />
                      </svg>XML
                    </button>
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
          <div class="rounded-lg p-4 overflow-auto max-h-80 code-panel">
            <pre class="text-sm text-slate-300 whitespace-pre-wrap font-mono">{{ parsedMetadata }}</pre>
          </div>
          <div class="mt-4 text-right">
            <button class="btn btn-ghost btn-sm" (click)="metadataItem = null">{{ 'COMMON.CANCEL' | translate }}</button>
          </div>
        </div>
      </div>

      <!-- PDF Preview Modal -->
      <div *ngIf="pdfPreviewUrl" class="modal modal-open">
        <div class="modal-overlay" (click)="closePdfPreview()"></div>
        <div class="modal-content w-full max-w-6xl p-6" style="height: 92vh;">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-lg font-semibold text-white">{{ 'TAXABLE.PREVIEW_PDF' | translate }}</h3>
            <div class="flex gap-2">
              <button class="btn btn-ghost btn-sm" (click)="downloadCurrentPdf()" title="{{ 'TAXABLE.DOWNLOAD_PDF' | translate }}">
                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5M16.5 12L12 16.5m0 0L7.5 12m4.5 4.5V3" />
                </svg>
              </button>
              <button class="btn btn-ghost btn-sm" (click)="closePdfPreview()">
                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
          </div>
          <iframe [src]="pdfPreviewUrl" class="w-full rounded-lg border border-white/10" style="height: calc(100% - 60px);"></iframe>
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
  pdfPreviewUrl: SafeResourceUrl | null = null;
  pdfPreviewItem: TaxableExpense | null = null;
  categoryKeys = CATEGORY_KEYS;
  startDate: string = '';
  endDate: string = '';

  summaryMetrics = {
    total: 0,
    averageTicket: 0,
    topVendor: '',
    topVendorAmount: 0,
    invoicedCount: 0
  };

  constructor(
    private service: TaxableExpenseService,
    private creditCardService: CreditCardService,
    private expenseService: ExpenseService,
    private sanitizer: DomSanitizer
  ) {
    const now = new Date();
    this.startDate = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().split('T')[0];
    this.endDate = new Date(now.getFullYear(), now.getMonth() + 1, 0).toISOString().split('T')[0];
  }

  ngOnInit() { this.loadAll(); }

  loadAll() {
    this.loading = true;
    this.creditCardService.getAll().subscribe(cards => {
      this.creditCards = cards;
      this.expenseService.getAll().subscribe(expenses => {
        this.cardPayments = expenses.filter(e => e.category === 0);
        this.service.getAll(this.startDate, this.endDate).subscribe({
          next: (data) => {
            this.items = data;
            this.calculateMetrics();
            this.loading = false;
          },
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

  previewPdf(item: TaxableExpense) {
    this.pdfPreviewItem = item;
    this.service.getFileBlob(item.id, 'pdf').subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        this.pdfPreviewUrl = this.sanitizer.bypassSecurityTrustResourceUrl(url);
      }
    });
  }

  closePdfPreview() {
    if (this.pdfPreviewUrl) {
      const url = this.pdfPreviewUrl as any;
      if (typeof url === 'string') URL.revokeObjectURL(url);
    }
    this.pdfPreviewUrl = null;
    this.pdfPreviewItem = null;
  }

  downloadCurrentPdf() {
    if (!this.pdfPreviewItem) return;
    this.downloadFile(this.pdfPreviewItem, 'pdf');
  }

  downloadXml(item: TaxableExpense) {
    this.downloadFile(item, 'xml');
  }

  private downloadFile(item: TaxableExpense, type: 'pdf' | 'xml') {
    this.service.getFileBlob(item.id, type).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${item.vendor}_${item.date}.${type}`;
        a.click();
        URL.revokeObjectURL(url);
      }
    });
  }

  calculateMetrics() {
    this.summaryMetrics.total = this.items.reduce((acc, curr) => acc + curr.amountMXN, 0);
    this.summaryMetrics.averageTicket = this.items.length > 0 ? this.summaryMetrics.total / this.items.length : 0;
    this.summaryMetrics.invoicedCount = this.items.filter(i => i.invoicePdfUrl || i.invoiceXmlUrl).length;

    const vendorTotals: { [key: string]: number } = {};
    for (const item of this.items) {
      if (item.vendor) {
        vendorTotals[item.vendor] = (vendorTotals[item.vendor] || 0) + item.amountMXN;
      }
    }

    let maxVendor = '';
    let maxAmount = 0;
    for (const v in vendorTotals) {
      if (vendorTotals[v] > maxAmount) {
        maxAmount = vendorTotals[v];
        maxVendor = v;
      }
    }

    this.summaryMetrics.topVendor = maxVendor;
    this.summaryMetrics.topVendorAmount = maxAmount;
  }
}
