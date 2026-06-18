<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue'
import { RouterLink, RouterView, useRoute, useRouter } from 'vue-router'
import { AUTH_UNAUTHORIZED_EVENT } from './api/session'
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
  <div class="app-shell">
    <header class="app-header">
      <RouterLink class="brand" to="/">TicketFlow</RouterLink>

      <nav aria-label="主要導覽">
        <template v-if="appStore.isAuthenticated">
          <RouterLink to="/">首頁</RouterLink>
          <RouterLink to="/tickets">工單</RouterLink>
        </template>
        <template v-else>
          <RouterLink to="/login">登入</RouterLink>
          <RouterLink to="/register">註冊</RouterLink>
        </template>
      </nav>

      <div v-if="appStore.isAuthenticated" class="session-actions">
        <span>{{ appStore.currentUser?.displayName }}</span>
        <button type="button" @click="logout">登出</button>
      </div>
    </header>

    <main>
      <RouterView />
    </main>
  </div>
</template>
