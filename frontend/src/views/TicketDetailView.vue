<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import { deleteTicket, getTicketById, TicketApiError } from '../api/tickets'
import PriorityBadge from '../components/PriorityBadge.vue'
import StatusBadge from '../components/StatusBadge.vue'
import type { Ticket } from '../types/ticket'

const route = useRoute()
const router = useRouter()

const ticket = ref<Ticket | null>(null)
const isLoading = ref(true)
const isDeleting = ref(false)
const errorMessage = ref('')
const deleteErrorMessage = ref('')
const isNotFound = ref(false)
const isConfirmingDelete = ref(false)

const formatDateTime = (date: string) =>
  new Intl.DateTimeFormat('en', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  }).format(new Date(date))

const deleteCurrentTicket = async () => {
  if (!ticket.value) {
    return
  }

  deleteErrorMessage.value = ''
  isDeleting.value = true

  try {
    await deleteTicket(ticket.value.id)
    await router.push('/tickets')
  } catch {
    deleteErrorMessage.value =
      'Unable to delete this ticket. Please try again later.'
  } finally {
    isDeleting.value = false
  }
}

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

      <div class="ticket-actions">
        <button
          v-if="!isConfirmingDelete"
          type="button"
          class="danger-button"
          @click="isConfirmingDelete = true"
        >
          Delete ticket
        </button>
      </div>

      <div v-if="isConfirmingDelete" class="delete-confirmation">
        <p>This action cannot be undone.</p>
        <button
          type="button"
          class="danger-button"
          :disabled="isDeleting"
          @click="deleteCurrentTicket"
        >
          {{ isDeleting ? 'Deleting...' : 'Confirm delete' }}
        </button>
        <button
          type="button"
          class="secondary-button"
          :disabled="isDeleting"
          @click="isConfirmingDelete = false"
        >
          Cancel
        </button>
      </div>

      <div v-if="deleteErrorMessage" class="form-error" role="alert">
        {{ deleteErrorMessage }}
      </div>
    </article>
  </section>
</template>
