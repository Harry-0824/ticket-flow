<script setup lang="ts">
import TicketTable from '../components/TicketTable.vue'
import { mockTickets } from '../data/mockTickets'
</script>

<template>
  <section class="page">
    <h1>Tickets</h1>
    <p>Review support tickets from the current mock dataset.</p>

    <TicketTable :tickets="mockTickets" />
  </section>
</template>
