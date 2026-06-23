# Production Smoke Test

This script verifies the deployed TicketFlow API without storing secrets in the
repository.

## Command

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\production-smoke.ps1 -ApiBaseUrl "https://<render-service>.onrender.com"
```

You can also set the API origin with an environment variable:

```powershell
$env:TICKETFLOW_API_BASE_URL = "https://<render-service>.onrender.com"
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\production-smoke.ps1
```

`-ApiBaseUrl` may include or omit a trailing `/api`; the script normalizes both
forms.

## Coverage

- `GET /health`
- `POST /api/auth/register` with a unique smoke-test email
- `POST /api/auth/login`
- `POST /api/tickets`
- `GET /api/tickets`
- `GET /api/tickets/{id}`
- `PUT /api/tickets/{id}`
- `DELETE /api/tickets/{id}`

## Cleanup

The script deletes the ticket it creates. If a later step fails after ticket
creation, the `finally` cleanup path attempts to delete that ticket before the
script exits.

The public API does not expose a user-delete endpoint, so the temporary smoke
test account can remain in the database. The script uses a unique email in the
form `tf-smoke-<runId>@ticketflow.local` to avoid conflicts and to make any
remaining test accounts easy to identify.

## Failure Behavior

Each step prints one `PASS`, `FAIL`, or `WARN` line. A failed required step exits
with a non-zero status code and identifies the failing step without printing the
generated password or bearer token.
