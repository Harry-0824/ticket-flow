import { ref } from 'vue'
import {
  createTicket as createTicketRequest,
  deleteTicket as deleteTicketRequest,
  getTicketApiErrorMessage,
  getTicketById,
  getTickets,
  TicketApiError,
  updateTicket as updateTicketRequest,
} from '../api/tickets'
import type {
  CreateTicketInput,
  TicketQueryParams,
  UpdateTicketInput,
} from '../api/tickets'
import type { Ticket } from '../types/ticket'

const loadTicketsErrorMessage = '目前無法載入工單，請稍後再試。'
const loadTicketErrorMessage = '目前無法載入此工單，請稍後再試。'
const createTicketErrorMessage = '目前無法建立工單，請稍後再試。'
const updateTicketErrorMessage = '目前無法更新工單，請稍後再試。'
const deleteTicketErrorMessage = '目前無法刪除此工單，請稍後再試。'

export type { CreateTicketInput, UpdateTicketInput }

export const useTickets = () => {
  const tickets = ref<Ticket[]>([])
  const isLoading = ref(true)
  const errorMessage = ref('')

  const loadTickets = async (
    filters?: TicketQueryParams,
    fallbackMessage = loadTicketsErrorMessage,
  ) => {
    isLoading.value = true
    errorMessage.value = ''

    try {
      tickets.value = await getTickets(filters)
    } catch {
      errorMessage.value = fallbackMessage
    } finally {
      isLoading.value = false
    }
  }

  const fetchTicket = async (id: Ticket['id']) => {
    errorMessage.value = ''

    try {
      return await getTicketById(id)
    } catch (error) {
      if (error instanceof TicketApiError && error.status === 404) {
        return null
      }

      errorMessage.value = loadTicketErrorMessage
      return undefined
    }
  }

  const createTicket = async (payload: CreateTicketInput) => {
    errorMessage.value = ''

    try {
      return await createTicketRequest(payload)
    } catch (error) {
      errorMessage.value = getTicketApiErrorMessage(
        error,
        createTicketErrorMessage,
      )
      return undefined
    }
  }

  const updateTicket = async (id: Ticket['id'], payload: UpdateTicketInput) => {
    errorMessage.value = ''

    try {
      return await updateTicketRequest(id, payload)
    } catch (error) {
      errorMessage.value = getTicketApiErrorMessage(
        error,
        updateTicketErrorMessage,
      )
      return undefined
    }
  }

  const deleteTicket = async (id: Ticket['id']) => {
    errorMessage.value = ''

    try {
      await deleteTicketRequest(id)
      return true
    } catch {
      errorMessage.value = deleteTicketErrorMessage
      return false
    }
  }

  return {
    tickets,
    isLoading,
    errorMessage,
    loadTickets,
    fetchTicket,
    createTicket,
    updateTicket,
    deleteTicket,
  }
}
