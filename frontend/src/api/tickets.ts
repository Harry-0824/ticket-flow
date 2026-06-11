import { API_BASE_URL } from './config'
import type { Ticket, TicketPriority, TicketStatus } from '../types/ticket'

export type TicketQueryParams = {
  status?: TicketStatus
  priority?: TicketPriority
  assignee?: string
  search?: string
}

export type CreateTicketInput = Omit<Ticket, 'id' | 'createdAt' | 'updatedAt'>

export type UpdateTicketInput = Partial<CreateTicketInput>

const ticketsUrl = (path = '') => `${API_BASE_URL}/tickets${path}`

const buildQueryString = (params: TicketQueryParams = {}) => {
  const searchParams = new URLSearchParams()

  Object.entries(params).forEach(([key, value]) => {
    if (value) {
      searchParams.set(key, value)
    }
  })

  const queryString = searchParams.toString()
  return queryString ? `?${queryString}` : ''
}

const requestJson = async <T>(url: string, init?: RequestInit): Promise<T> => {
  const response = await fetch(url, {
    headers: {
      'Content-Type': 'application/json',
      ...init?.headers,
    },
    ...init,
  })

  if (!response.ok) {
    throw new Error(`Ticket API request failed with status ${response.status}`)
  }

  return response.json() as Promise<T>
}

export const getTickets = (params?: TicketQueryParams) =>
  requestJson<Ticket[]>(ticketsUrl(buildQueryString(params)))

export const getTicketById = (id: Ticket['id']) =>
  requestJson<Ticket>(ticketsUrl(`/${id}`))

export const createTicket = (ticket: CreateTicketInput) =>
  requestJson<Ticket>(ticketsUrl(), {
    method: 'POST',
    body: JSON.stringify(ticket),
  })

export const updateTicket = (id: Ticket['id'], ticket: UpdateTicketInput) =>
  requestJson<Ticket>(ticketsUrl(`/${id}`), {
    method: 'PUT',
    body: JSON.stringify(ticket),
  })

export const deleteTicket = async (id: Ticket['id']) => {
  const response = await fetch(ticketsUrl(`/${id}`), {
    method: 'DELETE',
  })

  if (!response.ok) {
    throw new Error(`Ticket API request failed with status ${response.status}`)
  }
}
