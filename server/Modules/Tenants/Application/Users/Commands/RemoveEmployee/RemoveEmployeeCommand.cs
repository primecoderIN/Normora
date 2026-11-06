using MediatR;
using Normora.Shared.Interfaces;

namespace Normora.Modules.Tenants.Application.Users.Commands;

/// <summary>Soft-removes a tenant membership while retaining identity and removal audit data.</summary>
public record RemoveEmployeeCommand(Guid MembershipId) : IRequest<bool>, IAuditableRequest
{
    public string Action => "Employee.Removed";
    public string TargetType => "Employee";
    public string? GetTargetId(object? response) => MembershipId.ToString();
    public string? GetMetadata(object? response) => null;
}

