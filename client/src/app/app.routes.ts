import { Routes } from '@angular/router';
import { authGuard } from '@core/guards/auth.guard';
import { roleGuard } from '@core/guards/role.guard';

export const routes: Routes = [
  // Public vs Protected routes: Login and callbacks are public, but all workspace views are strictly protected by our Auth and Role guards
  {
    path: '',
    redirectTo: 'auth/login',
    pathMatch: 'full'
  },
  {
    path: 'auth',
    children: [
      {
        path: 'login',
        loadComponent: () => import('./features/auth/login/login').then(m => m.Login)
      },
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
    path: 'onboarding',
    canActivate: [authGuard],
    loadComponent: () => import('./features/onboarding/onboarding.component').then(m => m.OnboardingComponent)
  },
  {
    path: 'employer',
    canActivate: [authGuard, roleGuard],
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
          }
        ]
      }
    ]
  },
  {
    path: 'employee',
    canActivate: [authGuard, roleGuard],
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
      }
    ]
  },
  // Wildcard — redirect unknown URLs to login instead of blank screen
  {
    path: '**',
    redirectTo: 'auth/login'
  }
];
