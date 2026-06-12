<script setup lang="ts">
import type { TicketPriority } from '../types/ticket'

defineProps<{
  priority: TicketPriority
}>()
</script>

<template>
  <span class="badge priority-badge" :data-priority="priority">
    {{ priority }}
  </span>
</template>
