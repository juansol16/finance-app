import { Component, EventEmitter, Output } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-invite-user-form',
  standalone: false,
  template: `
    <div class="modal modal-open">
      <div class="modal-overlay" (click)="closed.emit()"></div>
      <div class="modal-content w-full max-w-lg">
        <h3 class="text-lg font-bold mb-4">{{ 'INVITE.TITLE' | translate }}</h3>

        <!-- Full Name -->
        <div class="form-group mb-3">
          <label class="form-label">{{ 'INVITE.FULL_NAME' | translate }}</label>
          <input type="text" class="input input-bordered w-full" [(ngModel)]="form.fullName" />
        </div>

        <!-- Email -->
        <div class="form-group mb-3">
          <label class="form-label">{{ 'INVITE.EMAIL' | translate }}</label>
          <input type="email" class="input input-bordered w-full" [(ngModel)]="form.email" />
        </div>

        <!-- Password -->
        <div class="form-group mb-3">
          <label class="form-label">{{ 'INVITE.PASSWORD' | translate }}</label>
          <input type="password" class="input input-bordered w-full" [(ngModel)]="form.password" />
        </div>

        <!-- Role -->
        <div class="form-group mb-3">
          <label class="form-label">{{ 'INVITE.ROLE' | translate }}</label>
          <select class="select select-bordered w-full" [(ngModel)]="form.role">
            <option value="Contador">{{ 'INVITE.ROLE_CONTADOR' | translate }}</option>
            <option value="Pareja">{{ 'INVITE.ROLE_PAREJA' | translate }}</option>
          </select>
        </div>

        <!-- Language -->
        <div class="form-group mb-3">
          <label class="form-label">{{ 'INVITE.LANGUAGE' | translate }}</label>
          <select class="select select-bordered w-full" [(ngModel)]="form.preferredLanguage">
            <option value="es">Español</option>
            <option value="en">English</option>
          </select>
        </div>

        <!-- Actions -->
        <div class="modal-action">
          <button class="btn btn-ghost" (click)="closed.emit()">{{ 'COMMON.CANCEL' | translate }}</button>
          <button class="btn btn-primary" [disabled]="saving" (click)="submit()">
            <span *ngIf="saving" class="spinner-dot-pulse spinner-sm"><span></span></span>
            {{ 'INVITE.SEND' | translate }}
          </button>
        </div>
      </div>
    </div>
  `
})
export class InviteUserFormComponent {
  @Output() closed = new EventEmitter<void>();

  form = {
    fullName: '',
    email: '',
    password: '',
    role: 'Contador',
    preferredLanguage: 'es'
  };

  saving = false;

  constructor(
    private authService: AuthService,
    private notify: NotificationService,
    private translate: TranslateService
  ) {}

  submit() {
    this.saving = true;
    this.authService.inviteUser(this.form).subscribe({
      next: () => {
        this.saving = false;
        this.translate.get('INVITE.SUCCESS').subscribe(msg => this.notify.success(msg));
        this.closed.emit();
      },
      error: (err) => {
        this.saving = false;
        const key = err.status === 409 ? 'INVITE.ERROR_EMAIL_EXISTS' : 'COMMON.OPERATION_ERROR';
        this.translate.get(key).subscribe(msg => this.notify.error(msg));
      }
    });
  }
}
