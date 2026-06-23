import { ref } from 'vue'
import type { TicketPriority, TicketStatus } from '../types/ticket'

const statusOptions: TicketStatus[] = ['Open', 'InProgress', 'Done', 'Archived']
const priorityOptions: TicketPriority[] = ['Low', 'Medium', 'High']
const statusLabels: Record<TicketStatus, string> = {
  Open: '待處理',
  InProgress: '處理中',
  Done: '已完成',
  Archived: '已封存',
}
const priorityLabels: Record<TicketPriority, string> = {
  Low: '低',
  Medium: '中',
  High: '高',
}

export const useFilters = () => {
  const statusFilter = ref<TicketStatus | ''>('')
  const priorityFilter = ref<TicketPriority | ''>('')
  const keywordFilter = ref('')

  const clearFilters = () => {
    statusFilter.value = ''
    priorityFilter.value = ''
    keywordFilter.value = ''
  }

  return {
    statusFilter,
    priorityFilter,
    keywordFilter,
    statusOptions,
    priorityOptions,
    statusLabels,
    priorityLabels,
    clearFilters,
  }
}
