import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseChartDirective, provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { TranslateModule } from '@ngx-translate/core';

import { DashboardComponent } from './dashboard.component';

@NgModule({
    declarations: [
        DashboardComponent
    ],
    imports: [
        CommonModule,
        FormsModule,
        BaseChartDirective,
        TranslateModule
    ],
    providers: [
        provideCharts(withDefaultRegisterables())
    ]
})
export class DashboardModule { }
