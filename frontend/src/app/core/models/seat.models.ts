/** Mirrors SmartTix.Application.DTOs.Seats — SignalR "seatsChanged" payloads. */
export interface SeatsChangedEvent {
  eventId: string;
  status: 'held' | 'confirmed' | 'payment_failed' | 'cancelled' | 'expired';
  reason: string;
  seats: ChangedSeat[];
  timestamp: string;
}

export interface ChangedSeat {
  seatId: string;
  seatCode: string;
}
