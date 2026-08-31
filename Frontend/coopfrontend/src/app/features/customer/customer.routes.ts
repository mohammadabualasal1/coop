import { Component } from '@angular/core';
import { Routes } from '@angular/router';

@Component({
  selector: 'app-customer-placeholder',
  template: `<p>قيد الإنشاء</p>`
})
class CustomerPlaceholderComponent {}

export const CUSTOMER_ROUTES: Routes = [{ path: '', component: CustomerPlaceholderComponent }];
