import { Routes } from '@angular/router';
import { Component } from '@angular/core';

@Component({
  selector: 'app-bookings-placeholder',
  template: `<p class="pt-16 text-center text-lg opacity-70">
    My bookings — coming soon
  </p>`,
})
class BookingsPlaceholderComponent {}

export const BOOKING_ROUTES: Routes = [
  { path: '', component: BookingsPlaceholderComponent },
];
