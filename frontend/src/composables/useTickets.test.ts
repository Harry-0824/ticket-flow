import { describe, expect, it, vi } from 'vitest'
import { getTickets } from '../api/tickets'
import { useTickets } from './useTickets'
import type { Ticket } from '../types/ticket'

vi.mock('../api/tickets', () => ({
  getTickets: vi.fn(),
}))

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
})
