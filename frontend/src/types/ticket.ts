export type TicketStatus = 'open' | 'in_progress' | 'resolved' | 'closed'

export type TicketPriority = 'low' | 'medium' | 'high' | 'urgent'

export type Ticket = {
  id: string
  title: string
  description: string
  status: TicketStatus
  priority: TicketPriority
  requesterName: string
  assigneeName: string | null
  createdAt: string
  updatedAt: string
}
