<script setup lang="ts">
import { RouterLink } from 'vue-router'
import type { Ticket } from '../types/ticket'
import PriorityBadge from './PriorityBadge.vue'
import StatusBadge from './StatusBadge.vue'

defineProps<{
  tickets: Ticket[]
}>()

const formatDate = (date: string) =>
  new Intl.DateTimeFormat('zh-TW', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(new Date(date))
</script>

<template>
  <div class="ticket-table-wrap">
    <table class="ticket-table">
      <thead>
        <tr>
          <th scope="col">工單</th>
          <th scope="col">狀態</th>
          <th scope="col">優先級</th>
          <th scope="col">負責人</th>
          <th scope="col">更新時間</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="ticket in tickets" :key="ticket.id">
          <td>
            <RouterLink class="ticket-title" :to="`/tickets/${ticket.id}`">
              {{ ticket.title }}
            </RouterLink>
            <div class="ticket-description">{{ ticket.description }}</div>
          </td>
          <td><StatusBadge :status="ticket.status" /></td>
          <td><PriorityBadge :priority="ticket.priority" /></td>
          <td>{{ ticket.assignee || '未指派' }}</td>
          <td>{{ formatDate(ticket.updatedAt) }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
