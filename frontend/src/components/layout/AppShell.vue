<script setup lang="ts">
import SidebarNav from './SidebarNav.vue'

defineProps<{
  userName?: string
}>()

defineEmits<{
  logout: []
}>()

const healthItems = [
  { label: 'API Online', value: 'Connected', tone: 'healthy' },
  { label: 'Active tickets', value: 'Live queue', tone: 'neutral' },
  { label: 'Avg response', value: '< 200ms', tone: 'neutral' },
] as const
</script>

<template>
  <div class="workspace-shell">
    <aside class="workspace-sidebar">
      <SidebarNav
        :user-name="userName"
        :health-items="healthItems"
        @logout="$emit('logout')"
      />
    </aside>

    <section class="workspace-content">
      <div class="workspace-main">
        <slot />
      </div>
    </section>
  </div>
</template>
