import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from '../../../core/tokens';
import { ApiResponse } from '../../../core/models/api-response';
import {
  BookingDetail,
  ConfirmBookingRequest,
  HoldDetails,
  HoldSeatsRequest,
  HoldSeatsResponse,
} from '../models/booking.models';

@Injectable({ providedIn: 'root' })
export class BookingService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly snackBar = inject(MatSnackBar);

  /** POST /api/Bookings/hold — marks seats Held for 10 minutes. */
  holdSeats(request: HoldSeatsRequest): Observable<HoldSeatsResponse> {
    return this.http
      .post<ApiResponse<HoldSeatsResponse>>(`${this.baseUrl}/api/Bookings/hold`, request)
      .pipe(map((response) => this.unwrap(response)));
  }

  /** GET /api/Bookings/hold/{holdId} — checkout refresh support. */
  getHold(holdId: string): Observable<HoldDetails> {
    return this.http
      .get<ApiResponse<HoldDetails>>(`${this.baseUrl}/api/Bookings/hold/${holdId}`)
      .pipe(map((response) => this.unwrap(response)));
  }

  releaseHold(holdId: string): Observable<void> {
    return this.http.delete(`${this.baseUrl}/api/Bookings/hold/${holdId}`).pipe(map(() => void 0));
  }

  /** POST /api/Bookings/confirm — mock payment; seats become Booked. */
  confirmBooking(request: ConfirmBookingRequest): Observable<BookingDetail> {
    return this.http
      .post<ApiResponse<BookingDetail>>(`${this.baseUrl}/api/Bookings/confirm`, request)
      .pipe(map((response) => this.unwrap(response)));
  }

  getMyBookings(): Observable<BookingDetail[]> {
    return this.http
      .get<ApiResponse<BookingDetail[]>>(`${this.baseUrl}/api/Bookings/my`)
      .pipe(map((response) => this.unwrap(response)));
  }

  getBooking(id: string): Observable<BookingDetail> {
    return this.http
      .get<ApiResponse<BookingDetail>>(`${this.baseUrl}/api/Bookings/${id}`)
      .pipe(map((response) => this.unwrap(response)));
  }

  cancelBooking(id: string): Observable<void> {
    return this.http.put(`${this.baseUrl}/api/Bookings/${id}/cancel`, {}).pipe(map(() => void 0));
  }

  downloadTicket(id: string, bookingReference: string): Observable<void> {
    return this.http
      .get(`${this.baseUrl}/api/Bookings/${id}/ticket`, {
        responseType: 'blob',
        headers: new HttpHeaders({ Accept: 'application/pdf' }),
      })
      .pipe(map((blob) => this.saveBlob(blob, `${bookingReference}.pdf`)));
  }

  private unwrap<T>(response: ApiResponse<T>): T {
    if (!response.success || response.data === null) {
      const message = response.message || 'Received an unexpected empty response';
      this.snackBar.open(message, 'Close', { duration: 4000 });
      throw new Error(message);
    }
    return response.data;
  }

  private saveBlob(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = filename;
    anchor.click();
    URL.revokeObjectURL(url);
  }
}
