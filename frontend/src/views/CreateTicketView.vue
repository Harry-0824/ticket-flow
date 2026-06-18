<script setup lang="ts">
import { reactive, ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import { createTicket, getTicketApiErrorMessage } from '../api/tickets'
import type { CreateTicketInput } from '../api/tickets'
import type { TicketPriority, TicketStatus } from '../types/ticket'

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
    errorMessage.value = '請填寫標題與描述。'
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
  } catch (error) {
    errorMessage.value = getTicketApiErrorMessage(
      error,
      '目前無法建立工單，請稍後再試。',
    )
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <section class="page">
    <RouterLink to="/tickets">返回工單列表</RouterLink>

    <h1>建立工單</h1>
    <p>新增一筆支援工單，讓目前佇列保持清楚可追蹤。</p>

    <form class="ticket-form" @submit.prevent="submitTicket">
      <div class="form-intro">
        <strong>工單內容</strong>
        <span>標題與描述是必填欄位，其餘欄位可依目前狀態調整。</span>
      </div>

      <label>
        標題
        <input v-model="form.title" required placeholder="例如：登入頁錯誤" />
      </label>

      <label>
        描述
        <textarea
          v-model="form.description"
          required
          rows="5"
          placeholder="描述使用者遇到的問題、重現步驟或需要協助的內容"
        ></textarea>
      </label>

      <label>
        狀態
        <select v-model="form.status">
          <option v-for="status in statusOptions" :key="status" :value="status">
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
        <input v-model="form.assignee" placeholder="未指派可留空" />
      </label>

      <div v-if="errorMessage" class="form-error" role="alert">
        {{ errorMessage }}
      </div>

      <div class="form-actions">
        <button type="submit" :disabled="isSubmitting">
          {{ isSubmitting ? '建立中...' : '建立工單' }}
        </button>
        <RouterLink class="secondary-link" to="/tickets">取消</RouterLink>
      </div>
    </form>
  </section>
</template>
