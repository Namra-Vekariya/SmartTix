/** Mirrors SmartTix.Application.DTOs.Seats — SignalR "seatsChanged" payloads. */
export interface SeatsChangedEvent {
  eventId: string;
  status: 'Available' | 'Held' | 'Booked' | 'Blocked';
  reason: string;
  seats: ChangedSeat[];
  timestamp: string;
}

export interface ChangedSeat {
  seatId: string;
  seatCode: string;
}
