<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { getTickets } from '../api/tickets'
import PriorityBadge from '../components/PriorityBadge.vue'
import StatusBadge from '../components/StatusBadge.vue'
import type { Ticket } from '../types/ticket'

const tickets = ref<Ticket[]>([])
const isLoading = ref(true)
const errorMessage = ref('')

const loadDashboard = async () => {
  isLoading.value = true
  errorMessage.value = ''

  try {
    tickets.value = await getTickets()
  } catch {
    errorMessage.value = '目前無法載入工單摘要，請稍後再試。'
  } finally {
    isLoading.value = false
  }
}

const dashboardStats = computed(() => {
  const openTickets = tickets.value.filter((ticket) => ticket.status === 'Open')
  const inProgressTickets = tickets.value.filter(
    (ticket) => ticket.status === 'InProgress',
  )
  const highPriorityTickets = tickets.value.filter(
    (ticket) => ticket.priority === 'High',
  )
  const unassignedTickets = tickets.value.filter(
    (ticket) => !ticket.assignee.trim(),
  )

  return [
    { label: '總工單', value: tickets.value.length, tone: 'neutral' },
    { label: '待處理', value: openTickets.length, tone: 'open' },
    { label: '處理中', value: inProgressTickets.length, tone: 'progress' },
    { label: '高優先級', value: highPriorityTickets.length, tone: 'high' },
    { label: '未指派', value: unassignedTickets.length, tone: 'unassigned' },
  ]
})

const recentTickets = computed(() =>
  [...tickets.value]
    .sort(
      (current, next) =>
        new Date(next.updatedAt).getTime() -
        new Date(current.updatedAt).getTime(),
    )
    .slice(0, 5),
)

const formatDateTime = (date: string) =>
  new Intl.DateTimeFormat('zh-TW', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(date))

onMounted(loadDashboard)
</script>

<template>
  <section class="page">
    <div class="page-header">
      <div>
        <h1>TicketFlow</h1>
        <p>
          以 Vue 3 與 ASP.NET Core Web API 打造的小型工單管理作品，
          目前首頁直接呈現工單隊列狀況，方便快速掌握待處理工作。
        </p>
      </div>

      <RouterLink class="primary-link" to="/tickets">查看工單</RouterLink>
    </div>

    <div v-if="isLoading" class="placeholder-panel" role="status">
      正在載入工單摘要...
    </div>

    <div v-else-if="errorMessage" class="placeholder-panel" role="alert">
      {{ errorMessage }}
    </div>

    <template v-else>
      <div v-if="tickets.length === 0" class="placeholder-panel">
        目前尚無工單。建立第一筆工單後，這裡會顯示隊列摘要與最近更新。
      </div>

      <template v-else>
        <section class="dashboard-grid" aria-label="工單摘要">
          <article
            v-for="stat in dashboardStats"
            :key="stat.label"
            class="metric-card"
            :data-tone="stat.tone"
          >
            <span>{{ stat.label }}</span>
            <strong>{{ stat.value }}</strong>
          </article>
        </section>

        <section class="dashboard-section" aria-labelledby="recent-heading">
          <div class="section-heading">
            <div>
              <h2 id="recent-heading">最近更新</h2>
              <p>依照更新時間排序，快速回到正在處理的工單。</p>
            </div>

            <RouterLink class="secondary-link" to="/tickets">
              全部工單
            </RouterLink>
          </div>

          <div class="recent-ticket-list">
            <RouterLink
              v-for="ticket in recentTickets"
              :key="ticket.id"
              class="recent-ticket-row"
              :to="`/tickets/${ticket.id}`"
            >
              <div>
                <strong>{{ ticket.title }}</strong>
                <span>更新於 {{ formatDateTime(ticket.updatedAt) }}</span>
              </div>

              <div class="recent-ticket-badges">
                <StatusBadge :status="ticket.status" />
                <PriorityBadge :priority="ticket.priority" />
              </div>
            </RouterLink>
          </div>
        </section>
      </template>
    </template>
  </section>
</template>
