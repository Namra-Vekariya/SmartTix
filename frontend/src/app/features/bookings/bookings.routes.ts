import { Routes } from '@angular/router';
import { BookingsListComponent } from './list/bookings-list.component';
import { CheckoutComponent } from './checkout/checkout.component';
import { BookingDetailComponent } from './detail/booking-detail.component';
export const BOOKING_ROUTES: Routes = [
  { path: '', component: BookingsListComponent },
  { path: 'checkout/:holdId', component: CheckoutComponent },
  { path: ':id', component: BookingDetailComponent },
];
