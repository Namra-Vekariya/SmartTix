import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from '../../../core/tokens';
import { ApiResponse } from '../../../core/models/api-response';
import { PagedResult } from '../../../core/models/paged-result';
import {
  EventDetail,
  EventFacets,
  EventListParams,
  EventSummary,
  SeatMap,
} from '../models/event.models';

@Injectable({ providedIn: 'root' })
export class EventsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly snackBar = inject(MatSnackBar);

  // GET /api/events — pagination + filtering happen server-side.
  getEvents(params: EventListParams): Observable<PagedResult<EventSummary>> {
    return this.http
      .get<ApiResponse<PagedResult<EventSummary>>>(`${this.baseUrl}/api/events`, {
        params: this.toHttpParams(params),
      })
      .pipe(map((response) => this.unwrap(response)));
  }

  /** GET /api/events/facets — distinct genres/cities for the filter dropdowns. */
  getFacets(): Observable<EventFacets> {
    return this.http
      .get<ApiResponse<EventFacets>>(`${this.baseUrl}/api/events/facets`)
      .pipe(map((response) => this.unwrap(response)));
  }

  /** GET /api/events/{id} */
  getEvent(id: string): Observable<EventDetail> {
    return this.http
      .get<ApiResponse<EventDetail>>(`${this.baseUrl}/api/events/${id}`)
      .pipe(map((response) => this.unwrap(response)));
  }

  /** GET /api/events/{id}/seats — full seat map for the detail page. */
  getSeatMap(eventId: string): Observable<SeatMap> {
    return this.http
      .get<ApiResponse<SeatMap>>(`${this.baseUrl}/api/events/${eventId}/seats`)
      .pipe(map((response) => this.unwrap(response)));
  }

  /**
   * Unwraps the ApiResponse envelope. HTTP-level failures (4xx/5xx/network)
   * are toasted by the global error interceptor; envelope failures
   * (success=false or missing data on a 200) never reach that interceptor,
   * so they are toasted here and rethrown for the caller's error path.
   */
  private unwrap<T>(response: ApiResponse<T>): T {
    if (!response.success || response.data === null) {
      const message = response.message || 'Received an unexpected empty response';
      this.snackBar.open(message, 'Close', { duration: 4000 });
      throw new Error(message);
    }
    return response.data;
  }

  private toHttpParams(params: EventListParams): HttpParams {
    let httpParams = new HttpParams();

    const entries: [string, string | number | undefined][] = [
      ['page', params.page],
      ['pageSize', params.pageSize],
      ['city', params.city],
      ['genre', params.genre],
      ['dateFrom', params.dateFrom],
      ['dateTo', params.dateTo],
      ['search', params.search],
      ['sortBy', params.sortBy],
      ['sortDirection', params.sortDirection],
    ];

    for (const [key, value] of entries) {
      if (value !== undefined && value !== null && value !== '') {
        httpParams = httpParams.set(key, value);
      }
    }

    return httpParams;
  }
}
