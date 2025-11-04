import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { TransactionImportComponent } from './transactions/transaction-import/transaction-import.component';

const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'transactions/import' },
  { path: 'transactions/import', component: TransactionImportComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
