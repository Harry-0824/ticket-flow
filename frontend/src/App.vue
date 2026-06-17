<template>
  <div class="app-shell">
    <header class="app-header">
      <RouterLink class="brand" to="/">TicketFlow</RouterLink>
      <nav aria-label="主要導覽">
        <RouterLink to="/">首頁</RouterLink>
        <RouterLink to="/tickets">工單</RouterLink>
      </nav>
    </header>

    <main>
      <RouterView />
    </main>
  </div>
</template>
