import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-summary-card',
  standalone: false,
  template: `
    <div class="p-5 rounded-2xl border border-white/[0.08] relative overflow-hidden transition-all duration-300 hover:-translate-y-0.5 hover:shadow-xl group"
         [ngStyle]="{'background': 'linear-gradient(145deg, rgba(255, 255, 255, 0.03) 0%, rgba(255, 255, 255, 0.01) 100%)'}">
      <div class="flex items-center justify-between mb-3">
        <span class="text-xs font-semibold uppercase tracking-wider text-slate-500">{{ title }}</span>
        <div class="w-8 h-8 rounded-lg flex items-center justify-center" [ngClass]="iconBgClass">
          <ng-content select="[icon]"></ng-content>
        </div>
      </div>
      <p class="text-2xl font-bold text-white">{{ value }}</p>
      <div *ngIf="trend" class="flex items-center mt-2 text-xs">
         <span [class]="trend > 0 ? 'text-emerald-400' : 'text-red-400'" class="font-medium">
           {{ trend > 0 ? '+' : ''}}{{ trend }}%
         </span>
         <span class="text-slate-500 ml-1">vs last month</span>
      </div>
      <div *ngIf="subtext" class="mt-2 text-xs text-slate-500">
        {{ subtext }}
      </div>
    </div>
  `,
  styles: [] // Removed @apply styles to prevent build errors
})
export class SummaryCardComponent {
  @Input() title: string = '';
  @Input() value: string | number = '';
  @Input() color: 'success' | 'danger' | 'info' | 'warning' | 'primary' = 'primary';
  @Input() trend: number | null = null;
  @Input() subtext: string | null = null;

  get iconBgClass() {
    switch (this.color) {
      case 'success': return 'bg-emerald-500/10 text-emerald-400';
      case 'danger': return 'bg-red-500/10 text-red-400';
      case 'info': return 'bg-blue-500/10 text-blue-400';
      case 'warning': return 'bg-amber-500/10 text-amber-400';
      case 'primary': return 'bg-cyan-500/10 text-cyan-400';
      default: return 'bg-cyan-500/10 text-cyan-400';
    }
  }
}
