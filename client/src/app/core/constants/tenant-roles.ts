export const TenantRoles = {
  Admin: 'admin',
  Employee: 'employee'
} as const;

export type TenantRole = typeof TenantRoles[keyof typeof TenantRoles];
