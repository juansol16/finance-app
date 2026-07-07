import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { BaseChartDirective, provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { TranslateModule } from '@ngx-translate/core';

import { AdvisorDashboardComponent } from './advisor-dashboard.component';
import { StatementDetailComponent } from './statement-detail.component';
import { StatementUploadComponent } from './statement-upload.component';

@NgModule({
  declarations: [AdvisorDashboardComponent, StatementDetailComponent, StatementUploadComponent],
  imports: [CommonModule, FormsModule, RouterModule, BaseChartDirective, TranslateModule],
  providers: [provideCharts(withDefaultRegisterables())],
})
export class FinancialAdvisorModule {}
