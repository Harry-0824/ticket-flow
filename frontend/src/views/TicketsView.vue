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
    errorMessage.value = 'Unable to load tickets. Please try again later.'
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
    <h1>Tickets</h1>
    <p>Review support tickets from the current ticket queue.</p>

    <form class="ticket-filters" @submit.prevent="loadTickets">
      <label>
        Status
        <select v-model="statusFilter">
          <option value="">All statuses</option>
          <option v-for="status in statusOptions" :key="status" :value="status">
            {{ status }}
          </option>
        </select>
      </label>

      <label>
        Priority
        <select v-model="priorityFilter">
          <option value="">All priorities</option>
          <option
            v-for="priority in priorityOptions"
            :key="priority"
            :value="priority"
          >
            {{ priority }}
          </option>
        </select>
      </label>

      <label>
        Keyword
        <input
          v-model="keywordFilter"
          type="search"
          placeholder="Search title or description"
        />
      </label>

      <div class="filter-actions">
        <button type="submit">Apply</button>
        <button type="button" class="secondary-button" @click="clearFilters">
          Clear
        </button>
      </div>
    </form>

    <div v-if="isLoading" class="placeholder-panel" role="status">
      Loading tickets...
    </div>

    <div v-else-if="errorMessage" class="placeholder-panel" role="alert">
      {{ errorMessage }}
    </div>

    <div v-else-if="tickets.length === 0" class="placeholder-panel">
      No tickets found.
    </div>

    <TicketTable v-else :tickets="tickets" />
  </section>
</template>
