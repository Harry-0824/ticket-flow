<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { getTickets } from '../api/tickets'
import TicketTable from '../components/TicketTable.vue'
import { useFilters } from '../composables/useFilters'
import type { Ticket } from '../types/ticket'

const tickets = ref<Ticket[]>([])
const isLoading = ref(true)
const errorMessage = ref('')
const {
  statusFilter,
  priorityFilter,
  keywordFilter,
  statusOptions,
  priorityOptions,
  statusLabels,
  priorityLabels,
  clearFilters: resetFilters,
} = useFilters()

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
  resetFilters()
  await loadTickets()
}

onMounted(async () => {
  await loadTickets()
})
</script>

<template>
  <section class="page">
    <header class="page-header">
      <div>
        <h1>工單列表</h1>
        <p>檢視目前佇列中的支援工單，並依狀態、優先級或關鍵字篩選。</p>
      </div>
      <RouterLink class="primary-link" to="/tickets/new">建立工單</RouterLink>
    </header>

    <form class="ticket-filters" @submit.prevent="loadTickets">
      <div class="filter-heading">
        <strong>篩選條件</strong>
        <span>縮小目前工單佇列的檢視範圍</span>
      </div>

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
