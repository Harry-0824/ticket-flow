<template>
  <div class="app-shell">
    <header class="app-header">
      <RouterLink class="brand" to="/">TicketFlow</RouterLink>
      <nav aria-label="Primary navigation">
        <RouterLink to="/">Home</RouterLink>
        <RouterLink to="/tickets">Tickets</RouterLink>
      </nav>
    </header>

    <main>
      <RouterView />
    </main>
  </div>
</template>
