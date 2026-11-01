import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, UntypedFormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subject, catchError, of, takeUntil } from 'rxjs';
import { finalize } from 'rxjs';
import { AuthService } from '../../auth/data/auth.service';
import { BookingService } from '../data/booking.service';
import { HoldDetails } from '../models/booking.models';

@Component({
  selector: 'app-checkout',
  imports: [
    CurrencyPipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.scss',
})
export class CheckoutComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly bookingService = inject(BookingService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroy$ = new Subject<void>();

  readonly loading = signal(true);
  readonly hold = signal<HoldDetails | null>(null);
  readonly paying = signal(false);
  readonly remainingSeconds = signal(0);

  /** mm:ss — drives the countdown chip in the header. */
  readonly countdown = computed(() => {
    const total = this.remainingSeconds();
    const mm = Math.floor(Math.max(total, 0) / 60)
      .toString()
      .padStart(2, '0');
    const ss = (Math.max(total, 0) % 60).toString().padStart(2, '0');
    return `${mm}:${ss}`;
  });

  readonly holdExpired = computed(() => this.remainingSeconds() <= 0);

  /** Contact details — prefilled from the signed-in user. */
  readonly contactForm: UntypedFormGroup = this.fb.nonNullable.group({
    fullName: [
      this.auth.currentUser()?.fullName ?? '',
      [Validators.required, Validators.maxLength(100)],
    ],
    email: [
      this.auth.currentUser()?.email ?? '',
      [Validators.required, Validators.email, Validators.maxLength(255)],
    ],
    phone: ['', [Validators.required, Validators.maxLength(15)]],
  });

  readonly paymentForm: UntypedFormGroup = this.fb.nonNullable.group({
    cardNumber: ['', [Validators.required, Validators.pattern(/^\d{4}( ?\d{4}){3}$/)]],
    expiry: ['', [Validators.required, Validators.pattern(/^(0[1-9]|1[0-2])\/\d{2}$/)]],
    cvv: ['', [Validators.required, Validators.pattern(/^\d{3,4}$/)]],
  });

  private timer?: ReturnType<typeof setInterval>;
  private holdConfirmed = false;
  private holdReleased = false;

  ngOnInit(): void {
    const holdId = this.route.snapshot.paramMap.get('holdId') ?? '';
    if (!holdId) {
      this.failToEvents('Invalid checkout session.');
      return;
    }

    this.bookingService
      .getHold(holdId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (hold) => this.initHold(hold),
        error: () => this.failToEvents(),
      });
  }

  ngOnDestroy(): void {
    this.stopTimer();
    this.releaseHold();
    this.destroy$.next();
    this.destroy$.complete();
  }

  onCardInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const digits = input.value.replace(/\D/g, '').slice(0, 16);
    const formatted = digits.replace(/(\d{4})(?=\d)/g, '$1 ');
    this.paymentForm.get('cardNumber')?.setValue(formatted);
    input.value = formatted;
  }

  onExpiryInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    let digits = input.value.replace(/\D/g, '').slice(0, 4);
    if (digits.length >= 3) digits = `${digits.slice(0, 2)}/${digits.slice(2)}`;
    this.paymentForm.get('expiry')?.setValue(digits);
    input.value = digits;
  }

  onPhoneInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const digits = input.value.replace(/\D/g, '').slice(0, 15);
    this.contactForm.get('phone')?.setValue(digits);
    input.value = digits;
  }

  onCvvInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const digits = input.value.replace(/\D/g, '').slice(0, 4);
    this.paymentForm.get('cvv')?.setValue(digits);
    input.value = digits;
  }

  canPay(): boolean {
    return (
      this.contactForm.valid && this.paymentForm.valid && !this.paying() && !this.holdExpired()
    );
  }

  pay(): void {
    const hold = this.hold();
    if (!hold || !this.canPay()) return;

    const { fullName, email, phone } = this.contactForm.getRawValue();
    const { cardNumber, expiry, cvv } = this.paymentForm.getRawValue();

    this.paying.set(true);
    this.bookingService
      .confirmBooking({
        holdId: hold.holdId,
        fullName: fullName.trim(),
        email: email.trim(),
        phone: phone.trim(),
        payment: {
          cardNumber: this.sanitizeCardNumber(cardNumber),
          expiry,
          cvv,
        },
      })
      .pipe(
        finalize(() => this.paying.set(false)),
        takeUntil(this.destroy$),
      )
      .subscribe({
        next: (booking) => {
          this.holdConfirmed = true;
          this.stopTimer();
          void this.router.navigate(['/bookings', booking.id], {
            queryParams: { confirmed: '1' },
          });
        },
        error: () => {
          // Interceptor toasted the reason (e.g. random mock decline).
          // Seats are still held — the countdown keeps running so the user
          // can fix the form and retry.
        },
      });
  }

  // "Back to event" — release the hold, then leave checkout.
  releaseAndBack(): void {
    const hold = this.hold();
    this.releaseHold();
    void this.router.navigate(hold ? ['/events', hold.eventId] : ['/events']);
  }

  private releaseHold(): void {
    const hold = this.hold();
    if (!hold || this.holdConfirmed || this.holdReleased) return;

    this.holdReleased = true;
    this.bookingService
      .releaseHold(hold.holdId)
      .pipe(catchError(() => of(void 0)))
      .subscribe();
  }

  /** Strips display spaces so the backend regex ^\d{16}$ passes. */
  private sanitizeCardNumber(raw: string): string {
    return raw.replace(/\D/g, '');
  }

  private initHold(hold: HoldDetails): void {
    if (hold.status === 'Confirmed') {
      void this.router.navigate(['/bookings', hold.bookingId], {
        queryParams: { confirmed: '1' },
      });
      return;
    }

    if (hold.status === 'Expired' || hold.status === 'Cancelled') {
      this.snackBar.open('Your hold has expired. Please select your seats again.', 'Close', {
        duration: 5000,
      });
      void this.router.navigate(['/events', hold.eventId]);
      return;
    }

    this.hold.set(hold);
    this.startTimer(new Date(hold.expiresAt).getTime());
    this.loading.set(false);
  }

  private startTimer(expiresAtMs: number): void {
    const tick = () => {
      const remaining = Math.floor((expiresAtMs - Date.now()) / 1000);
      this.remainingSeconds.set(remaining);

      if (remaining <= 0) {
        this.stopTimer();
        this.releaseHold();
        this.snackBar.open(
          'Your hold has expired and the seats were released. Please select seats again.',
          'Close',
          { duration: 6000 },
        );
        const hold = this.hold();
        void this.router.navigate(hold ? ['/events', hold.eventId] : ['/events']);
      }
    };

    tick();
    if (this.remainingSeconds() > 0) {
      this.timer = setInterval(tick, 1000);
    }
  }

  private stopTimer(): void {
    if (this.timer !== undefined) {
      clearInterval(this.timer);
      this.timer = undefined;
    }
  }

  private failToEvents(message?: string): void {
    if (message) this.snackBar.open(message, 'Close', { duration: 4000 });
    this.loading.set(false);
    void this.router.navigate(['/events']);
  }
}
