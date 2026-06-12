<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { getTicketById, TicketApiError } from '../api/tickets'
import PriorityBadge from '../components/PriorityBadge.vue'
import StatusBadge from '../components/StatusBadge.vue'
import type { Ticket } from '../types/ticket'

const route = useRoute()

const ticket = ref<Ticket | null>(null)
const isLoading = ref(true)
const errorMessage = ref('')
const isNotFound = ref(false)

const formatDateTime = (date: string) =>
  new Intl.DateTimeFormat('en', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  }).format(new Date(date))

onMounted(async () => {
  const ticketId = Number(route.params.id)

  if (!Number.isInteger(ticketId) || ticketId <= 0) {
    isNotFound.value = true
    isLoading.value = false
    return
  }

  try {
    ticket.value = await getTicketById(ticketId)
  } catch (error) {
    if (error instanceof TicketApiError && error.status === 404) {
      isNotFound.value = true
      return
    }

    errorMessage.value = 'Unable to load this ticket. Please try again later.'
  } finally {
    isLoading.value = false
  }
})
</script>

<template>
  <section class="page">
    <RouterLink to="/tickets">Back to tickets</RouterLink>

    <div v-if="isLoading" class="placeholder-panel" role="status">
      Loading ticket...
    </div>

    <div v-else-if="errorMessage" class="placeholder-panel" role="alert">
      {{ errorMessage }}
    </div>

    <div v-else-if="isNotFound" class="placeholder-panel">
      Ticket not found.
    </div>

    <article v-else-if="ticket" class="placeholder-panel">
      <h1>{{ ticket.title }}</h1>
      <p>{{ ticket.description }}</p>
      <p>
        <StatusBadge :status="ticket.status" />
        <PriorityBadge :priority="ticket.priority" />
      </p>

      <dl>
        <div>
          <dt>Assignee</dt>
          <dd>{{ ticket.assignee || 'Unassigned' }}</dd>
        </div>
        <div>
          <dt>Created</dt>
          <dd>{{ formatDateTime(ticket.createdAt) }}</dd>
        </div>
        <div>
          <dt>Updated</dt>
          <dd>{{ formatDateTime(ticket.updatedAt) }}</dd>
        </div>
      </dl>
    </article>
  </section>
</template>
