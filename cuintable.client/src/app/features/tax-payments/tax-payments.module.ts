import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import { TaxPaymentListComponent } from './tax-payment-list/tax-payment-list.component';
import { TaxPaymentFormComponent } from './tax-payment-form/tax-payment-form.component';
import { MarkAsPaidComponent } from './mark-as-paid/mark-as-paid.component';
import { SharedModule } from '../../shared/shared.module';

@NgModule({
    declarations: [
        TaxPaymentListComponent,
        TaxPaymentFormComponent,
        MarkAsPaidComponent
    ],
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        TranslateModule,
        SharedModule
    ],
    exports: [
        TaxPaymentListComponent
    ]
})
export class TaxPaymentsModule { }
