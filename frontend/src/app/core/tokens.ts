import { InjectionToken } from '@angular/core';

/** Origin of the .NET API, e.g. http://localhost:5065 (dev). */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL');
