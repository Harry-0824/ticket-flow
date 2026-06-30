<script setup lang="ts">
import { computed } from 'vue'
import type { TicketPriority } from '../types/ticket'

const props = defineProps<{
  priority: TicketPriority | string
}>()

const priorityLabels: Record<TicketPriority, string> = {
  Low: '低',
  Medium: '中',
  High: '高',
}

const priorityClasses: Record<TicketPriority, string> = {
  Low: 'priority-badge--low',
  Medium: 'priority-badge--medium',
  High: 'priority-badge--high',
}

const fallbackClass = 'priority-badge--unknown'

const priorityLabel = computed(
  () => priorityLabels[props.priority as TicketPriority] ?? props.priority,
)
const priorityClass = computed(
  () => priorityClasses[props.priority as TicketPriority] ?? fallbackClass,
)
</script>

<template>
  <span class="badge priority-badge" :class="priorityClass" :title="priorityLabel">
    {{ priorityLabel }}
  </span>
</template>

<style scoped>
.priority-badge {
  max-width: 100%;
  border: 1px solid transparent;
  background: rgba(148, 163, 184, 0.14);
  color: var(--color-text-muted);
  overflow: hidden;
  text-overflow: ellipsis;
}

.priority-badge--high {
  border-color: rgba(249, 115, 22, 0.28);
  background: rgba(249, 115, 22, 0.16);
  color: #fdba74;
}

.priority-badge--medium {
  border-color: rgba(251, 191, 36, 0.28);
  background: rgba(251, 191, 36, 0.14);
  color: #fde68a;
}

.priority-badge--low {
  border-color: rgba(34, 197, 94, 0.24);
  background: rgba(34, 197, 94, 0.14);
  color: #bbf7d0;
}

.priority-badge--unknown {
  border-color: rgba(148, 163, 184, 0.24);
  background: rgba(100, 116, 139, 0.18);
  color: #e2e8f0;
}
</style>
