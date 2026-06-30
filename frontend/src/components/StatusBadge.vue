<script setup lang="ts">
import { computed } from 'vue'
import type { TicketStatus } from '../types/ticket'

const props = defineProps<{
  status: TicketStatus | string
}>()

const statusLabels: Record<TicketStatus, string> = {
  Open: '待處理',
  InProgress: '處理中',
  Done: '已完成',
  Archived: '已封存',
}

const statusClasses: Record<TicketStatus, string> = {
  Open: 'status-badge--open',
  InProgress: 'status-badge--progress',
  Done: 'status-badge--done',
  Archived: 'status-badge--archived',
}

const fallbackClass = 'status-badge--unknown'

const statusLabel = computed(() => statusLabels[props.status as TicketStatus] ?? props.status)
const statusClass = computed(
  () => statusClasses[props.status as TicketStatus] ?? fallbackClass,
)
</script>

<template>
  <span class="badge status-badge" :class="statusClass" :title="statusLabel">
    {{ statusLabel }}
  </span>
</template>

<style scoped>
.status-badge {
  max-width: 100%;
  border: 1px solid transparent;
  background: rgba(148, 163, 184, 0.14);
  color: var(--color-text-muted);
  overflow: hidden;
  text-overflow: ellipsis;
}

.status-badge--open {
  border-color: rgba(76, 141, 255, 0.26);
  background: rgba(76, 141, 255, 0.16);
  color: #bfdbfe;
}

.status-badge--progress {
  border-color: rgba(139, 92, 246, 0.26);
  background: rgba(139, 92, 246, 0.16);
  color: #ddd6fe;
}

.status-badge--done {
  border-color: rgba(52, 211, 153, 0.24);
  background: rgba(52, 211, 153, 0.16);
  color: #bbf7d0;
}

.status-badge--archived {
  border-color: rgba(100, 116, 139, 0.28);
  background: rgba(51, 65, 85, 0.34);
  color: #cbd5e1;
}

.status-badge--unknown {
  border-color: rgba(148, 163, 184, 0.24);
  background: rgba(100, 116, 139, 0.18);
  color: #e2e8f0;
}
</style>
