<script setup lang="ts">
import { reactive, ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import { getAuthApiErrorMessage, register } from '../api/auth'
import { useAppStore } from '../stores/app'

const router = useRouter()
const appStore = useAppStore()

const form = reactive({
  email: '',
  displayName: '',
  password: '',
})
const isSubmitting = ref(false)
const errorMessage = ref('')

const submitRegister = async () => {
  errorMessage.value = ''
  isSubmitting.value = true

  try {
    const session = await register({
      email: form.email.trim(),
      displayName: form.displayName.trim(),
      password: form.password,
    })
    appStore.setSession(session)
    await router.push({ name: 'home' })
  } catch (error) {
    errorMessage.value = getAuthApiErrorMessage(
      error,
      '註冊失敗，請確認欄位後再試。',
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
        <h1>建立 TicketFlow 帳號</h1>
        <p>註冊後即可建立與管理工單。</p>
      </div>

      <form class="ticket-form auth-form" @submit.prevent="submitRegister">
        <label>
          Email
          <input v-model="form.email" type="email" autocomplete="email" required />
        </label>

        <label>
          顯示名稱
          <input v-model="form.displayName" autocomplete="name" required />
        </label>

        <label>
          密碼
          <input
            v-model="form.password"
            type="password"
            autocomplete="new-password"
            minlength="8"
            required
          />
        </label>

        <div v-if="errorMessage" class="form-error" role="alert">
          {{ errorMessage }}
        </div>

        <div class="form-actions">
          <button type="submit" :disabled="isSubmitting">
            {{ isSubmitting ? '建立中...' : '建立帳號' }}
          </button>
          <RouterLink class="secondary-link" to="/login">已有帳號</RouterLink>
        </div>
      </form>
    </div>
  </section>
</template>
