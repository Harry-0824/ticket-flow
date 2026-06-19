<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import { getAuthApiErrorMessage, login } from '../api/auth'
import { useAppStore } from '../stores/app'

const route = useRoute()
const router = useRouter()
const appStore = useAppStore()

const form = reactive({
  email: '',
  password: '',
})
const isSubmitting = ref(false)
const errorMessage = ref('')

const statusMessage = computed(() => {
  if (appStore.authMessage) {
    return appStore.authMessage
  }

  return route.query.expired ? '登入已過期，請重新登入。' : ''
})

const submitLogin = async () => {
  errorMessage.value = ''
  isSubmitting.value = true

  try {
    const session = await login({
      email: form.email.trim(),
      password: form.password,
    })
    appStore.setSession(session)

    const redirect = typeof route.query.redirect === 'string'
      ? route.query.redirect
      : '/'
    await router.push(redirect)
  } catch (error) {
    errorMessage.value = getAuthApiErrorMessage(
      error,
      '登入失敗，請確認帳號密碼後再試。',
    )
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <section class="auth-page">
    <div class="auth-panel">
      <div class="auth-heading">
        <h1>登入 TicketFlow</h1>
        <p>使用帳號進入工單工作區。</p>
      </div>

      <div v-if="statusMessage" class="form-notice" role="status">
        {{ statusMessage }}
      </div>

      <form class="ticket-form auth-form" @submit.prevent="submitLogin">
        <label>
          Email
          <input v-model="form.email" type="email" autocomplete="email" required />
        </label>

        <label>
          密碼
          <input
            v-model="form.password"
            type="password"
            autocomplete="current-password"
            required
          />
        </label>

        <div v-if="errorMessage" class="form-error" role="alert">
          {{ errorMessage }}
        </div>

        <div class="form-actions">
          <button type="submit" :disabled="isSubmitting">
            {{ isSubmitting ? '登入中...' : '登入' }}
          </button>
          <RouterLink class="secondary-link" to="/register">建立帳號</RouterLink>
        </div>
      </form>
    </div>
  </section>
</template>
