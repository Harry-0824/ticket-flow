import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  createTicket as createTicketRequest,
  getTicketApiErrorMessage,
  getTickets,
} from '../api/tickets'
import { useTickets } from './useTickets'
import type { Ticket } from '../types/ticket'

vi.mock('../api/tickets', () => ({
  createTicket: vi.fn(),
  getTicketApiErrorMessage: vi.fn((_error: unknown, fallback: string) => fallback),
  getTickets: vi.fn(),
}))

const mockedCreateTicket = vi.mocked(createTicketRequest)
const mockedGetTicketApiErrorMessage = vi.mocked(getTicketApiErrorMessage)
const mockedGetTickets = vi.mocked(getTickets)

const ticket: Ticket = {
  id: 1,
  title: 'Login issue',
  description: 'User cannot sign in.',
  status: 'Open',
  priority: 'High',
  assignee: 'Alex',
  createdAt: '2026-06-23T00:00:00Z',
  updatedAt: '2026-06-23T00:00:00Z',
}

describe('useTickets', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('loads tickets with optional filters', async () => {
    mockedGetTickets.mockResolvedValueOnce([ticket])
    const state = useTickets()

    await state.loadTickets({
      status: 'Open',
      priority: 'High',
      keyword: 'login',
    })

    expect(mockedGetTickets).toHaveBeenCalledWith({
      status: 'Open',
      priority: 'High',
      keyword: 'login',
    })
    expect(state.tickets.value).toEqual([ticket])
    expect(state.errorMessage.value).toBe('')
    expect(state.isLoading.value).toBe(false)
  })

  it('sets an error message when loading fails', async () => {
    mockedGetTickets.mockRejectedValueOnce(new Error('Network failed'))
    const state = useTickets()

    await state.loadTickets()

    expect(state.tickets.value).toEqual([])
    expect(state.errorMessage.value).toBe('目前無法載入工單，請稍後再試。')
    expect(state.isLoading.value).toBe(false)
  })

  it('creates a ticket and returns the created record', async () => {
    mockedCreateTicket.mockResolvedValueOnce(ticket)
    const state = useTickets()
    const payload = {
      title: 'Login issue',
      description: 'User cannot sign in.',
      status: 'Open' as const,
      priority: 'High' as const,
      assignee: 'Alex',
    }

    await expect(state.createTicket(payload)).resolves.toEqual(ticket)

    expect(mockedCreateTicket).toHaveBeenCalledWith(payload)
    expect(state.errorMessage.value).toBe('')
  })

  it('sets an error message when creating a ticket fails', async () => {
    const error = new Error('Validation failed')
    mockedCreateTicket.mockRejectedValueOnce(error)
    mockedGetTicketApiErrorMessage.mockReturnValueOnce('Title is required.')
    const state = useTickets()

    await expect(
      state.createTicket({
        title: '',
        description: 'User cannot sign in.',
        status: 'Open',
        priority: 'High',
        assignee: 'Alex',
      }),
    ).resolves.toBeUndefined()

    expect(mockedGetTicketApiErrorMessage).toHaveBeenCalledWith(
      error,
      '目前無法建立工單，請稍後再試。',
    )
    expect(state.errorMessage.value).toBe('Title is required.')
  })
})
