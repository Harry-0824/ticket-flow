# Supabase RLS And Security Hardening Assessment

This assessment covers the current TicketFlow backend-managed auth model and
Supabase PostgreSQL deployment. It does not change schema, application code,
Supabase policies, roles, keys, or production settings.

## Current State

Application access model:

- Frontend authenticates through `POST /api/auth/register` and
  `POST /api/auth/login`.
- The backend issues ASP.NET Core JWTs.
- Ticket API endpoints require backend JWT authorization.
- Tickets include `UserId`.
- Ticket list, detail, update, and delete operations enforce ownership in the
  backend.
- The frontend does not connect directly to Supabase.

Database access model:

- Local development defaults to SQLite.
- Production uses Supabase managed PostgreSQL through EF Core/Npgsql.
- Render stores the PostgreSQL connection string in
  `ConnectionStrings__TicketFlowPostgres`.
- Migration SQL artifacts are kept under `backend/Migrations/Postgres/`.
- The app currently uses a trusted backend database connection, not Supabase
  Auth or Supabase client-side Data API access.

## RLS Fit For The Current Architecture

RLS is valuable defense in depth, but it is not a drop-in replacement for the
current backend ownership checks.

Why:

- Supabase RLS examples commonly use Supabase Auth claims such as `auth.uid()`.
- TicketFlow currently uses ASP.NET Core JWTs, not Supabase Auth JWTs.
- The backend connects to PostgreSQL with a server-side connection string. From
  the database perspective, requests arrive through one database role, not as
  each end user.
- A simple policy like `TO authenticated USING (auth.uid() = user_id)` would not
  match TicketFlow users unless the auth model changes or the backend sets a
  trusted per-request database context.

Conclusion:

- Keep backend ownership checks as the primary authorization layer.
- Do not enable RLS blindly for the existing app role without a design issue.
- Treat RLS as a follow-up hardening track that needs explicit policy design,
  migration planning, and tests.
- If any tables are exposed through Supabase Data API, enable RLS and define
  explicit grants/policies before allowing client access.

## Risk Inventory

| Area | Current Risk | Impact | Recommendation |
| --- | --- | --- | --- |
| Ticket ownership | Backend now filters by `UserId` and checks ownership on CRUD. | Medium if future endpoints bypass the shared pattern. | Add regression tests for every new ticket endpoint and keep ownership logic mandatory. |
| Supabase Data API exposure | Normal app traffic does not need Data API access. Public schema exposure or default grants could still be risky if enabled. | High if tables are reachable with broad grants and no RLS. | Disable unused Data API exposure or verify explicit grants plus RLS for exposed tables. |
| Database connection role | A broad `postgres` or admin-like connection string gives the app more privileges than needed. | High if app code or credentials are compromised. | Create a least-privileged application DB role for runtime; keep migration privileges separate. |
| Migration execution | Startup migration is operationally convenient but couples deploy startup to schema changes. | Medium to High for production deploy reliability. | Add a separate migration/runbook issue if deploys become sensitive; keep Render logs reviewed after release. |
| Secret handling | Secrets are intended to live in Render env vars only. | High if connection string, JWT secret, or Supabase keys are committed or logged. | Continue secret scans, never paste values into PRs/issues, and rotate on suspected exposure. |
| Service role / secret keys | The current app does not require Supabase service role or secret keys. | High if introduced into frontend or logs. | Do not add service role keys unless a backend-only issue explicitly requires it. |
| CORS | Render allows the configured Netlify origin. | Medium if wildcard or stale origins are added. | Keep `Cors__AllowedOrigins` exact and review after every frontend URL change. |
| Build/deploy identity | `/build-info` is planned for non-sensitive commit/environment visibility. | Low if only non-sensitive fields are returned. | Keep endpoint limited to commit/environment/build time fallback values; never return env var dumps. |

## Recommended Follow-Up Issues

1. Least-privileged PostgreSQL runtime role

   Goal: create a runtime DB role with only the privileges required by the API,
   and keep migration/admin privileges out of the app connection string.

   Risk: High, because it changes production database access.

2. Supabase Data API exposure audit

   Goal: verify whether `public` tables are exposed through Supabase Data API,
   and document grants for `anon`, `authenticated`, and service/admin roles.

   Risk: Medium to High, depending on current grants.

3. RLS design spike for backend-managed auth

   Goal: decide whether to keep Data API disabled, introduce Supabase Auth,
   create policies around a trusted backend-set session variable, or use another
   Postgres-level authorization pattern.

   Risk: High, because incorrect policies can either block valid app traffic or
   create a false sense of security.

4. Production migration runbook

   Goal: decide whether startup migrations are sufficient for this portfolio
   deployment, or whether production should use an explicit migration step with
   rollback notes.

   Risk: Medium to High, because it affects deploy reliability.

5. Security regression tests for ownership

   Goal: add tests that fail whenever a ticket endpoint can return, update, or
   delete another user's ticket.

   Risk: Medium, because it guards API authorization behavior.

## Manual Review Checklist

- Confirm frontend code does not contain Supabase database URLs, service role
  keys, JWT secrets, or secret API keys.
- Confirm Render env vars hold all production secrets.
- Confirm Supabase Data API is not required for current app traffic.
- Confirm any exposed tables have explicit grants and RLS policies.
- Confirm backend ownership tests cover list, detail, update, and delete.
- Confirm production smoke tests use temporary accounts and clean created
  tickets.
- Confirm no document, PR, or issue includes real connection strings or tokens.

## References

- Supabase Row Level Security:
  https://supabase.com/docs/guides/database/postgres/row-level-security
- Supabase API security:
  https://supabase.com/docs/guides/api/securing-your-api
- Supabase secure data guide:
  https://supabase.com/docs/guides/database/secure-data
- Supabase API keys:
  https://supabase.com/docs/guides/getting-started/api-keys
- Supabase connection strings and poolers:
  https://supabase.com/docs/guides/database/connecting-to-postgres
