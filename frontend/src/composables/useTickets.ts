import { ref } from 'vue'
import { getTickets } from '../api/tickets'
import type { TicketQueryParams } from '../api/tickets'
import type { Ticket } from '../types/ticket'

const loadTicketsErrorMessage = '目前無法載入工單，請稍後再試。'

export const useTickets = () => {
  const tickets = ref<Ticket[]>([])
  const isLoading = ref(true)
  const errorMessage = ref('')

  const loadTickets = async (filters?: TicketQueryParams) => {
    isLoading.value = true
    errorMessage.value = ''

    try {
      tickets.value = await getTickets(filters)
    } catch {
      errorMessage.value = loadTicketsErrorMessage
    } finally {
      isLoading.value = false
    }
  }

  return {
    tickets,
    isLoading,
    errorMessage,
    loadTickets,
  }
}
