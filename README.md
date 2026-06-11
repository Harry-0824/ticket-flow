# TicketFlow

TicketFlow is a portfolio-focused ticket management project planned as a Vue 3 frontend plus an ASP.NET Core Web API backend.

## Goal

Build a small, reviewable MVP that demonstrates a practical support-ticket workflow for freelance and portfolio use. The first implementation should stay focused on core ticket creation, listing, detail review, and status updates.

## Planned Tech Stack

- Frontend: Vue 3
- Backend: ASP.NET Core Web API
- Database: SQL Server or SQLite, to be selected during implementation
- API style: REST JSON endpoints

## MVP Scope

- Create a ticket with title, description, status, priority, and requester name.
- View a list of tickets.
- View one ticket detail page.
- Update ticket status.
- Keep data model and UI intentionally simple.

## Out of Scope

The MVP explicitly excludes:

- Authentication
- User roles or permissions
- Docker
- CI/CD
- Notifications
- File upload
- Production deployment
- Advanced search, reporting, or analytics
- Multi-tenant or billing features

## Planned Repository Structure

```text
/frontend
  Vue 3 application

/backend
  ASP.NET Core Web API application
```

This structure is planned only. Application folders should be created by later implementation issues.

## Planned API Routes

- `GET /api/tickets` - list tickets
- `GET /api/tickets/{id}` - get ticket details
- `POST /api/tickets` - create a ticket
- `PATCH /api/tickets/{id}/status` - update ticket status

## Planned Frontend Routes

- `/` - ticket list
- `/tickets/new` - create ticket
- `/tickets/:id` - ticket detail

## Local Setup Placeholder

Local setup instructions will be added after the frontend and backend projects exist. No app code or package files are included in this planning step.

## MVP Limitations

- The MVP is not production-ready.
- Security, authentication, deployment, and operational concerns are intentionally deferred.
- The first version should optimize for readable code and clear portfolio value, not full enterprise workflow coverage.

## Portfolio Positioning

TicketFlow is intended to show end-to-end product thinking, API design, frontend/backend integration, and disciplined scope control in a small ticket-management system.
