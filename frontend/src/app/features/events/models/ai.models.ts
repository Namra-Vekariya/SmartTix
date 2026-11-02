export type AiRecommendStatus = 'ok' | 'out_of_scope' | 'no_results';

export interface AiRecommendRequest {
  eventId: string;
  query: string;
}

export interface SeatRecommendation {
  seatCodes: string[]; // e.g. ["A-1", "A-2", "A-3"]
  reason: string;
  totalPrice: number;
  categoryName: string;
}

export interface AiRecommendResponse {
  status: AiRecommendStatus;
  summary: string;
  recommendations: SeatRecommendation[];
}
