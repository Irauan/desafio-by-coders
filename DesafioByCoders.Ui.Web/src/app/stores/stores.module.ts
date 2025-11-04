import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StoreListComponent } from './store-list/store-list.component';

@NgModule({
  declarations: [
    StoreListComponent
  ],
  imports: [
    CommonModule
  ],
  exports: [
    StoreListComponent
  ]
})
export class StoresModule { }
