import { Routes } from '@angular/router';

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
      }
    ]
  },
  {
    path: 'employer',
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
      }
    ]
  },
  {
    path: 'employee',
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
