import { API_BASE_URL } from './config'
import { emitUnauthorized, getStoredToken } from './session'
import type { Ticket, TicketPriority, TicketStatus } from '../types/ticket'

export type TicketQueryParams = {
  status?: TicketStatus
  priority?: TicketPriority
  keyword?: string
}

export type CreateTicketInput = Omit<Ticket, 'id' | 'createdAt' | 'updatedAt'>

export type UpdateTicketInput = Partial<CreateTicketInput>

export type TicketValidationError = {
  message: string
  errors: Record<string, string[]>
}

const ticketsUrl = (path = '') => `${API_BASE_URL}/tickets${path}`

export class TicketApiError extends Error {
  readonly status: number
  readonly validation?: TicketValidationError

  constructor(status: number, validation?: TicketValidationError) {
    super(validation?.message ?? `Ticket API request failed with status ${status}`)
    this.status = status
    this.validation = validation
  }
}

export const buildTicketQueryString = (params: TicketQueryParams = {}) => {
  const searchParams = new URLSearchParams()

  if (params.status) {
    searchParams.set('status', params.status)
  }

  if (params.priority) {
    searchParams.set('priority', params.priority)
  }

  if (params.keyword) {
    searchParams.set('keyword', params.keyword)
  }

  const queryString = searchParams.toString()
  return queryString ? `?${queryString}` : ''
}

const requestJson = async <T>(url: string, init?: RequestInit): Promise<T> => {
  const response = await fetch(url, {
    headers: buildHeaders(init?.headers, true),
    ...init,
  })

  if (!response.ok) {
    if (response.status === 401) {
      emitUnauthorized()
    }

    throw new TicketApiError(response.status, await readValidationError(response))
  }

  return response.json() as Promise<T>
}

const buildHeaders = (headers?: HeadersInit, includeJson = false) => {
  const nextHeaders = new Headers(headers)

  if (includeJson) {
    nextHeaders.set('Content-Type', 'application/json')
  }

  const token = getStoredToken()
  if (token) {
    nextHeaders.set('Authorization', `Bearer ${token}`)
  }

  return nextHeaders
}

const readValidationError = async (
  response: Response,
): Promise<TicketValidationError | undefined> => {
  if (response.status !== 400) {
    return undefined
  }

  try {
    return (await response.json()) as TicketValidationError
  } catch {
    return undefined
  }
}

export const getTicketApiErrorMessage = (
  error: unknown,
  fallback: string,
) => {
  if (!(error instanceof TicketApiError)) {
    return fallback
  }

  const fieldMessage = Object.values(error.validation?.errors ?? {})
    .flat()
    .find(Boolean)

  return fieldMessage ?? error.validation?.message ?? fallback
}

export const getTickets = (params?: TicketQueryParams) =>
  requestJson<Ticket[]>(ticketsUrl(buildTicketQueryString(params)))

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
    headers: buildHeaders(),
  })

  if (!response.ok) {
    if (response.status === 401) {
      emitUnauthorized()
    }

    throw new Error(`Ticket API request failed with status ${response.status}`)
  }
}
