<script setup lang="ts">
import { reactive, ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import { createTicket } from '../api/tickets'
import type { CreateTicketInput } from '../api/tickets'
import type { TicketPriority, TicketStatus } from '../types/ticket'

const router = useRouter()

const statusOptions: TicketStatus[] = ['Open', 'InProgress', 'Done', 'Archived']
const priorityOptions: TicketPriority[] = ['Low', 'Medium', 'High']

const form = reactive<CreateTicketInput>({
  title: '',
  description: '',
  status: 'Open',
  priority: 'Medium',
  assignee: '',
})

const isSubmitting = ref(false)
const errorMessage = ref('')

const submitTicket = async () => {
  errorMessage.value = ''

  if (!form.title.trim() || !form.description.trim()) {
    errorMessage.value = 'Title and description are required.'
    return
  }

  isSubmitting.value = true

  try {
    const createdTicket = await createTicket({
      title: form.title.trim(),
      description: form.description.trim(),
      status: form.status,
      priority: form.priority,
      assignee: form.assignee.trim(),
    })

    await router.push(
      createdTicket.id ? `/tickets/${createdTicket.id}` : '/tickets',
    )
  } catch {
    errorMessage.value = 'Unable to create ticket. Please try again later.'
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <section class="page">
    <RouterLink to="/tickets">Back to tickets</RouterLink>

    <h1>Create ticket</h1>
    <p>Add a new support ticket to the current queue.</p>

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
          <option v-for="status in statusOptions" :key="status" :value="status">
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

      <div v-if="errorMessage" class="form-error" role="alert">
        {{ errorMessage }}
      </div>

      <button type="submit" :disabled="isSubmitting">
        {{ isSubmitting ? 'Creating...' : 'Create ticket' }}
      </button>
    </form>
  </section>
</template>
