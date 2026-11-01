import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subject, finalize, takeUntil } from 'rxjs';
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
  selector: 'app-booking-detail',
  imports: [
    CurrencyPipe,
    DatePipe,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './booking-detail.component.html',
  styleUrl: './booking-detail.component.scss',
})
export class BookingDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly bookingService = inject(BookingService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroy$ = new Subject<void>();

  readonly loading = signal(true);
  readonly booking = signal<BookingDetail | null>(null);
  readonly justConfirmed = signal(false);

  readonly downloading = signal(false);
  readonly cancelling = signal(false);
  /** Inline confirm step for cancel — no browser dialogs. */
  readonly confirmCancel = signal(false);

  private bookingId = '';

  ngOnInit(): void {
    this.bookingId = this.route.snapshot.paramMap.get('id') ?? '';
    this.justConfirmed.set(this.route.snapshot.queryParamMap.get('confirmed') === '1');
    this.load();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  statusClass(status: BookingStatus): string {
    return STATUS_STYLES[status] ?? 'bg-gray-100 text-gray-600';
  }

  canDownloadTicket(): boolean {
    return this.booking()?.status === 'Confirmed';
  }

  canCancel(): boolean {
    return this.booking()?.status === 'Pending';
  }

  downloadTicket(): void {
    const booking = this.booking();
    if (!booking || !this.canDownloadTicket()) return;

    this.downloading.set(true);
    this.bookingService
      .downloadTicket(booking.id, booking.bookingReference)
      .pipe(
        finalize(() => this.downloading.set(false)),
        takeUntil(this.destroy$),
      )
      .subscribe({
        next: () => this.snackBar.open('Ticket downloaded.', 'Close', { duration: 3000 }),
        // Errors are toasted by the global interceptor.
        error: () => {},
      });
  }

  cancelBooking(): void {
    const booking = this.booking();
    if (!booking || !this.canCancel()) return;

    this.cancelling.set(true);
    this.bookingService
      .cancelBooking(booking.id)
      .pipe(
        finalize(() => this.cancelling.set(false)),
        takeUntil(this.destroy$),
      )
      .subscribe({
        next: () => {
          this.confirmCancel.set(false);
          this.justConfirmed.set(false);
          this.snackBar.open('Booking cancelled — your seats are available again.', 'Close', {
            duration: 4000,
          });
          this.load();
        },
        // Errors are toasted by the global interceptor.
        error: () => this.confirmCancel.set(false),
      });
  }

  private load(): void {
    this.loading.set(true);
    this.bookingService
      .getBooking(this.bookingId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (booking) => {
          this.booking.set(booking);
          this.loading.set(false);
        },
        error: () => {
          // Interceptor toasted (404/401) — booking list is the safe fallback.
          this.loading.set(false);
          void this.router.navigate(['/bookings']);
        },
      });
  }
}
