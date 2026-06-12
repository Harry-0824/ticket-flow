<script setup lang="ts">
import type { TicketStatus } from '../types/ticket'

defineProps<{
  status: TicketStatus
}>()
</script>

<template>
  <span class="badge status-badge" :data-status="status">
    {{ status }}
  </span>
</template>
