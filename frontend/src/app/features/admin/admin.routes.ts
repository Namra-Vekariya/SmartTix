import { Routes } from '@angular/router';
import { Component } from '@angular/core';

@Component({
  selector: 'app-admin-placeholder',
  template: `<p class="pt-16 text-center text-lg opacity-70">
    Admin dashboard — coming soon
  </p>`,
})
class AdminPlaceholderComponent {}

export const ADMIN_ROUTES: Routes = [
  { path: '', component: AdminPlaceholderComponent },
];
