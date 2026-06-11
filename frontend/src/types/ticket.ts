export type TicketStatus = 'Open' | 'InProgress' | 'Done' | 'Archived'

export type TicketPriority = 'Low' | 'Medium' | 'High'

export type Ticket = {
  id: number
  title: string
  description: string
  status: TicketStatus
  priority: TicketPriority
  assignee: string
  createdAt: string
  updatedAt: string
}
