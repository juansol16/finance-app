import { Component, OnInit } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../core/services/auth.service';

@Component({
    selector: 'app-topbar',
    templateUrl: './topbar.component.html',
    standalone: false
})
export class TopbarComponent implements OnInit {
    isDarkMode = false;
    currentLang = 'en';

    constructor(
        public authService: AuthService,
        private translate: TranslateService
    ) { }

    ngOnInit() {
        this.currentLang = localStorage.getItem('lang') || 'en';
        const savedTheme = localStorage.getItem('theme');

        if (savedTheme === 'dark' || (!savedTheme && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
            this.isDarkMode = true;
            document.documentElement.setAttribute('data-theme', 'dark');
        }
    }

    toggleLanguage() {
        this.currentLang = this.currentLang === 'en' ? 'es' : 'en';
        this.translate.use(this.currentLang);
        localStorage.setItem('lang', this.currentLang);
    }

    toggleDarkMode() {
        this.isDarkMode = !this.isDarkMode;
        const theme = this.isDarkMode ? 'dark' : 'light';
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem('theme', theme);
    }
}
