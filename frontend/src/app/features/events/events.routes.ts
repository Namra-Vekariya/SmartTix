import { Routes } from '@angular/router';

export const EVENT_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./list/events-list.component').then(
        (m) => m.EventsListComponent,
      ),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./detail/event-detail.component').then(
        (m) => m.EventDetailComponent,
      ),
  },
];
