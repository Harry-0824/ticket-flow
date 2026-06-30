<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue'
import { RouterLink, RouterView, useRoute, useRouter } from 'vue-router'
import { AUTH_UNAUTHORIZED_EVENT } from './api/session'
import AppShell from './components/layout/AppShell.vue'
import { useAppStore } from './stores/app'

const appStore = useAppStore()
const route = useRoute()
const router = useRouter()

const logout = async () => {
  appStore.clearSession()
  await router.push({ name: 'login' })
}

const handleUnauthorized = async () => {
  appStore.clearSession('登入已過期，請重新登入。')

  if (route.name !== 'login' && route.name !== 'register') {
    await router.push({ name: 'login', query: { expired: '1' } })
  }
}

onMounted(() => {
  window.addEventListener(AUTH_UNAUTHORIZED_EVENT, handleUnauthorized)
})

onUnmounted(() => {
  window.removeEventListener(AUTH_UNAUTHORIZED_EVENT, handleUnauthorized)
})
</script>

<template>
  <AppShell
    v-if="appStore.isAuthenticated"
    :user-name="appStore.currentUser?.displayName"
    @logout="logout"
  >
    <RouterView />
  </AppShell>

  <div v-else class="app-shell guest-shell">
    <header class="app-header">
      <RouterLink class="brand" to="/">TicketFlow</RouterLink>

      <nav aria-label="主要導覽">
        <RouterLink to="/login">登入</RouterLink>
        <RouterLink to="/register">註冊</RouterLink>
      </nav>
    </header>

    <main class="guest-main">
      <RouterView />
    </main>
  </div>
</template>
