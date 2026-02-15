import { Component, OnInit } from '@angular/core';
import { CreditCard, CreditCardService } from '../../core/services/credit-card.service';

@Component({
  selector: 'app-credit-card-list',
  standalone: false,
  template: `
    <div class="max-w-7xl mx-auto">
      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-white">{{ 'NAV.CREDIT_CARDS' | translate }}</h1>
          <p class="text-sm text-slate-500 mt-1">{{ cards.length }} {{ 'COMMON.RECORDS' | translate }}</p>
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

      <div *ngIf="!loading && cards.length === 0" class="glass-card p-12 text-center">
        <svg class="w-12 h-12 mx-auto text-slate-600 mb-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1">
          <path stroke-linecap="round" stroke-linejoin="round" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
        </svg>
        <p class="text-slate-500">{{ 'COMMON.NO_DATA' | translate }}</p>
      </div>

      <!-- Card Grid -->
      <div *ngIf="!loading && cards.length > 0" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        <div *ngFor="let card of cards" class="glass-card p-5 hover:border-white/[0.12] transition-all">
          <div class="flex justify-between items-start mb-4">
            <div>
              <h3 class="text-base font-semibold text-white">{{ card.nickname }}</h3>
              <p class="text-xs text-slate-500 mt-0.5">{{ card.bank }}</p>
            </div>
            <span class="text-xs font-medium px-2 py-1 rounded-md"
                  [class]="card.isActive ? 'badge-glow-success' : 'badge-glow-danger'">
              {{ card.isActive ? ('CREDIT_CARD.ACTIVE' | translate) : ('CREDIT_CARD.INACTIVE' | translate) }}
            </span>
          </div>

          <p class="font-mono text-xl text-slate-300 tracking-wider mb-4">
            <span class="text-slate-600">****  ****  ****</span>  {{ card.lastFourDigits }}
          </p>

          <div class="flex justify-end gap-1 border-t border-white/[0.06] pt-3">
            <button class="btn btn-ghost btn-xs text-slate-400 hover:text-cyan-400" (click)="edit(card)">
              {{ 'COMMON.EDIT' | translate }}
            </button>
            <button class="btn btn-ghost btn-xs text-slate-400 hover:text-red-400" (click)="confirmDelete(card)">
              {{ 'COMMON.DELETE' | translate }}
            </button>
          </div>
        </div>
      </div>

      <!-- Form Modal -->
      <div *ngIf="showForm" class="modal modal-open">
        <div class="modal-overlay" (click)="showForm = false"></div>
        <div class="modal-content w-full max-w-md p-6">
          <h3 class="text-lg font-semibold text-white mb-5">
            {{ editing ? ('COMMON.EDIT' | translate) : ('COMMON.ADD' | translate) }} {{ 'NAV.CREDIT_CARDS' | translate }}
          </h3>

          <div class="space-y-4">
            <div>
              <label class="text-xs font-medium text-slate-400 mb-1 block">{{ 'CREDIT_CARD.BANK' | translate }}</label>
              <input type="text" class="input input-bordered w-full" [(ngModel)]="form.bank" />
            </div>
            <div>
              <label class="text-xs font-medium text-slate-400 mb-1 block">{{ 'CREDIT_CARD.NICKNAME' | translate }}</label>
              <input type="text" class="input input-bordered w-full" [(ngModel)]="form.nickname" />
            </div>
            <div>
              <label class="text-xs font-medium text-slate-400 mb-1 block">{{ 'CREDIT_CARD.LAST_FOUR' | translate }}</label>
              <input type="text" class="input input-bordered w-full" [(ngModel)]="form.lastFourDigits"
                     maxlength="4" placeholder="1234" />
            </div>
            <div *ngIf="editing">
              <label class="flex items-center gap-3 cursor-pointer">
                <input type="checkbox" class="toggle toggle-sm toggle-primary" [(ngModel)]="form.isActive" />
                <span class="text-sm text-slate-300">{{ 'CREDIT_CARD.ACTIVE' | translate }}</span>
              </label>
            </div>
          </div>

          <div class="flex gap-3 mt-6">
            <button class="btn btn-ghost flex-1" (click)="showForm = false">{{ 'COMMON.CANCEL' | translate }}</button>
            <button class="btn btn-primary flex-1" [disabled]="saving" (click)="save()">{{ 'COMMON.SAVE' | translate }}</button>
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
            <button class="btn btn-error flex-1" (click)="deleteCard()">{{ 'COMMON.DELETE' | translate }}</button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class CreditCardListComponent implements OnInit {
  cards: CreditCard[] = [];
  loading = true;
  showForm = false;
  saving = false;
  editing: CreditCard | null = null;
  deleting: CreditCard | null = null;

  form = { bank: '', nickname: '', lastFourDigits: '', isActive: true };

  constructor(private service: CreditCardService) {}

  ngOnInit() { this.load(); }

  load() {
    this.loading = true;
    this.service.getAll().subscribe({
      next: (data) => { this.cards = data; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  edit(card: CreditCard) {
    this.editing = card;
    this.form = {
      bank: card.bank,
      nickname: card.nickname,
      lastFourDigits: card.lastFourDigits,
      isActive: card.isActive
    };
    this.showForm = true;
  }

  save() {
    this.saving = true;
    const save$ = this.editing
      ? this.service.update(this.editing.id, this.form)
      : this.service.create(this.form);

    save$.subscribe({
      next: () => { this.saving = false; this.showForm = false; this.load(); },
      error: () => { this.saving = false; }
    });
  }

  confirmDelete(card: CreditCard) { this.deleting = card; }

  deleteCard() {
    if (!this.deleting) return;
    this.service.delete(this.deleting.id).subscribe({
      next: () => { this.deleting = null; this.load(); }
    });
  }
}
