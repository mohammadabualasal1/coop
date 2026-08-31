import { Component } from '@angular/core';
import { Routes } from '@angular/router';

@Component({
  selector: 'app-merchant-placeholder',
  template: `<p>قيد الإنشاء</p>`
})
class MerchantPlaceholderComponent {}

export const MERCHANT_ROUTES: Routes = [{ path: '', component: MerchantPlaceholderComponent }];
