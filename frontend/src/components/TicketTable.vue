<script setup lang="ts">
import type { Ticket } from '../types/ticket'
import PriorityBadge from './PriorityBadge.vue'
import StatusBadge from './StatusBadge.vue'

defineProps<{
  tickets: Ticket[]
}>()

const formatDate = (date: string) =>
  new Intl.DateTimeFormat('en', {
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
          <th scope="col">Ticket</th>
          <th scope="col">Status</th>
          <th scope="col">Priority</th>
          <th scope="col">Assignee</th>
          <th scope="col">Updated</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="ticket in tickets" :key="ticket.id">
          <td>
            <div class="ticket-title">{{ ticket.title }}</div>
            <div class="ticket-description">{{ ticket.description }}</div>
          </td>
          <td><StatusBadge :status="ticket.status" /></td>
          <td><PriorityBadge :priority="ticket.priority" /></td>
          <td>{{ ticket.assignee || 'Unassigned' }}</td>
          <td>{{ formatDate(ticket.updatedAt) }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
