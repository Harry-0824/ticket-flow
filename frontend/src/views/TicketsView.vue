<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getTickets } from '../api/tickets'
import TicketTable from '../components/TicketTable.vue'
import type { Ticket, TicketPriority, TicketStatus } from '../types/ticket'

const tickets = ref<Ticket[]>([])
const isLoading = ref(true)
const errorMessage = ref('')
const statusFilter = ref<TicketStatus | ''>('')
const priorityFilter = ref<TicketPriority | ''>('')
const keywordFilter = ref('')

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

const loadTickets = async () => {
  isLoading.value = true
  errorMessage.value = ''

  try {
    tickets.value = await getTickets({
      status: statusFilter.value || undefined,
      priority: priorityFilter.value || undefined,
      keyword: keywordFilter.value.trim() || undefined,
    })
  } catch {
    errorMessage.value = '目前無法載入工單，請稍後再試。'
  } finally {
    isLoading.value = false
  }
}

const clearFilters = async () => {
  statusFilter.value = ''
  priorityFilter.value = ''
  keywordFilter.value = ''
  await loadTickets()
}

onMounted(async () => {
  await loadTickets()
})
</script>

<template>
  <section class="page">
    <h1>工單列表</h1>
    <p>檢視目前佇列中的支援工單，並依狀態、優先級或關鍵字篩選。</p>

    <form class="ticket-filters" @submit.prevent="loadTickets">
      <label>
        狀態
        <select v-model="statusFilter">
          <option value="">全部狀態</option>
          <option v-for="status in statusOptions" :key="status" :value="status">
            {{ statusLabels[status] }}
          </option>
        </select>
      </label>

      <label>
        優先級
        <select v-model="priorityFilter">
          <option value="">全部優先級</option>
          <option
            v-for="priority in priorityOptions"
            :key="priority"
            :value="priority"
          >
            {{ priorityLabels[priority] }}
          </option>
        </select>
      </label>

      <label>
        關鍵字
        <input
          v-model="keywordFilter"
          type="search"
          placeholder="搜尋標題或描述"
        />
      </label>

      <div class="filter-actions">
        <button type="submit">套用</button>
        <button type="button" class="secondary-button" @click="clearFilters">
          清除
        </button>
      </div>
    </form>

    <div v-if="isLoading" class="placeholder-panel" role="status">
      正在載入工單...
    </div>

    <div v-else-if="errorMessage" class="placeholder-panel" role="alert">
      {{ errorMessage }}
    </div>

    <div v-else-if="tickets.length === 0" class="placeholder-panel">
      沒有符合條件的工單。
    </div>

    <TicketTable v-else :tickets="tickets" />
  </section>
</template>
