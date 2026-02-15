import { Component, EventEmitter, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { TaxPaymentService } from '../../../core/services/tax-payment.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
    selector: 'app-tax-payment-form',
    templateUrl: './tax-payment-form.component.html',
    standalone: false
})
export class TaxPaymentFormComponent {
    @Output() close = new EventEmitter<void>();
    paymentForm: FormGroup;
    loading = false;
    months = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
    years: number[] = [];

    constructor(
        private fb: FormBuilder,
        private taxPaymentService: TaxPaymentService,
        private notification: NotificationService,
        private translate: TranslateService
    ) {
        const currentYear = new Date().getFullYear();
        for (let i = currentYear - 1; i <= currentYear + 1; i++) {
            this.years.push(i);
        }

        this.paymentForm = this.fb.group({
            periodMonth: [new Date().getMonth() + 1, Validators.required],
            periodYear: [currentYear, Validators.required],
            amountDue: [null, [Validators.required, Validators.min(0.01)]],
            dueDate: [new Date().toISOString().substring(0, 10), Validators.required]
        });
    }

    getMonthKey(m: number): string {
        return this.translate.instant(`DASHBOARD.MONTH_${m}`);
    }

    onSubmit(): void {
        if (this.paymentForm.valid) {
            this.loading = true;
            this.taxPaymentService.create(this.paymentForm.value).subscribe({
                next: () => {
                    this.notification.success(this.translate.instant('COMMON.SAVED_SUCCESS'));
                    this.loading = false;
                    this.close.emit();
                },
                error: () => {
                    this.notification.error(this.translate.instant('COMMON.OPERATION_ERROR'));
                    this.loading = false;
                }
            });
        }
    }

    onCancel(): void {
        this.close.emit();
    }
}
