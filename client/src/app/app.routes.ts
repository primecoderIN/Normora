import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
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
        // Dedicated OAuth callback route — Keycloak redirects here with ?code=&state=
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
        path: 'settings/team',
        loadComponent: () => import('./features/employer/settings/team/team-settings.component').then(m => m.TeamSettingsComponent)
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
        redirectTo: 'ask',
        pathMatch: 'full'
      },
      {
        path: 'ask',
        loadComponent: () => import('./features/employee/ask/ask').then(m => m.Ask)
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
  }
];
