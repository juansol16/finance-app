import { HTTP_INTERCEPTORS, HttpClient, HttpClientModule } from '@angular/common/http';
import { NgModule, provideBrowserGlobalErrorListeners } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { ToastrModule } from 'ngx-toastr';
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';

import { AppRoutingModule } from './app-routing-module';
import { App } from './app';
import { JwtInterceptor } from './core/interceptors/jwt.interceptor';
import { LoginComponent } from './features/auth/login.component';
import { RegisterComponent } from './features/auth/register.component';
import { IncomeListComponent } from './features/incomes/income-list.component';
import { IncomeFormComponent } from './features/incomes/income-form.component';
import { CreditCardListComponent } from './features/credit-cards/credit-card-list.component';
import { ExpenseListComponent } from './features/expenses/expense-list.component';
import { ExpenseFormComponent } from './features/expenses/expense-form.component';
import { TaxableExpenseListComponent } from './features/taxable-expenses/taxable-expense-list.component';

import { TaxableExpenseFormComponent } from './features/taxable-expenses/taxable-expense-form.component';
import { DashboardModule } from './features/dashboard/dashboard.module';
import { TaxPaymentsModule } from './features/tax-payments/tax-payments.module';
import { LayoutsModule } from './layouts/layouts.module';


@NgModule({
  declarations: [
    App,
    LoginComponent,
    RegisterComponent,
    IncomeListComponent,
    IncomeFormComponent,
    CreditCardListComponent,
    ExpenseListComponent,
    ExpenseFormComponent,
    TaxableExpenseListComponent,
    TaxableExpenseFormComponent
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
    FormsModule,
    AppRoutingModule,
    DashboardModule,
    TaxPaymentsModule,
    LayoutsModule,
    BrowserAnimationsModule,
    ToastrModule.forRoot(),
    TranslateModule.forRoot({
      defaultLanguage: 'en',
      loader: {
        provide: TranslateLoader,
        useClass: TranslateHttpLoader,
        deps: [HttpClient]
      }
    })
  ],
  providers: [
    provideBrowserGlobalErrorListeners(),
    { provide: HTTP_INTERCEPTORS, useClass: JwtInterceptor, multi: true }
  ],
  bootstrap: [App]
})
export class AppModule { }
