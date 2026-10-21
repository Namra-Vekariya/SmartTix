import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, UntypedFormGroup } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { debounceTime, Subject, takeUntil } from 'rxjs';
import { PagedResult } from '../../../core/models/paged-result';
import { EventsService } from '../data/events.service';
import { EventFacets, EventListParams, EventSummary } from '../models/event.models';

const SORT_OPTIONS = [
  { value: 'date-asc', label: 'Date ↑' },
  { value: 'date-desc', label: 'Date ↓' },
  { value: 'price-asc', label: 'Price ↑' },
  { value: 'price-desc', label: 'Price ↓' },
  { value: 'popularity-desc', label: 'Most popular' },
] as const;

@Component({
  selector: 'app-events-list',
  imports: [
    CurrencyPipe,
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
  ],
  templateUrl: './events-list.component.html',
  styleUrl: './events-list.component.scss',
})
export class EventsListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly eventsService = inject(EventsService);
  private readonly destroy$ = new Subject<void>();

  readonly loading = signal(false);
  readonly result = signal<PagedResult<EventSummary> | null>(null);
  readonly facets = signal<EventFacets>({ genres: [], cities: [] });
  readonly sortOptions = SORT_OPTIONS;

  readonly filters: UntypedFormGroup = this.fb.nonNullable.group({
    search: [''],
    city: [''],
    genre: [''],
    dateFrom: [''],
    dateTo: [''],
    sort: ['date-asc'],
  });

  ngOnInit(): void {
    this.eventsService
      .getFacets()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (facets) => this.facets.set(facets),
        // Toast comes from the service/interceptor; keep dropdowns empty.
        error: () => {},
      });

    // First page load is explicit. valueChanges never emits on subscribe,
    // so every emission below is a real user filter change.
    this.load({ page: 1 });

    this.filters.valueChanges
      .pipe(debounceTime(300), takeUntil(this.destroy$))
      .subscribe(() => this.load({ page: 1 }));
  }

  onPage(event: PageEvent): void {
    this.load({ page: event.pageIndex + 1, pageSize: event.pageSize });
  }

  clearFilters(): void {
    this.filters.reset({
      search: '',
      city: '',
      genre: '',
      dateFrom: '',
      dateTo: '',
      sort: 'date-asc',
    });
  }

  get hasActiveFilters(): boolean {
    const { search, city, genre, dateFrom, dateTo, sort } = this.filters.getRawValue();
    return Boolean(search || city || genre || dateFrom || dateTo || sort !== 'date-asc');
  }

  private load(overrides: Partial<EventListParams>): void {
    const { search, city, genre, dateFrom, dateTo, sort } = this.filters.getRawValue();
    const [sortBy, sortDirection] = (sort as string).split('-') as [
      EventListParams['sortBy'],
      EventListParams['sortDirection'],
    ];

    const params: EventListParams = {
      page: overrides.page ?? this.result()?.page ?? 1,
      pageSize: overrides.pageSize ?? this.result()?.pageSize ?? 12,
      search: search || undefined,
      city: city || undefined,
      genre: genre || undefined,
      dateFrom: dateFrom ? `${dateFrom}T00:00:00` : undefined,
      dateTo: dateTo ? `${dateTo}T23:59:59` : undefined,
      sortBy,
      sortDirection,
    };

    this.loading.set(true);
    this.eventsService
      .getEvents(params)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.result.set(result);
          this.loading.set(false);
        },
        // Errors surface via the global error interceptor.
        error: () => this.loading.set(false),
      });
  }
}
