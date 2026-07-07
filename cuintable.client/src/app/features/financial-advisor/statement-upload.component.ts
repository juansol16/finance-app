import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CreditCard } from '../../core/services/credit-card.service';
import {
  FinancialAdvisorService,
  StatementDetail,
} from '../../core/services/financial-advisor.service';

@Component({
  selector: 'app-statement-upload',
  standalone: false,
  template: `
    <div class="modal modal-open">
      <div class="modal-overlay" (click)="tryClose()"></div>
      <div class="modal-content w-full max-w-md p-6">
        <h3 class="text-lg font-semibold text-white mb-1">
          {{ 'ADVISOR.UPLOAD_TITLE' | translate }}
        </h3>
        <p class="text-xs text-slate-500 mb-5">{{ 'ADVISOR.UPLOAD_DESC' | translate }}</p>

        <!-- Analyzing state -->
        <div *ngIf="uploading" class="py-10 text-center">
          <div
            class="w-10 h-10 mx-auto border-2 border-cyan-500/30 border-t-cyan-500 rounded-full animate-spin mb-4"
          ></div>
          <p class="text-sm text-slate-300 font-medium">{{ 'ADVISOR.ANALYZING' | translate }}</p>
          <p class="text-xs text-slate-500 mt-2">{{ 'ADVISOR.ANALYZING_HINT' | translate }}</p>
        </div>

        <div *ngIf="!uploading" class="space-y-4">
          <!-- Drop zone -->
          <label
            class="block border-2 border-dashed rounded-xl p-8 text-center cursor-pointer transition-colors"
            [class]="
              dragOver
                ? 'border-cyan-400/70 bg-cyan-500/5'
                : 'border-white/[0.12] hover:border-white/[0.25]'
            "
            (dragover)="$event.preventDefault(); dragOver = true"
            (dragleave)="dragOver = false"
            (drop)="onDrop($event)"
          >
            <input
              type="file"
              accept="application/pdf,.pdf"
              class="hidden"
              (change)="onFilePicked($event)"
            />
            <svg
              class="w-8 h-8 mx-auto text-slate-500 mb-2"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              stroke-width="1.5"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5m-13.5-9L12 3m0 0l4.5 4.5M12 3v13.5"
              />
            </svg>
            <p *ngIf="!file" class="text-sm text-slate-400">
              {{ 'ADVISOR.UPLOAD_HINT' | translate }}
            </p>
            <p *ngIf="file" class="text-sm text-cyan-300 font-medium break-all">{{ file.name }}</p>
          </label>

          <!-- Optional card -->
          <div>
            <label class="text-xs font-medium text-slate-400 mb-1 block">{{
              'ADVISOR.UPLOAD_CARD_LABEL' | translate
            }}</label>
            <select class="select select-bordered w-full" [(ngModel)]="creditCardId">
              <option [ngValue]="null">{{ 'ADVISOR.UPLOAD_CARD_AUTO' | translate }}</option>
              <option *ngFor="let card of cards" [ngValue]="card.id">
                {{ card.nickname }} ({{ card.bank }} ****{{ card.lastFourDigits }})
              </option>
            </select>
          </div>

          <p *ngIf="error" class="text-xs text-red-400">{{ error | translate }}</p>

          <div class="flex gap-3 pt-1">
            <button class="btn btn-ghost flex-1" (click)="closed.emit()">
              {{ 'COMMON.CANCEL' | translate }}
            </button>
            <button class="btn btn-primary flex-1" [disabled]="!file" (click)="upload()">
              {{ 'ADVISOR.ANALYZE' | translate }}
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class StatementUploadComponent {
  @Input() cards: CreditCard[] = [];
  @Output() closed = new EventEmitter<void>();
  @Output() uploaded = new EventEmitter<StatementDetail>();

  file: File | null = null;
  creditCardId: string | null = null;
  uploading = false;
  dragOver = false;
  error: string | null = null;

  constructor(private service: FinancialAdvisorService) {}

  tryClose() {
    if (!this.uploading) this.closed.emit();
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    this.dragOver = false;
    const file = event.dataTransfer?.files?.[0];
    if (file) this.setFile(file);
  }

  onFilePicked(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) this.setFile(file);
    input.value = '';
  }

  private setFile(file: File) {
    this.error = null;
    if (!file.name.toLowerCase().endsWith('.pdf') && file.type !== 'application/pdf') {
      this.error = 'ADVISOR.ONLY_PDF';
      this.file = null;
      return;
    }
    this.file = file;
  }

  upload() {
    if (!this.file) return;
    this.uploading = true;
    this.error = null;

    this.service.upload(this.file, this.creditCardId).subscribe({
      next: (detail) => {
        this.uploading = false;
        this.uploaded.emit(detail);
      },
      error: (err) => {
        this.uploading = false;
        this.error = typeof err?.error === 'string' ? err.error : 'ADVISOR.UPLOAD_ERROR';
      },
    });
  }
}
