<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import PriorityBadge from '../components/PriorityBadge.vue'
import StatusBadge from '../components/StatusBadge.vue'
import { useTickets } from '../composables/useTickets'
import type { Ticket } from '../types/ticket'

const route = useRoute()
const router = useRouter()
const {
  deleteTicket,
  errorMessage: ticketErrorMessage,
  fetchTicket,
} = useTickets()

const ticket = ref<Ticket | null>(null)
const isLoading = ref(true)
const isDeleting = ref(false)
const errorMessage = ref('')
const deleteErrorMessage = ref('')
const isNotFound = ref(false)
const isConfirmingDelete = ref(false)

const formatDateTime = (date: string) =>
  new Intl.DateTimeFormat('zh-TW', {
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
    const wasDeleted = await deleteTicket(ticket.value.id)
    if (!wasDeleted) {
      deleteErrorMessage.value = ticketErrorMessage.value
      return
    }
    await router.push('/tickets')
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
    const loadedTicket = await fetchTicket(ticketId)

    if (loadedTicket === null) {
      isNotFound.value = true
      return
    }

    if (!loadedTicket) {
      errorMessage.value = ticketErrorMessage.value
      return
    }

    ticket.value = loadedTicket
  } finally {
    isLoading.value = false
  }
})
</script>

<template>
  <section class="page">
    <RouterLink to="/tickets">返回工單列表</RouterLink>

    <div v-if="isLoading" class="placeholder-panel" role="status">
      正在載入工單...
    </div>

    <div v-else-if="errorMessage" class="placeholder-panel" role="alert">
      {{ errorMessage }}
    </div>

    <div v-else-if="isNotFound" class="placeholder-panel">
      找不到此工單。
    </div>

    <article v-else-if="ticket" class="ticket-detail-card">
      <div class="ticket-detail-header">
        <div>
          <h1>{{ ticket.title }}</h1>
          <p>{{ ticket.description }}</p>
        </div>
        <RouterLink class="secondary-link" :to="`/tickets/${ticket.id}/edit`">
          編輯工單
        </RouterLink>
      </div>

      <div class="badge-row" aria-label="工單狀態與優先級">
        <StatusBadge :status="ticket.status" />
        <PriorityBadge :priority="ticket.priority" />
      </div>

      <dl class="metadata-grid">
        <div>
          <dt>負責人</dt>
          <dd>{{ ticket.assignee || '未指派' }}</dd>
        </div>
        <div>
          <dt>建立時間</dt>
          <dd>{{ formatDateTime(ticket.createdAt) }}</dd>
        </div>
        <div>
          <dt>更新時間</dt>
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
          刪除工單
        </button>
      </div>

      <div v-if="isConfirmingDelete" class="delete-confirmation">
        <p>此操作無法復原，刪除後會回到工單列表。</p>
        <button
          type="button"
          class="danger-button"
          :disabled="isDeleting"
          @click="deleteCurrentTicket"
        >
          {{ isDeleting ? '刪除中...' : '確認刪除' }}
        </button>
        <button
          type="button"
          class="secondary-button"
          :disabled="isDeleting"
          @click="isConfirmingDelete = false"
        >
          取消
        </button>
      </div>

      <div v-if="deleteErrorMessage" class="form-error" role="alert">
        {{ deleteErrorMessage }}
      </div>
    </article>
  </section>
</template>
