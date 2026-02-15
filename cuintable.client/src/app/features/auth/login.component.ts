import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: false,
  template: `
    <div class="flex min-h-[80vh] items-center justify-center">
      <div class="card w-full max-w-md bg-base-200 shadow-xl">
        <div class="card-body">
          <h2 class="card-title text-2xl justify-center mb-4">{{ 'AUTH.LOGIN' | translate }}</h2>

          <div *ngIf="error" class="alert alert-error mb-4">
            <span>{{ error }}</span>
          </div>

          <div class="form-group">
            <label class="form-label">{{ 'AUTH.EMAIL' | translate }}</label>
            <input type="email" class="input input-bordered w-full" [(ngModel)]="email" />
          </div>

          <div class="form-group mt-3">
            <label class="form-label">{{ 'AUTH.PASSWORD' | translate }}</label>
            <input type="password" class="input input-bordered w-full" [(ngModel)]="password"
                   (keyup.enter)="login()" />
          </div>

          <button class="btn btn-primary w-full mt-6" [disabled]="loading" (click)="login()">
            <span *ngIf="loading" class="spinner-dot-pulse spinner-sm"><span></span></span>
            {{ 'AUTH.SIGN_IN' | translate }}
          </button>

          <p class="text-center mt-4 text-sm">
            {{ 'AUTH.NO_ACCOUNT' | translate }}
            <a class="link link-primary" routerLink="/register">{{ 'AUTH.REGISTER' | translate }}</a>
          </p>
        </div>
      </div>
    </div>
  `
})
export class LoginComponent {
  email = '';
  password = '';
  error = '';
  loading = false;

  constructor(private authService: AuthService, private router: Router) {}

  login() {
    this.loading = true;
    this.error = '';
    this.authService.login({ email: this.email, password: this.password }).subscribe({
      next: () => {
        this.loading = false;
        this.router.navigate(['/incomes']);
      },
      error: () => {
        this.loading = false;
        this.error = 'Invalid email or password';
      }
    });
  }
}
