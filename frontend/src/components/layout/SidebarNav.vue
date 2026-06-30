<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink, useRoute } from 'vue-router'

type HealthItem = {
  label: string
  value: string
  tone: 'healthy' | 'neutral'
}

const props = defineProps<{
  healthItems: readonly HealthItem[]
  userName?: string
}>()

defineEmits<{
  logout: []
}>()

const route = useRoute()

const navItems = computed(() => [
  {
    label: 'Dashboard',
    to: '/',
    isActive: route.name === 'home',
  },
  {
    label: 'Tickets',
    to: '/tickets',
    isActive: ['tickets', 'create-ticket', 'ticket-detail', 'edit-ticket'].includes(
      String(route.name ?? ''),
    ),
  },
  {
    label: 'Workflow',
    isActive: false,
  },
  {
    label: 'Reports',
    isActive: false,
  },
  {
    label: 'Settings',
    isActive: false,
  },
])
</script>

<template>
  <div class="sidebar-nav">
    <div class="sidebar-brand-block">
      <RouterLink class="sidebar-brand" to="/">TicketFlow</RouterLink>
      <p class="sidebar-brand-copy">
        Support queue workspace for dashboard, triage, and response flow.
      </p>
    </div>

    <nav class="sidebar-links" aria-label="工作台側邊導覽">
      <component
        :is="item.to ? RouterLink : 'span'"
        v-for="item in navItems"
        :key="item.label"
        :to="item.to"
        class="sidebar-link"
        :class="{
          'sidebar-link--active': item.isActive,
          'sidebar-link--placeholder': !item.to,
        }"
        :aria-disabled="item.to ? undefined : 'true'"
      >
        <span class="sidebar-link-label">{{ item.label }}</span>
        <span v-if="!item.to" class="sidebar-link-pill">Soon</span>
      </component>
    </nav>

    <section class="sidebar-health-card" aria-label="System Health">
      <div class="sidebar-section-heading">
        <strong>System Health</strong>
        <span>Shell preview</span>
      </div>

      <div class="sidebar-health-list">
        <article
          v-for="item in props.healthItems"
          :key="item.label"
          class="sidebar-health-item"
        >
          <span class="sidebar-health-label">
            <span
              class="sidebar-health-dot"
              :class="{ 'sidebar-health-dot--healthy': item.tone === 'healthy' }"
            />
            {{ item.label }}
          </span>
          <strong>{{ item.value }}</strong>
        </article>
      </div>
    </section>

    <div class="sidebar-session">
      <div>
        <span class="sidebar-session-label">Signed in</span>
        <strong>{{ props.userName || 'TicketFlow Operator' }}</strong>
      </div>

      <button type="button" class="sidebar-logout" @click="$emit('logout')">登出</button>
    </div>
  </div>
</template>
