import { Component } from '@angular/core';
import { Routes } from '@angular/router';

@Component({
  selector: 'app-admin-placeholder',
  template: `<p>قيد الإنشاء</p>`
})
class AdminPlaceholderComponent {}

export const ADMIN_ROUTES: Routes = [{ path: '', component: AdminPlaceholderComponent }];
