import { NgModule } from '@angular/core';
import { HttpClientModule } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { TransactionImportComponent } from './transaction-import/transaction-import.component';
import { StoresModule } from '../stores/stores.module';

@NgModule({
  declarations: [
    TransactionImportComponent
  ],
  imports: [
    CommonModule,
    HttpClientModule,
    StoresModule
  ],
  exports: [
    TransactionImportComponent
  ]
})
export class TransactionsModule { }
