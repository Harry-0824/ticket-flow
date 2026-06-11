import type { Ticket } from '../types/ticket'

export const mockTickets = [
  {
    id: 1,
    title: 'Unable to access billing dashboard',
    description: 'The customer can sign in but receives an empty state when opening billing.',
    status: 'Open',
    priority: 'High',
    assignee: 'Mina Lee',
    createdAt: '2026-06-01T09:15:00.000Z',
    updatedAt: '2026-06-01T10:20:00.000Z',
  },
  {
    id: 2,
    title: 'Update company name on invoice',
    description: 'Finance requested the legal company name to be corrected before renewal.',
    status: 'InProgress',
    priority: 'Medium',
    assignee: 'Noah Lin',
    createdAt: '2026-06-02T14:40:00.000Z',
    updatedAt: '2026-06-03T08:05:00.000Z',
  },
  {
    id: 3,
    title: 'Password reset email delayed',
    description: 'The requester reports that password reset messages arrive after the token expires.',
    status: 'Done',
    priority: 'High',
    assignee: 'Mina Lee',
    createdAt: '2026-06-04T03:30:00.000Z',
    updatedAt: '2026-06-04T05:12:00.000Z',
  },
  {
    id: 4,
    title: 'Clarify onboarding checklist wording',
    description: 'A new teammate found one onboarding checklist item ambiguous.',
    status: 'Archived',
    priority: 'Low',
    assignee: '',
    createdAt: '2026-06-05T11:00:00.000Z',
    updatedAt: '2026-06-06T16:45:00.000Z',
  },
] satisfies Ticket[]
