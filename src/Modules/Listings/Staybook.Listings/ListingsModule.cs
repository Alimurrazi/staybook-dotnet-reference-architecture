using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Staybook.Listings;

/// <summary>
/// Registers the Listings module with the host. Staybook.Api calls these two methods and knows
/// nothing else about the module.
/// </summary>
public static class ListingsModule
{
    /// <summary>The PostgreSQL schema this module owns. No other module reads it.</summary>
    public const string Schema = "listings";

    public static IHostApplicationBuilder AddListingsModule(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Services, Marten documents (in the schema above) and validators are registered here.
        return builder;
    }

    public static IEndpointRouteBuilder MapListingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // Endpoints from the Endpoints/ folder are mapped here.
        return endpoints;
    }
}
