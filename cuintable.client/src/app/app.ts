import { Component, OnInit } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  standalone: false,
  styleUrl: './app.css'
})
export class App implements OnInit {
  title = 'MiGestor Fiscal';

  constructor(
    private translate: TranslateService
  ) {
    this.translate.addLangs(['en', 'es']);
    this.translate.setDefaultLang('en');
  }

  ngOnInit() {
    // Initial language/theme setup can still happen here or be moved to a service
    const savedLang = localStorage.getItem('lang') || 'en';
    this.translate.use(savedLang);

    const savedTheme = localStorage.getItem('theme');
    if (savedTheme === 'dark' || (!savedTheme && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
      document.documentElement.setAttribute('data-theme', 'dark');
    }
  }
}
