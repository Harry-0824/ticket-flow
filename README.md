# TicketFlow

TicketFlow is a small full-stack ticket management MVP built for portfolio and interview discussion. It demonstrates a focused CRUD workflow: a user can view, filter, create, edit, and delete support tickets through a Vue frontend connected to an ASP.NET Core Web API.

The project is intentionally scoped as an MVP. It favors clear product boundaries, readable implementation, and end-to-end data flow over enterprise features.

## Tech Stack

- Frontend: Vue 3, TypeScript, Vue Router, Vite
- Backend: ASP.NET Core Web API
- Data access: EF Core
- Database: SQLite
- API style: REST JSON endpoints

## Completed MVP Features

- Ticket list loaded from the backend API
- Status, priority, and keyword filters using backend-supported query params
- Ticket detail page loaded by ticket id
- Create ticket form
- Edit ticket form
- Delete ticket action with confirmation
- Loading, error, empty, and not-found states across the main ticket flows

## Data Flow

The frontend calls the API service in `frontend/src/api/tickets.ts`. That service sends HTTP requests to the ASP.NET Core backend under `/api/tickets`.

The backend handles ticket CRUD through minimal API endpoints, uses EF Core for data access, and persists ticket records in SQLite. Ticket data flows back to the Vue views as JSON and is rendered through focused pages and reusable badge/table components.

## Architecture

```text
frontend/
  Vue 3 app, routes, views, components, and API client

backend/
  ASP.NET Core Web API, EF Core DbContext, models, and migrations
```

The frontend keeps state local to each page for this MVP. The backend keeps the ticket API simple and aligned to the current ticket model: title, description, status, priority, assignee, created time, and updated time.

## API Contract Summary

- `GET /api/tickets`
  - Returns the ticket list.
  - Supports `status`, `priority`, and `keyword` query params.
- `GET /api/tickets/{id}`
  - Returns one ticket by id.
- `POST /api/tickets`
  - Creates a ticket.
- `PUT /api/tickets/{id}`
  - Updates a ticket.
- `DELETE /api/tickets/{id}`
  - Deletes a ticket.

## Local Development

Run the backend:

```bash
cd backend
dotnet restore
dotnet run
```

Run the frontend:

```bash
cd frontend
npm install
npm run dev
```

Build the frontend:

```bash
cd frontend
npm run build
```

## Portfolio Story

TicketFlow is a compact example of taking a product slice from planning to working full-stack MVP. The work is organized around small GitHub Issues, each with a narrow scope, validation step, and pull request.

The project shows:

- API contract alignment between frontend and backend
- Real API integration instead of mock data
- Incremental CRUD workflow delivery
- Simple route and view boundaries
- Practical loading, error, and not-found handling
- Scope discipline for issue-driven development

## Out of Scope

These are not implemented in the current MVP:

- Authentication or roles
- Docker setup
- Deployment setup
- CI setup
- Notifications
- File uploads
- Reporting or analytics
- Multi-tenant or billing workflows

Future work can add these capabilities after the core ticket workflow remains stable.
