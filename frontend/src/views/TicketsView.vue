<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getTickets } from '../api/tickets'
import TicketTable from '../components/TicketTable.vue'
import type { Ticket } from '../types/ticket'

const tickets = ref<Ticket[]>([])
const isLoading = ref(true)
const errorMessage = ref('')

onMounted(async () => {
  try {
    tickets.value = await getTickets()
  } catch {
    errorMessage.value = 'Unable to load tickets. Please try again later.'
  } finally {
    isLoading.value = false
  }
})
</script>

<template>
  <section class="page">
    <h1>Tickets</h1>
    <p>Review support tickets from the current ticket queue.</p>

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
