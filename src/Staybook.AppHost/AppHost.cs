// Starts everything Staybook needs locally with one command:
//   dotnet run --project src/Staybook.AppHost
// Aspire runs PostgreSQL and Keycloak as containers (Docker must be running), passes
// their connection details to the API, and opens a dashboard with logs, traces and metrics.

var builder = DistributedApplication.CreateBuilder(args);

// One database for the whole monolith; each module owns its own schema inside it.
// The data volume keeps data between runs.
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("staybook-postgres-data");
var database = postgres.AddDatabase("staybook");

// Registered now so the topology is complete; the realm, users and token validation
// are configured in article 4. The API doesn't reference it yet.
// No fixed port yet, so it can't clash with anything on 8080; article 4 decides whether
// a stable issuer URL needs one.
builder.AddKeycloak("keycloak")
    .WithDataVolume("staybook-keycloak-data");

builder.AddProject<Projects.Staybook_Api>("api")
    .WithReference(database)
    .WaitFor(database);

builder.Build().Run();
