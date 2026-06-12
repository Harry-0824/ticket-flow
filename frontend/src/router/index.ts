import { createRouter, createWebHistory } from 'vue-router'
import CreateTicketView from '../views/CreateTicketView.vue'
import HomeView from '../views/HomeView.vue'
import TicketDetailView from '../views/TicketDetailView.vue'
import TicketsView from '../views/TicketsView.vue'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
    },
    {
      path: '/tickets',
      name: 'tickets',
      component: TicketsView,
    },
    {
      path: '/tickets/new',
      name: 'create-ticket',
      component: CreateTicketView,
    },
    {
      path: '/tickets/:id',
      name: 'ticket-detail',
      component: TicketDetailView,
    },
  ],
})
