import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SummaryCardComponent } from './components/summary-card/summary-card.component';

@NgModule({
    declarations: [
        SummaryCardComponent
    ],
    imports: [
        CommonModule
    ],
    exports: [
        SummaryCardComponent,
        CommonModule
    ]
})
export class SharedModule { }
