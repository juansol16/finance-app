import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: false,
  template: `
    <div class="min-h-screen flex items-center justify-center p-4" style="background: #0b0f19;">
      <div class="fixed inset-0 overflow-hidden pointer-events-none">
        <div class="absolute top-1/4 -left-32 w-96 h-96 rounded-full opacity-[0.03]" style="background: radial-gradient(circle, #6366f1, transparent 70%);"></div>
        <div class="absolute bottom-1/4 -right-32 w-96 h-96 rounded-full opacity-[0.03]" style="background: radial-gradient(circle, #06b6d4, transparent 70%);"></div>
      </div>

      <div class="w-full max-w-sm relative z-10">
        <div class="text-center mb-8">
          <div class="w-12 h-12 rounded-xl gradient-primary flex items-center justify-center mx-auto mb-4">
            <svg class="w-6 h-6 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6" />
            </svg>
          </div>
          <h1 class="text-2xl font-bold text-white">MiGestor Fiscal</h1>
          <p class="text-sm text-slate-500 mt-1">RESICO Management Platform</p>
        </div>

        <div class="glass-card p-6">
          <h2 class="text-lg font-semibold text-white mb-5">{{ 'AUTH.REGISTER' | translate }}</h2>

          <div *ngIf="error" class="mb-4 p-3 rounded-lg text-sm badge-glow-danger">
            {{ error }}
          </div>

          <div class="space-y-4">
            <div>
              <label class="text-xs font-medium text-slate-400 mb-1.5 block">{{ 'AUTH.FULL_NAME' | translate }}</label>
              <input type="text" class="input input-bordered w-full" [(ngModel)]="fullName" />
            </div>

            <div>
              <label class="text-xs font-medium text-slate-400 mb-1.5 block">{{ 'AUTH.EMAIL' | translate }}</label>
              <input type="email" class="input input-bordered w-full" [(ngModel)]="email" />
            </div>

            <div>
              <label class="text-xs font-medium text-slate-400 mb-1.5 block">{{ 'AUTH.PASSWORD' | translate }}</label>
              <input type="password" class="input input-bordered w-full" [(ngModel)]="password" />
            </div>

            <button class="btn btn-primary w-full mt-2" [disabled]="loading" (click)="register()">
              <div *ngIf="loading" class="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin mr-2"></div>
              {{ 'AUTH.SIGN_UP' | translate }}
            </button>
          </div>

          <p class="text-center mt-5 text-sm text-slate-500">
            {{ 'AUTH.HAS_ACCOUNT' | translate }}
            <a class="text-cyan-400 hover:text-cyan-300 font-medium transition-colors" routerLink="/login">
              {{ 'AUTH.LOGIN' | translate }}
            </a>
          </p>
        </div>
      </div>
    </div>
  `
})
export class RegisterComponent {
  fullName = '';
  email = '';
  password = '';
  error = '';
  loading = false;

  constructor(private authService: AuthService, private router: Router) {}

  register() {
    this.loading = true;
    this.error = '';
    this.authService.register({
      email: this.email,
      password: this.password,
      fullName: this.fullName,
      preferredLanguage: 'es'
    }).subscribe({
      next: () => {
        this.loading = false;
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.loading = false;
        this.error = err.error?.message || 'Registration failed';
      }
    });
  }
}
