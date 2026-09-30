export const ApiMessages = {
  Forbidden: {
    summary: 'Access Denied',
    detail: 'You do not have permission to perform this action.'
  },
  RateLimit: {
    summary: 'Slow Down',
    detail: 'You are sending requests too fast. Please wait a moment and try again.'
  },
  NotFound: {
    summary: 'Not Found',
    detail: 'The requested resource was not found or you lack permission to view it.'
  },
  ServerError: {
    summary: 'Server Error',
    detail: 'An unexpected error occurred. Please try again later.'
  }
} as const;
