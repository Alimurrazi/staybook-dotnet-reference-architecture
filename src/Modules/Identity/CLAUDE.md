# Identity module

Users (guest, host, admin) and the current-user abstraction. Keycloak issues the tokens.
Schema: `identity`. Introduced in **article 4**; until then this is an empty skeleton.

## Layout

```
Staybook.Identity/
  Domain/          User profile and roles
  Application/     Current-user abstraction and use cases
  Infrastructure/  Keycloak and JWT bearer configuration, claims mapping
  Endpoints/       Minimal API endpoints, mapped in IdentityModule.MapIdentityEndpoints
  IdentityModule.cs
Staybook.Identity.Contracts/   ICurrentUser and user IDs that other modules depend on
```

## Rules

- Handlers depend on the current-user abstraction from `Staybook.Identity.Contracts`, never on `HttpContext` or raw claims.
- Keycloak owns credentials. Staybook never stores or sees a password.
- Role policies decide who may call an endpoint; ownership ("only this listing's host") is checked against the resource, never against an ID the client sent.
- Don't implement anything here before article 4.
