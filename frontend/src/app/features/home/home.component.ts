import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-home',
  imports: [RouterLink, MatButtonModule],
  template: `
    <section class="mx-auto max-w-4xl pt-16 text-center">
      <h1 class="text-4xl font-bold">Find your next event</h1>
      <p class="mt-4 text-lg opacity-70">
        Browse events, pick your seats in real time, and book in seconds.
      </p>
      <div class="mt-8 flex justify-center gap-4">
        <a matButton="filled" routerLink="/events">Browse events</a>
        <a mat-stroked-button routerLink="/register">Create account</a>
      </div>
    </section>
  `,
})
export class HomeComponent {}
