import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import { MainLayoutComponent } from './main-layout/main-layout.component';
import { SidebarComponent } from './sidebar/sidebar.component';
import { TopbarComponent } from './topbar/topbar.component';
import { InviteUserFormComponent } from './sidebar/invite-user-form.component';

@NgModule({
    declarations: [
        MainLayoutComponent,
        SidebarComponent,
        TopbarComponent,
        InviteUserFormComponent
    ],
    imports: [
        CommonModule,
        RouterModule,
        FormsModule,
        TranslateModule
    ],
    exports: [
        MainLayoutComponent
    ]
})
export class LayoutsModule { }
