import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: false,
  template: `
    <div class="min-h-screen flex items-center justify-center p-4 app-shell">
      <!-- Playful background blobs -->
      <div class="fixed inset-0 overflow-hidden pointer-events-none">
        <div class="app-blob w-[32rem] h-[32rem] -top-32 -right-32 opacity-[0.22] animate-blob" style="background: radial-gradient(circle, #38bdf8, transparent 70%);"></div>
        <div class="app-blob w-[28rem] h-[28rem] -bottom-32 -left-24 opacity-[0.18] animate-blob" style="background: radial-gradient(circle, #1d4ed8, transparent 70%); animation-delay: -7s;"></div>
        <div class="app-blob w-[22rem] h-[22rem] top-1/3 left-1/4 opacity-[0.12] animate-blob" style="background: radial-gradient(circle, #2dd4bf, transparent 70%); animation-delay: -13s;"></div>
      </div>

      <div class="w-full max-w-sm relative z-10">
        <div class="text-center mb-8">
          <div class="w-16 h-16 rounded-[1.4rem] gradient-primary flex items-center justify-center mx-auto mb-4 shadow-xl shadow-blue-500/40 animate-float">
            <svg class="w-8 h-8" fill="none" viewBox="0 0 24 24">
              <circle cx="11" cy="12" r="7.3" stroke="white" stroke-width="2" />
              <path d="M11 8.4v7.2M9.2 10c0-.9.8-1.5 1.8-1.5s1.8.5 1.8 1.4-.8 1.3-1.8 1.4-1.8.5-1.8 1.4.8 1.4 1.8 1.4 1.8-.6 1.8-1.5"
                    stroke="white" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" />
              <path d="M19 3.6l.62 1.68L21.3 5.9l-1.68.62L19 8.2l-.62-1.68L16.7 5.9l1.68-.62z" fill="#bae6fd" />
            </svg>
          </div>
          <h1 class="text-3xl font-extrabold text-white tracking-tight">Cuintable</h1>
          <p class="text-[11px] font-bold uppercase tracking-[0.25em] text-gradient-primary mt-1.5">MiGestor Fiscal · RESICO</p>
        </div>

        <div class="glass-card p-7">
          <h2 class="text-xl font-bold text-white mb-1">{{ 'AUTH.REGISTER' | translate }} ✨</h2>
          <p class="text-sm text-slate-400 mb-5">{{ 'AUTH.WELCOME_NEW' | translate }}</p>

          <div *ngIf="error" class="mb-4 p-3 rounded-xl text-sm badge-glow-danger">
            {{ error }}
          </div>

          <div class="space-y-4">
            <div>
              <label class="text-xs font-semibold text-slate-400 mb-1.5 block">{{ 'AUTH.FULL_NAME' | translate }}</label>
              <input type="text" class="input input-bordered w-full" [(ngModel)]="fullName" />
            </div>

            <div>
              <label class="text-xs font-semibold text-slate-400 mb-1.5 block">{{ 'AUTH.EMAIL' | translate }}</label>
              <input type="email" class="input input-bordered w-full" [(ngModel)]="email" />
            </div>

            <div>
              <label class="text-xs font-semibold text-slate-400 mb-1.5 block">{{ 'AUTH.PASSWORD' | translate }}</label>
              <input type="password" class="input input-bordered w-full" [(ngModel)]="password" />
            </div>

            <button class="btn btn-primary w-full mt-2 rounded-full" [disabled]="loading" (click)="register()">
              <div *ngIf="loading" class="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin mr-2"></div>
              {{ 'AUTH.SIGN_UP' | translate }}
            </button>
          </div>

          <p class="text-center mt-5 text-sm text-slate-500">
            {{ 'AUTH.HAS_ACCOUNT' | translate }}
            <a class="text-blue-300 hover:text-blue-200 font-semibold transition-colors" routerLink="/login">
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
