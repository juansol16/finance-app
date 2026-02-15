import { Component } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { ChangeDetectorRef } from '@angular/core';
import { filter } from 'rxjs/operators';

@Component({
    selector: 'app-main-layout',
    templateUrl: './main-layout.component.html',
    standalone: false
})
export class MainLayoutComponent {
    sidebarOpen = false;

    constructor(private router: Router, private cdr: ChangeDetectorRef) {
        this.router.events.pipe(
            filter(event => event instanceof NavigationEnd)
        ).subscribe(() => {
            this.sidebarOpen = false;
            this.cdr.detectChanges();
        });
    }

    toggleSidebar() {
        this.sidebarOpen = !this.sidebarOpen;
    }

    closeSidebar() {
        this.sidebarOpen = false;
    }
}
