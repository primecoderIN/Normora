import { Routes } from '@angular/router';
import { authGuard } from '@core/guards/auth.guard';
import { roleGuard } from '@core/guards/role.guard';
import { workspaceGuard } from '@core/guards/workspace.guard';

export const routes: Routes = [
  // Public vs Protected routes: Login and callbacks are public, but all workspace views are strictly protected by our Auth and Role guards
  {
    path: '',
    loadComponent: () => import('./features/auth/login/login').then(m => m.Login),
    pathMatch: 'full'
  },
  {
    path: 'auth',
    children: [
      {
        // Dedicated OAuth callback route where Keycloak redirects with the authorization code so we can exchange it for a session token
        // The AuthCallback component handles the token exchange and navigates to the dashboard.
        path: 'callback',
        loadComponent: () => import('./features/auth/callback/callback').then(m => m.AuthCallback)
      }
    ]
  },
  {
    path: 'accept-invite',
    loadComponent: () => import('./features/accept-invite/accept-invite.component').then(m => m.AcceptInviteComponent)
  },
  {
    // UX-2 / SEC-11 / SEC-12: Dedicated access-denied page for authenticated users
    // who lack the required role or workspace membership. Redirected here by role.guard
    // and workspace.guard instead of the confusing /auth/login redirect.
    path: 'access-denied',
    loadComponent: () => import('./features/access-denied/access-denied.component').then(m => m.AccessDeniedComponent)
  },
  {
    path: 'onboarding',
    canActivate: [authGuard],
    loadComponent: () => import('./features/onboarding/onboarding.component').then(m => m.OnboardingComponent)
  },
  {
    path: 'app/workspaces/:slug',
    canActivate: [authGuard, workspaceGuard],
    children: [
      {
        path: 'employer',
        canActivate: [roleGuard],
        data: { role: 'employer' },
        loadComponent: () => import('./layout/employer-layout/employer-layout').then(m => m.EmployerLayout),
        children: [
          {
            path: '',
            redirectTo: 'dashboard',
            pathMatch: 'full'
          },
          {
            path: 'dashboard',
            loadComponent: () => import('./features/employer/dashboard/dashboard').then(m => m.Dashboard)
          },
          {
            path: 'documents',
            loadComponent: () => import('./features/employer/documents/documents').then(m => m.Documents)
          },
          {
            path: 'conversations',
            loadComponent: () => import('./features/employee/conversations/conversations').then(m => m.Conversations)
          },
          {
            path: 'employees',
            loadComponent: () => import('./features/employer/employees/employees').then(m => m.Employees)
          },
          {
            // Settings is a nested group — each sub-page gets its own lazy chunk
            path: 'settings',
            children: [
              {
                path: '',
                redirectTo: 'departments',
                pathMatch: 'full'
              },
              {
                path: 'departments',
                loadComponent: () => import('./features/employer/departments/departments').then(m => m.Departments)
              },
              {
                path: 'user-groups',
                loadComponent: () => import('./features/employer/user-groups/user-groups').then(m => m.UserGroups)
              },
              {
                path: 'branding',
                loadComponent: () => import('./features/employer/settings/branding/branding.component').then(m => m.BrandingComponent)
              }
            ]
          }
        ]
      },
      {
        path: 'employee',
        canActivate: [roleGuard],
        data: { role: 'employee' },
        loadComponent: () => import('./layout/employee-layout/employee-layout').then(m => m.EmployeeLayout),
        children: [
          {
            path: '',
            redirectTo: 'conversations',
            pathMatch: 'full'
          },
          {
            path: 'conversations',
            loadComponent: () => import('./features/employee/conversations/conversations').then(m => m.Conversations)
          },
          {
            path: 'saved-answers',
            loadComponent: () => import('./features/employee/saved-answers/saved-answers').then(m => m.SavedAnswers)
          },
          {
            path: 'profile',
            loadComponent: () => import('./features/employee/profile/profile').then(m => m.ProfileComponent)
          }
        ]
      }
    ]
  },
  // Wildcard — render a premium 404 page for unknown URLs
  {
    path: '**',
    loadComponent: () => import('./features/not-found/not-found.component').then(m => m.NotFoundComponent)
  }
];
