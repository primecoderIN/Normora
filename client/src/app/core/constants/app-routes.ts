export const AppRoutes = {
  Login: '/',
  AuthCallback: '/auth/callback',
  Onboarding: '/onboarding',
  AcceptInvite: '/accept-invite',
  AccessDenied: '/access-denied',
  EmployerDashboard: '/employer/dashboard',
  EmployeeConversations: '/employee/conversations',
  
  // Helper to build dynamic workspace routes
  WorkspaceRoot: (slug: string) => `/app/workspaces/${slug}`,
  WorkspaceEmployerDashboard: (slug: string) => `/app/workspaces/${slug}/employer/dashboard`,
  WorkspaceEmployeeConversations: (slug: string) => `/app/workspaces/${slug}/employee/conversations`
};
