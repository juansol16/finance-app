import { Component, EventEmitter, Input, Output } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { TaxPaymentResponse, TaxPaymentService } from '../../../core/services/tax-payment.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
    selector: 'app-mark-as-paid',
    templateUrl: './mark-as-paid.component.html',
    standalone: false
})
export class MarkAsPaidComponent {
    @Input() payment: TaxPaymentResponse | null = null;
    @Output() close = new EventEmitter<void>();

    paymentDate: string = new Date().toISOString().substring(0, 10);
    receiptFile: File | null = null;
    loading = false;

    constructor(
        private taxPaymentService: TaxPaymentService,
        private notification: NotificationService,
        private translate: TranslateService
    ) { }

    onFileSelected(event: any): void {
        this.receiptFile = event.target.files[0];
    }

    onSubmit(): void {
        if (this.payment && this.paymentDate) {
            this.loading = true;
            this.taxPaymentService.markAsPaid(this.payment.id, this.paymentDate, this.receiptFile || undefined).subscribe({
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
