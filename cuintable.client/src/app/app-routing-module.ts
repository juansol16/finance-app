import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';
import { RoleGuard } from './core/guards/role.guard';
import { LoginComponent } from './features/auth/login.component';
import { RegisterComponent } from './features/auth/register.component';
import { IncomeListComponent } from './features/incomes/income-list.component';
import { CreditCardListComponent } from './features/credit-cards/credit-card-list.component';
import { ExpenseListComponent } from './features/expenses/expense-list.component';
import { TaxableExpenseListComponent } from './features/taxable-expenses/taxable-expense-list.component';
import { TaxPaymentListComponent } from './features/tax-payments/tax-payment-list/tax-payment-list.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { MainLayoutComponent } from './layouts/main-layout/main-layout.component';

const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [AuthGuard],
    children: [
      { path: 'dashboard', component: DashboardComponent, canActivate: [RoleGuard], data: { roles: ['Owner', 'Pareja'] } },
      { path: 'incomes', component: IncomeListComponent, canActivate: [RoleGuard], data: { roles: ['Owner'] } },
      { path: 'credit-cards', component: CreditCardListComponent, canActivate: [RoleGuard], data: { roles: ['Owner', 'Pareja'] } },
      { path: 'expenses', component: ExpenseListComponent, canActivate: [RoleGuard], data: { roles: ['Owner', 'Pareja'] } },
      { path: 'taxable-expenses', component: TaxableExpenseListComponent, canActivate: [RoleGuard], data: { roles: ['Owner', 'Contador', 'Pareja'] } },
      { path: 'tax-payments', component: TaxPaymentListComponent, canActivate: [RoleGuard], data: { roles: ['Owner', 'Contador', 'Pareja'] } },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  },
  { path: '**', redirectTo: '/dashboard' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
