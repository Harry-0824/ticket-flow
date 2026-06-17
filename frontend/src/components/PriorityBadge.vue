<script setup lang="ts">
import type { TicketPriority } from '../types/ticket'

defineProps<{
  priority: TicketPriority
}>()

const priorityLabels: Record<TicketPriority, string> = {
  Low: '低',
  Medium: '中',
  High: '高',
}
</script>

<template>
  <span class="badge priority-badge" :data-priority="priority">
    {{ priorityLabels[priority] }}
  </span>
</template>
