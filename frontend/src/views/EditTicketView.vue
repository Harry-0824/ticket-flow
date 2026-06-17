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
    submitErrorMessage.value = '請填寫標題與描述。'
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
    submitErrorMessage.value = '目前無法更新工單，請稍後再試。'
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

    errorMessage.value = '目前無法載入此工單，請稍後再試。'
  } finally {
    isLoading.value = false
  }
})
</script>

<template>
  <section class="page">
    <RouterLink :to="ticketId ? `/tickets/${ticketId}` : '/tickets'">
      返回工單
    </RouterLink>

    <div v-if="isLoading" class="placeholder-panel" role="status">
      正在載入工單...
    </div>

    <div v-else-if="errorMessage" class="placeholder-panel" role="alert">
      {{ errorMessage }}
    </div>

    <div v-else-if="isNotFound" class="placeholder-panel">
      找不到此工單。
    </div>

    <template v-else>
      <h1>編輯工單</h1>
      <p>更新工單內容，儲存後回到工單詳細頁。</p>

      <form class="ticket-form" @submit.prevent="submitTicket">
        <label>
          標題
          <input v-model="form.title" required />
        </label>

        <label>
          描述
          <textarea v-model="form.description" required rows="5"></textarea>
        </label>

        <label>
          狀態
          <select v-model="form.status">
            <option
              v-for="status in statusOptions"
              :key="status"
              :value="status"
            >
              {{ statusLabels[status] }}
            </option>
          </select>
        </label>

        <label>
          優先級
          <select v-model="form.priority">
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
          負責人
          <input v-model="form.assignee" />
        </label>

        <div v-if="submitErrorMessage" class="form-error" role="alert">
          {{ submitErrorMessage }}
        </div>

        <button type="submit" :disabled="isSubmitting">
          {{ isSubmitting ? '儲存中...' : '儲存變更' }}
        </button>
      </form>
    </template>
  </section>
</template>
