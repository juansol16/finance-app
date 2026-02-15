import { Component, OnInit } from '@angular/core';
import { CreditCard, CreditCardService } from '../../core/services/credit-card.service';

@Component({
  selector: 'app-credit-card-list',
  standalone: false,
  template: `
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-2xl font-bold">{{ 'NAV.CREDIT_CARDS' | translate }}</h1>
      <button class="btn btn-primary" (click)="showForm = true; editing = null">
        + {{ 'COMMON.ADD' | translate }}
      </button>
    </div>

    <div *ngIf="loading" class="flex justify-center py-12">
      <span class="spinner-dot-pulse"><span></span></span>
    </div>

    <div *ngIf="!loading && cards.length === 0" class="text-center py-12 text-base-content/60">
      <p class="text-lg">{{ 'COMMON.NO_DATA' | translate }}</p>
    </div>

    <!-- Card Grid -->
    <div *ngIf="!loading && cards.length > 0" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <div *ngFor="let card of cards" class="card bg-base-200 shadow">
        <div class="card-body">
          <div class="flex justify-between items-start">
            <div>
              <h3 class="card-title text-lg">{{ card.nickname }}</h3>
              <p class="text-sm text-base-content/60">{{ card.bank }}</p>
            </div>
            <span class="badge" [class.badge-success]="card.isActive" [class.badge-ghost]="!card.isActive">
              {{ card.isActive ? ('CREDIT_CARD.ACTIVE' | translate) : ('CREDIT_CARD.INACTIVE' | translate) }}
            </span>
          </div>
          <p class="font-mono text-xl mt-2">**** **** **** {{ card.lastFourDigits }}</p>
          <div class="card-actions justify-end mt-2">
            <button class="btn btn-ghost btn-sm" (click)="edit(card)">{{ 'COMMON.EDIT' | translate }}</button>
            <button class="btn btn-ghost btn-sm text-error" (click)="confirmDelete(card)">{{ 'COMMON.DELETE' | translate }}</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Form Modal -->
    <div *ngIf="showForm" class="modal modal-open">
      <div class="modal-overlay" (click)="showForm = false"></div>
      <div class="modal-content w-full max-w-md">
        <h3 class="text-lg font-bold mb-4">
          {{ editing ? ('COMMON.EDIT' | translate) : ('COMMON.ADD' | translate) }} {{ 'NAV.CREDIT_CARDS' | translate }}
        </h3>

        <div class="form-group mb-3">
          <label class="form-label">{{ 'CREDIT_CARD.BANK' | translate }}</label>
          <input type="text" class="input input-bordered w-full" [(ngModel)]="form.bank" />
        </div>

        <div class="form-group mb-3">
          <label class="form-label">{{ 'CREDIT_CARD.NICKNAME' | translate }}</label>
          <input type="text" class="input input-bordered w-full" [(ngModel)]="form.nickname" />
        </div>

        <div class="form-group mb-3">
          <label class="form-label">{{ 'CREDIT_CARD.LAST_FOUR' | translate }}</label>
          <input type="text" class="input input-bordered w-full" [(ngModel)]="form.lastFourDigits"
                 maxlength="4" placeholder="1234" />
        </div>

        <div *ngIf="editing" class="form-group mb-3">
          <label class="flex items-center gap-2 cursor-pointer">
            <input type="checkbox" class="toggle toggle-primary" [(ngModel)]="form.isActive" />
            <span>{{ 'CREDIT_CARD.ACTIVE' | translate }}</span>
          </label>
        </div>

        <div class="modal-action">
          <button class="btn btn-ghost" (click)="showForm = false">{{ 'COMMON.CANCEL' | translate }}</button>
          <button class="btn btn-primary" [disabled]="saving" (click)="save()">
            {{ 'COMMON.SAVE' | translate }}
          </button>
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
          <button class="btn btn-error" (click)="deleteCard()">{{ 'COMMON.DELETE' | translate }}</button>
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
