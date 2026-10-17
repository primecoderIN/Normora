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
  },
  CannotRemoveLastAdmin: 'Cannot remove the last administrator from the workspace.',
  PlanLimitExceeded: {
    summary: 'Plan Limit Exceeded',
    detail: 'Your workspace has reached its subscription resource limit. Please upgrade your plan to proceed.'
  },
  CheckoutSuccess: {
    summary: 'Payment Successful',
    detail: 'Your subscription has been activated successfully.'
  },
  CheckoutCancelled: {
    summary: 'Checkout Cancelled',
    detail: 'The checkout process was cancelled. No charges were made.'
  },
  CheckoutFailed: {
    summary: 'Checkout Failed',
    detail: 'Unable to initiate checkout session. Please try again.'
  },
  PortalFailed: {
    summary: 'Portal Error',
    detail: 'Unable to open billing portal. Please try again.'
  },
  SubscriptionUpdated: {
    summary: 'Subscription Updated',
    detail: 'Your subscription plan has been updated successfully.'
  },
  SubscriptionUpdateFailed: {
    summary: 'Update Failed',
    detail: 'Could not change the subscription plan. Please try again.'
  },
  SubscriptionQuotaFailed: {
    summary: 'Failed to Load Quotas',
    detail: 'Failed to retrieve subscription quotas. Please try again.'
  },
  DocumentUploaded: {
    summary: 'Success',
    detail: 'Document uploaded successfully'
  },
  DocumentDeleted: {
    summary: 'Success',
    detail: 'Document deleted successfully'
  },
  DocumentDeleteFailed: {
    summary: 'Error',
    detail: 'Failed to delete document'
  },
  EmployeeRemoved: {
    summary: 'Success',
    detail: 'Employee removed successfully'
  },
  EmployeeRemoveFailed: {
    summary: 'Error',
    detail: 'Failed to remove employee'
  },
  BrandingSaved: {
    summary: 'Saved!',
    detail: 'Branding changes applied to all users.'
  },
  BrandingSaveFailed: {
    summary: 'Error',
    detail: 'Failed to save branding.'
  },
  InvitationAccepted: {
    summary: 'Success',
    detail: 'Invitation accepted successfully'
  },
  InvitationAcceptFailed: {
    summary: 'Error',
    detail: 'Failed to accept invitation'
  },
  InvitationRejected: {
    summary: 'Rejected',
    detail: 'Invitation rejected successfully'
  },
  InvitationRejectFailed: {
    summary: 'Error',
    detail: 'Failed to reject invitation'
  }
} as const;

