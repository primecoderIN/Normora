using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Normora.Infrastructure.Agents;

namespace Normora.Infrastructure.Extensions;

/// <summary>
/// Exposes extension methods for mapping AG-UI CopilotKit protocol endpoints.
/// </summary>
public static class AguiEndpointExtensions
{
    public static IEndpointConventionBuilder MapNormoraAguiServer(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/api/copilotkit")
    {
        return endpoints.MapAGUIServer(
            pattern,
            httpContext => httpContext.RequestServices.GetRequiredService<NormoraWorkplaceAgent>());
    }
}

