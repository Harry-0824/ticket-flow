import { ref } from 'vue'
import {
  getAuthApiErrorMessage,
  login as loginRequest,
  register as registerRequest,
} from '../api/auth'
import type { LoginInput, RegisterInput } from '../api/auth'

const loginErrorMessage = '登入失敗，請確認帳號密碼後再試。'
const registerErrorMessage = '註冊失敗，請確認資料後再試。'

export type { LoginInput, RegisterInput }

export const useAuth = () => {
  const errorMessage = ref('')

  const login = async (input: LoginInput) => {
    errorMessage.value = ''

    try {
      return await loginRequest(input)
    } catch (error) {
      errorMessage.value = getAuthApiErrorMessage(error, loginErrorMessage)
      return undefined
    }
  }

  const register = async (input: RegisterInput) => {
    errorMessage.value = ''

    try {
      return await registerRequest(input)
    } catch (error) {
      errorMessage.value = getAuthApiErrorMessage(error, registerErrorMessage)
      return undefined
    }
  }

  return {
    errorMessage,
    login,
    register,
  }
}
