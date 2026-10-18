import { RenderMode, ServerRoute } from '@angular/ssr';

/**
 * Server routing configuration specifying rendering modes.
 * Root route is rendered on the server while dynamic interactive views use client-side rendering.
 */
export const serverRoutes: ServerRoute[] = [
  {
    path: '',
    renderMode: RenderMode.Server
  },
  {
    path: '**',
    renderMode: RenderMode.Client
  }
];

