using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Staybook.Identity;

/// <summary>
/// Registers the Identity module with the host. Staybook.Api calls these two methods and knows
/// nothing else about the module.
/// </summary>
public static class IdentityModule
{
    /// <summary>The PostgreSQL schema this module owns. No other module reads it.</summary>
    public const string Schema = "identity";

    public static IHostApplicationBuilder AddIdentityModule(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Services, Marten documents (in the schema above) and validators are registered here.
        return builder;
    }

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // Endpoints from the Endpoints/ folder are mapped here.
        return endpoints;
    }
}
