import { Component, OnInit } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { TaxPaymentService, TaxPaymentResponse, TaxPaymentStatus } from '../../../core/services/tax-payment.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
    selector: 'app-tax-payment-list',
    templateUrl: './tax-payment-list.component.html',
    standalone: false
})
export class TaxPaymentListComponent implements OnInit {
    payments: TaxPaymentResponse[] = [];
    filteredPayments: TaxPaymentResponse[] = [];
    loading = false;
    showCreateModal = false;
    showMarkPaidModal = false;
    paymentToMarkPaid: TaxPaymentResponse | null = null;

    filterYear: number | null = null;
    filterStatus: number | null = null;
    availableYears: number[] = [];

    constructor(
        private taxPaymentService: TaxPaymentService,
        private notification: NotificationService,
        private translate: TranslateService
    ) { }

    ngOnInit(): void {
        this.loadPayments();
    }

    loadPayments(): void {
        this.loading = true;
        this.taxPaymentService.getAll().subscribe({
            next: (data) => {
                this.payments = data;
                this.buildAvailableYears();
                this.applyFilters();
                this.loading = false;
            },
            error: () => {
                this.notification.error(this.translate.instant('COMMON.OPERATION_ERROR'));
                this.loading = false;
            }
        });
    }

    buildAvailableYears(): void {
        const years = new Set(this.payments.map(p => p.periodYear));
        this.availableYears = Array.from(years).sort((a, b) => b - a);
    }

    applyFilters(): void {
        this.filteredPayments = this.payments.filter(p => {
            if (this.filterYear !== null && p.periodYear !== this.filterYear) return false;
            if (this.filterStatus !== null && p.status !== this.filterStatus) return false;
            return true;
        });
    }

    onFilterChange(): void {
        this.applyFilters();
    }

    getStatusBadgeClass(status: TaxPaymentStatus): string {
        return status === TaxPaymentStatus.Pagado ? 'badge badge-success' : 'badge badge-warning';
    }

    getStatusLabel(status: TaxPaymentStatus): string {
        return status === TaxPaymentStatus.Pagado
            ? this.translate.instant('TAX_PAYMENT.STATUS_PAGADO')
            : this.translate.instant('TAX_PAYMENT.STATUS_PENDIENTE');
    }

    getDueDateAlert(payment: TaxPaymentResponse): string | null {
        if (payment.status === TaxPaymentStatus.Pagado) return null;
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        const due = new Date(payment.dueDate);
        const diffDays = Math.ceil((due.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
        if (diffDays < 0) return 'overdue';
        if (diffDays <= 5) return 'due-soon';
        return null;
    }

    openCreateModal(): void {
        this.showCreateModal = true;
    }

    closeCreateModal(): void {
        this.showCreateModal = false;
        this.loadPayments();
    }

    openMarkPaidModal(payment: TaxPaymentResponse): void {
        this.paymentToMarkPaid = payment;
        this.showMarkPaidModal = true;
    }

    closeMarkPaidModal(): void {
        this.showMarkPaidModal = false;
        this.paymentToMarkPaid = null;
        this.loadPayments();
    }

    uploadDetermination(paymentId: string, event: any): void {
        const file = event.target.files[0];
        if (file) {
            this.loading = true;
            this.taxPaymentService.uploadDetermination(paymentId, file).subscribe({
                next: () => {
                    this.notification.success(this.translate.instant('COMMON.UPLOAD_SUCCESS'));
                    this.loadPayments();
                },
                error: () => {
                    this.notification.error(this.translate.instant('COMMON.OPERATION_ERROR'));
                    this.loading = false;
                }
            });
        }
    }

    deletePayment(id: string): void {
        const msg = this.translate.instant('COMMON.CONFIRM_DELETE');
        if (confirm(msg)) {
            this.taxPaymentService.delete(id).subscribe({
                next: () => {
                    this.notification.success(this.translate.instant('COMMON.DELETED_SUCCESS'));
                    this.loadPayments();
                },
                error: () => {
                    this.notification.error(this.translate.instant('COMMON.OPERATION_ERROR'));
                }
            });
        }
    }
}
