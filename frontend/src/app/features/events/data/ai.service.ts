import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from '../../../core/tokens';
import { ApiResponse } from '../../../core/models/api-response';
import { AiRecommendRequest, AiRecommendResponse } from '../models/ai.models';

@Injectable({ providedIn: 'root' })
export class AiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly snackBar = inject(MatSnackBar);

  /** POST /api/Ai/recommend — natural-language seat concierge (rate-limited). */
  recommend(request: AiRecommendRequest): Observable<AiRecommendResponse> {
    return this.http
      .post<ApiResponse<AiRecommendResponse>>(`${this.baseUrl}/api/Ai/recommend`, request)
      .pipe(map((response) => this.unwrap(response)));
  }

  private unwrap<T>(response: ApiResponse<T>): T {
    if (!response.success || response.data === null) {
      const message = response.message || 'Received an unexpected empty response';
      this.snackBar.open(message, 'Close', { duration: 4000 });
      throw new Error(message);
    }
    return response.data;
  }
}
