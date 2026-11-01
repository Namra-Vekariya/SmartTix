export type BookingStatus = 'Pending' | 'Confirmed' | 'Cancelled' | 'Refunded' | 'Expired';

export type PaymentStatus = 'Pending' | 'Paid' | 'Failed' | 'Refunded';

export interface HoldSeatsRequest {
  eventId: string;
  seatIds: string[];
}

export interface HeldSeat {
  seatId: string;
  seatCode: string; // "A-1"
  categoryName: string; // "VIP"
  price: number;
  rowLabel: string;
  seatNumber: number;
}

export interface HoldSeatsResponse {
  holdId: string;
  eventId: string;
  eventName: string;
  expiresAt: string;
  totalAmount: number;
  seats: HeldSeat[];
}

export interface HoldDetails {
  holdId: string;
  bookingId: string;
  status: BookingStatus;
  eventId: string;
  eventName: string;
  expiresAt: string;
  totalAmount: number;
  seats: HeldSeat[];
}

export interface MockPaymentDetails {
  cardNumber: string; // 16 digits
  expiry: string; // MM/YY
  cvv: string; // 3-4 digits
}

export interface ConfirmBookingRequest {
  holdId: string;
  fullName: string;
  email: string;
  phone: string;
  payment: MockPaymentDetails;
}

export interface BookingEvent {
  id: string;
  name: string;
  artistName: string;
  venueName: string;
  venueCity: string;
  eventDate: string;
  posterImageUrl: string | null;
}

export interface BookingSeatDetail {
  seatId: string;
  seatCode: string;
  rowLabel: string;
  seatNumber: number;
  categoryName: string;
  priceAtBooking: number;
}

/** GET /api/Bookings/my, GET /api/Bookings/{id}, POST confirm response. */
export interface BookingDetail {
  id: string;
  bookingReference: string; // "SMX-2026-48291"
  status: BookingStatus;
  paymentStatus: PaymentStatus;
  totalAmount: number;
  bookedAt: string;
  expiresAt: string;
  event: BookingEvent;
  bookerName: string;
  bookerEmail: string;
  bookerPhone: string;
  seats: BookingSeatDetail[];
}
