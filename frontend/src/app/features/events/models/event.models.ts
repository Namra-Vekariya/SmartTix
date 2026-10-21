/** Mirrors SmartTix.Application.DTOs.Events / Seats (camelCase JSON). */

export type EventStatus = 'Draft'|'Published' | 'Ongoing' | 'Completed' | 'Cancelled';

export interface EventSummary {
  id: string;
  name: string;
  artistName: string;
  venueName: string;
  venueCity: string;
  eventDate: string;
  posterImageUrl: string | null;
  genre: string;
  status: EventStatus;
  startingPrice: number;
  availableSeatsCount: number;
  totalSeatsCount: number;
}

export interface EventCategory {
  id: string;
  name: string;
  price: number;
  colorHex: string | null;
  totalSeats: number;
  availableSeats: number;
}

export interface EventDetail {
  id: string;
  name: string;
  artistName: string;
  description: string | null;
  genre: string | null;
  venueName: string;
  venueCity: string;
  venueAddress: string | null;
  eventDate: string;
  doorsOpenTime: string | null;
  posterImageUrl: string | null;
  status: EventStatus;
  createdAt: string;
  categories: EventCategory[];
  totalSeatsCount: number;
  availableSeatsCount: number;
}

export interface EventFacets {
  genres: string[];
  cities: string[];
}

export type SeatStatus = 'Available' | 'Held' | 'Booked';

export interface Seat {
  id: string;
  rowLabel: string;
  seatNumber: number;
  seatCode: string;
  status: SeatStatus;
  categoryId: string;
  categoryName: string;
  price: number;
  colorHex: string | null;
}

export interface SeatCategoryInfo {
  id: string;
  name: string;
  price: number;
  colorHex: string | null;
}

export interface SeatMap {
  eventId: string;
  eventName: string;
  categories: SeatCategoryInfo[];
  seats: Seat[];
  numberOfRows: number;
  seatsPerRow: number;
}

/** Query string sent to GET /api/events — all fields optional. */
export interface EventListParams {
  page?: number;
  pageSize?: number;
  city?: string;
  genre?: string;
  dateFrom?: string;
  dateTo?: string;
  search?: string;
  sortBy?: 'date' | 'price' | 'popularity';
  sortDirection?: 'asc' | 'desc';
}
