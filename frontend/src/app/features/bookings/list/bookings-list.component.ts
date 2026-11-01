import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subject, takeUntil } from 'rxjs';
import { BookingService } from '../data/booking.service';
import { BookingDetail, BookingStatus } from '../models/booking.models';

/** Status → chip color classes (Tailwind). */
const STATUS_STYLES: Record<BookingStatus, string> = {
  Pending: 'bg-amber-100 text-amber-800',
  Confirmed: 'bg-green-100 text-green-800',
  Cancelled: 'bg-gray-100 text-gray-600',
  Expired: 'bg-gray-100 text-gray-600',
  Refunded: 'bg-blue-100 text-blue-800',
};

@Component({
  selector: 'app-bookings-list',
  imports: [
    CurrencyPipe,
    DatePipe,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './bookings-list.component.html',
})
export class BookingsListComponent implements OnInit {
  private readonly bookingService = inject(BookingService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroy$ = new Subject<void>();

  readonly loading = signal(true);
  readonly bookings = signal<BookingDetail[]>([]);

  readonly downloadingId = signal<string | null>(null);

  ngOnInit(): void {
    this.bookingService
      .getMyBookings()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (bookings) => {
          this.bookings.set(bookings);
          this.loading.set(false);
        },
        // Interceptor toasted the error; empty list is the safe fallback.
        error: () => this.loading.set(false),
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  statusClass(status: BookingStatus): string {
    return STATUS_STYLES[status] ?? 'bg-gray-100 text-gray-600';
  }

  seatSummary(booking: BookingDetail): string {
    const codes = booking.seats.map((s) => s.seatCode);
    return codes.join(', ');
  }

  downloadTicket(booking: BookingDetail): void {
    if (booking.status !== 'Confirmed') return;

    this.downloadingId.set(booking.id);
    this.bookingService
      .downloadTicket(booking.id, booking.bookingReference)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.downloadingId.set(null);
          this.snackBar.open('Ticket downloaded.', 'Close', { duration: 3000 });
        },
        error: () => this.downloadingId.set(null),
      });
  }
}
