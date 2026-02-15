import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
    selector: 'app-sidebar',
    templateUrl: './sidebar.component.html',
    standalone: false
})
export class SidebarComponent {

    menuItems = [
        { label: 'NAV.DASHBOARD', icon: 'icon-home', route: '/dashboard' },
        { label: 'NAV.INCOMES', icon: 'icon-money', route: '/incomes' },
        { label: 'NAV.EXPENSES', icon: 'icon-credit-card', route: '/expenses' },
        { label: 'NAV.DEDUCTIBLE_EXPENSES', icon: 'icon-document', route: '/taxable-expenses' },
        { label: 'NAV.TAX_PAYMENTS', icon: 'icon-calendar', route: '/tax-payments' },
        { label: 'NAV.CREDIT_CARDS', icon: 'icon-credit-card', route: '/credit-cards' }
    ];

    constructor(public authService: AuthService, private router: Router) { }

    isActive(route: string): boolean {
        return this.router.url.startsWith(route);
    }
}
