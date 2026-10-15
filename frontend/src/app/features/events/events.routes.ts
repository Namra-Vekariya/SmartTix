import { Routes } from '@angular/router';
import { Component } from '@angular/core';

@Component({
  selector: 'app-events-placeholder',
  template: `<p class="pt-16 text-center text-lg opacity-70">
    Events — coming soon
  </p>`,
})
class EventsPlaceholderComponent {}

export const EVENT_ROUTES: Routes = [
  { path: '', component: EventsPlaceholderComponent },
];
