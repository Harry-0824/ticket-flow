// Netlify 正式環境會設定 VITE_API_BASE_URL 指向 Render；本機開發則交給 Vite proxy 轉送 /api。
export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api'
