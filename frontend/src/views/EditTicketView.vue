<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import { getTicketById, TicketApiError, updateTicket } from '../api/tickets'
import type { UpdateTicketInput } from '../api/tickets'
import type { TicketPriority, TicketStatus } from '../types/ticket'

const route = useRoute()
const router = useRouter()

const statusOptions: TicketStatus[] = ['Open', 'InProgress', 'Done', 'Archived']
const priorityOptions: TicketPriority[] = ['Low', 'Medium', 'High']

const form = reactive<Required<UpdateTicketInput>>({
  title: '',
  description: '',
  status: 'Open',
  priority: 'Medium',
  assignee: '',
})

const ticketId = ref<number | null>(null)
const isLoading = ref(true)
const isSubmitting = ref(false)
const errorMessage = ref('')
const submitErrorMessage = ref('')
const isNotFound = ref(false)

const submitTicket = async () => {
  submitErrorMessage.value = ''

  if (ticketId.value === null) {
    isNotFound.value = true
    return
  }

  if (!form.title.trim() || !form.description.trim()) {
    submitErrorMessage.value = 'Title and description are required.'
    return
  }

  isSubmitting.value = true

  try {
    const updatedTicket = await updateTicket(ticketId.value, {
      title: form.title.trim(),
      description: form.description.trim(),
      status: form.status,
      priority: form.priority,
      assignee: form.assignee.trim(),
    })

    await router.push(`/tickets/${updatedTicket.id || ticketId.value}`)
  } catch {
    submitErrorMessage.value =
      'Unable to update ticket. Please try again later.'
  } finally {
    isSubmitting.value = false
  }
}

onMounted(async () => {
  const id = Number(route.params.id)

  if (!Number.isInteger(id) || id <= 0) {
    isNotFound.value = true
    isLoading.value = false
    return
  }

  ticketId.value = id

  try {
    const ticket = await getTicketById(id)

    form.title = ticket.title
    form.description = ticket.description
    form.status = ticket.status
    form.priority = ticket.priority
    form.assignee = ticket.assignee
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
    <RouterLink :to="ticketId ? `/tickets/${ticketId}` : '/tickets'">
      Back to ticket
    </RouterLink>

    <div v-if="isLoading" class="placeholder-panel" role="status">
      Loading ticket...
    </div>

    <div v-else-if="errorMessage" class="placeholder-panel" role="alert">
      {{ errorMessage }}
    </div>

    <div v-else-if="isNotFound" class="placeholder-panel">
      Ticket not found.
    </div>

    <template v-else>
      <h1>Edit ticket</h1>
      <p>Update the ticket details and return to the ticket page.</p>

      <form class="ticket-form" @submit.prevent="submitTicket">
        <label>
          Title
          <input v-model="form.title" required />
        </label>

        <label>
          Description
          <textarea v-model="form.description" required rows="5"></textarea>
        </label>

        <label>
          Status
          <select v-model="form.status">
            <option
              v-for="status in statusOptions"
              :key="status"
              :value="status"
            >
              {{ status }}
            </option>
          </select>
        </label>

        <label>
          Priority
          <select v-model="form.priority">
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
          Assignee
          <input v-model="form.assignee" />
        </label>

        <div v-if="submitErrorMessage" class="form-error" role="alert">
          {{ submitErrorMessage }}
        </div>

        <button type="submit" :disabled="isSubmitting">
          {{ isSubmitting ? 'Saving...' : 'Save changes' }}
        </button>
      </form>
    </template>
  </section>
</template>
