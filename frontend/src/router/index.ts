import { createRouter, createWebHistory } from 'vue-router'
import CreateTicketView from '../views/CreateTicketView.vue'
import EditTicketView from '../views/EditTicketView.vue'
import HomeView from '../views/HomeView.vue'
import LoginView from '../views/LoginView.vue'
import RegisterView from '../views/RegisterView.vue'
import TicketDetailView from '../views/TicketDetailView.vue'
import TicketsView from '../views/TicketsView.vue'
import { useAppStore } from '../stores/app'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
      meta: { requiresAuth: true },
    },
    {
      path: '/login',
      name: 'login',
      component: LoginView,
      meta: { guestOnly: true },
    },
    {
      path: '/register',
      name: 'register',
      component: RegisterView,
      meta: { guestOnly: true },
    },
    {
      path: '/tickets',
      name: 'tickets',
      component: TicketsView,
      meta: { requiresAuth: true },
    },
    {
      path: '/tickets/new',
      name: 'create-ticket',
      component: CreateTicketView,
      meta: { requiresAuth: true },
    },
    {
      path: '/tickets/:id',
      name: 'ticket-detail',
      component: TicketDetailView,
      meta: { requiresAuth: true },
    },
    {
      path: '/tickets/:id/edit',
      name: 'edit-ticket',
      component: EditTicketView,
      meta: { requiresAuth: true },
    },
  ],
})

router.beforeEach((to) => {
  const appStore = useAppStore()

  // route meta 是前端第一層保護；真正的權限仍由後端 JWT 驗證，兩邊一起避免未登入操作工單。
  if (to.meta.requiresAuth && !appStore.isAuthenticated) {
    return {
      name: 'login',
      query: { redirect: to.fullPath },
    }
  }

  // 已登入使用者不需要再進登入/註冊頁，避免覆蓋目前有效 session。
  if (to.meta.guestOnly && appStore.isAuthenticated) {
    return { name: 'home' }
  }
})
