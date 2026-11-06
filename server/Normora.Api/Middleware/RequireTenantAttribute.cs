namespace Normora.Api.Middleware;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireTenantAttribute(params string[] roles) 
    : Normora.Shared.Authorization.RequireTenantAttribute(roles);
