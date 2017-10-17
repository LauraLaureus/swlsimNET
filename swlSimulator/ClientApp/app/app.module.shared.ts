import {CommonModule} from "@angular/common";
import {NgModule} from "@angular/core";
import {HttpModule} from "@angular/http";
import {FormsModule, ReactiveFormsModule} from "@angular/forms";
import {RouterModule} from "@angular/router";
import {AppComponent} from "./components/app/app.component";
import {NavMenuComponent} from "./components/navmenu.component";
import {HomeComponent} from "./components/home.component";
import {ImportComponent} from "./components/import.component";
import {SpellqueryComponent} from "./components/spellquery.component";
import {ResultComponent} from "./components/result.component";
import {Configuration} from "./components/app/app.constants";
import { ChartsModule } from 'ng2-charts';

@NgModule({
    declarations: [
        AppComponent,
        NavMenuComponent,
        ImportComponent,
        ResultComponent,
        SpellqueryComponent,
        HomeComponent
    ],
    imports: [
        CommonModule,
        HttpModule,
        FormsModule,
        ChartsModule,
        ReactiveFormsModule,
        RouterModule.forRoot([
            { path: "", redirectTo: "home", pathMatch: "full" },
            { path: "home", component: HomeComponent },
            { path: "import", component: ImportComponent },
            { path: "result", component: ResultComponent },
            { path: "spellquery", component: SpellqueryComponent },
            { path: "**", redirectTo: "home" }
        ])
    ],
    exports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule
    ],
    providers: [
        Configuration
    ],
})
export class AppModuleShared {
}
