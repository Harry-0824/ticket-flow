<script setup lang="ts">
import type { TicketStatus } from '../types/ticket'

defineProps<{
  status: TicketStatus
}>()

const statusLabels: Record<TicketStatus, string> = {
  Open: '待處理',
  InProgress: '處理中',
  Done: '已完成',
  Archived: '已封存',
}
</script>

<template>
  <span class="badge status-badge" :data-status="status">
    {{ statusLabels[status] }}
  </span>
</template>
