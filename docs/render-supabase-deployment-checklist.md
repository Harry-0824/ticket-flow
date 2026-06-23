# Render + Supabase Deployment Checklist

Use this checklist before and after deploying TicketFlow to Render, Netlify, and
Supabase. Do not paste real secrets into this file, issues, pull requests, or
chat logs.

## 1. Render Backend Settings

Check the Render web service before deploy:

- Service type: Web Service.
- Runtime: Docker.
- Branch: `main`.
- Dockerfile path: `./backend/Dockerfile`.
- Docker context: `./backend`.
- Health check path: `/health`.
- Plan: Free is acceptable for the portfolio demo, with cold-start limits.

Required Render environment variables:

```text
ASPNETCORE_ENVIRONMENT=Production
Database__Provider=PostgreSQL
ConnectionStrings__TicketFlowPostgres=<supabase-session-pooler-connection-string>
Jwt__Issuer=TicketFlow
Jwt__Audience=TicketFlowClient
Jwt__Secret=<at-least-32-byte-random-secret>
Jwt__ExpiresMinutes=60
Cors__AllowedOrigins=https://<netlify-site>.netlify.app
```

Rules:

- Keep `ConnectionStrings__TicketFlowPostgres` and `Jwt__Secret` as Render
  secret/env values only.
- Do not commit `.env`, local connection strings, database passwords, service
  role keys, JWT secrets, or copied dashboard values.
- Confirm `Cors__AllowedOrigins` exactly matches the production Netlify origin.
- Render provides runtime metadata such as `RENDER_GIT_COMMIT`; use the deployed
  `/build-info` endpoint after Issue #93 is deployed to confirm the running
  commit.

## 2. Supabase PostgreSQL Settings

Use the Supabase project dashboard to confirm:

- Database is active and reachable.
- Connection string is copied from Project Settings -> Database -> Connect.
- For this long-running Render web service, prefer the Session Pooler connection
  string on port `5432` when direct IPv6 is not available.
- Keep Transaction Pooler on port `6543` for serverless or highly transient
  workloads; verify provider compatibility before switching because transaction
  pooling has different session behavior.
- The connection string user/password are never committed.

Security checks:

- Confirm the app uses backend-managed ASP.NET Core JWT auth, not Supabase Auth.
- Confirm the frontend does not receive Supabase secret keys or database
  connection strings.
- If any table is exposed through Supabase Data API, verify explicit grants and
  RLS policies. TicketFlow currently uses a trusted backend database connection,
  so Data API exposure is not required for normal app traffic.
- Review Supabase Security Advisor findings before production release.

## 3. Migration And Schema Checks

Before deploy:

- Confirm the latest merged code includes the expected EF Core migrations under
  `backend/Migrations/`.
- Confirm PostgreSQL SQL artifacts exist under `backend/Migrations/Postgres/`
  for production review.
- Confirm migration SQL does not contain local paths, passwords, or copied
  connection strings.

During deploy:

- Render starts the backend from `main`.
- The backend applies EF Core migrations at startup.
- Watch the Render deploy log for migration or database connection failures.

After deploy:

- Confirm Render reports a successful deploy.
- Confirm `GET /health` returns `200` and `{"status":"Healthy"}`.
- If Issue #93 is deployed, confirm `GET /build-info` returns JSON with
  `commit` and `environment`; missing optional values should be `unknown`.
- Compare `/build-info.commit` with the expected `main` commit or Render deploy
  metadata.

## 4. Frontend And CORS Checks

Check Netlify:

- Build base: `frontend`.
- Build command: `npm ci && npm run build`.
- Publish directory: `dist`.
- SPA redirect sends `/*` to `/index.html` with status `200`.

Check frontend environment:

```text
VITE_API_BASE_URL=https://<render-service>.onrender.com/api
```

Validation:

- Open the Netlify root URL.
- Refresh `/login`, `/register`, and `/tickets` directly; they should not return
  a Netlify 404.
- Register and login through the UI after the backend deploy is healthy.

## 5. Production Smoke Validation

Run the smoke script from Issue #92 after Render deploy completes:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\production-smoke.ps1 -ApiBaseUrl "https://<render-service>.onrender.com"
```

Expected coverage:

- `/health`
- register with a unique smoke-test email
- login
- create ticket
- list tickets
- read ticket
- update ticket
- delete ticket

Expected cleanup:

- The smoke-created ticket is deleted.
- The smoke-created account may remain because the public API has no user-delete
  endpoint. The email uses `tf-smoke-<runId>@ticketflow.local` for easy review.

## 6. Release Closeout

Before marking a deployment done:

- All issue PRs required for the release are merged to `develop`.
- A release PR from `develop` to `main` is merged.
- Render has deployed the latest `main`.
- `/health` passes on production.
- `/build-info` identifies the deployed commit when available.
- Production smoke script passes.
- A `main -> develop` sync PR is merged if `main` gained release-only commits.
- No secrets, local DB files, or generated smoke credentials are committed.

## References

- Supabase database connection docs:
  https://supabase.com/docs/guides/database/connecting-to-postgres
- Supabase RLS docs:
  https://supabase.com/docs/guides/database/postgres/row-level-security
- Supabase API security docs:
  https://supabase.com/docs/guides/api/securing-your-api
- Render default environment variables:
  https://render.com/docs/environment-variables
