using Marten;
using Staybook.Identity;
using Staybook.Listings;
using Staybook.Pricing;
using Staybook.ServiceDefaults;
using Wolverine;

// The composition root: it wires infrastructure and registers modules, and holds no
// business logic of its own.

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// The "staybook" connection comes from Aspire (AppHost). This also adds a database
// health check and Npgsql tracing and metrics.
builder.AddNpgsqlDataSource("staybook");

// Registered only. Modules add their documents and schemas from article 3, and Booking
// adds event streams in article 5.
builder.Services.AddMarten(_ => { })
    .UseNpgsqlDataSource();

// Registered only. Handlers arrive in article 3; durable messaging and the outbox in article 7.
builder.Host.UseWolverine();

// Each module registers its own services; the host only lists them.
builder.AddIdentityModule();
builder.AddListingsModule();
builder.AddPricingModule();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapIdentityEndpoints();
app.MapListingsEndpoints();
app.MapPricingEndpoints();

app.Run();
