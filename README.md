# TicketFlow

## 免費部署設定

TicketFlow 的展示部署以免費方案為硬限制：

- Frontend：Netlify Free，設定檔為 `netlify.toml`
- Backend：Render Free Web Service，設定檔為 `render.yaml`
- Database：Supabase Free managed PostgreSQL
- Auth：ASP.NET Core 自建 register / login / JWT，不使用 Supabase Auth

Netlify build 設定：

```bash
base = frontend
build command = npm ci && npm run build
publish directory = dist
```

`netlify.toml` 已設定 SPA redirect，重新整理 `/login`、`/register`、`/tickets/:id` 這類前端 route 不會回 404。Netlify 環境變數需要設定：

```bash
VITE_API_BASE_URL=https://<your-render-service>.onrender.com/api
```

Render backend 需要設定：

```bash
ASPNETCORE_ENVIRONMENT=Production
Database__Provider=PostgreSQL
ConnectionStrings__TicketFlowPostgres=<Supabase Session Pooler PostgreSQL connection string>
Jwt__Issuer=TicketFlow
Jwt__Audience=TicketFlowClient
Jwt__Secret=<至少 32 bytes 的隨機字串>
Jwt__ExpiresMinutes=60
Cors__AllowedOrigins=https://<your-netlify-site>.netlify.app
```

Supabase 只作為 PostgreSQL database provider。PostgreSQL schema SQL 放在 `backend/Migrations/Postgres/`，不要把 SQLite migration 直接套到 Supabase。

免費方案限制：

- Render Free 可能 cold start，第一次開 API 會比較慢。
- Supabase Free 有配額與 inactivity 限制，長時間未使用可能暫停。
- Netlify / Render / Supabase Free 都不保證 production SLA。
- 本專案部署目標是面試展示，不是正式商用 production hosting。

部署後 smoke test：

- Render `/health` 回 `Healthy`
- Netlify route refresh 不會 404
- 線上註冊成功
- 線上登入成功
- 登入後工單 CRUD 成功

TicketFlow 是一個作品集導向的小型全端工單管理 MVP。它展示一個聚焦的 CRUD 流程：使用者可以透過 Vue 前端查看、篩選、建立、編輯與刪除客服工單，資料則由 ASP.NET Core Web API 提供。

這個專案刻意維持 MVP 範圍，重點放在清楚的產品邊界、可讀性高的實作，以及前端、後端、資料庫之間的端到端資料流。

## 技術棧

- 前端：Vue 3、TypeScript、Vue Router、Vite
- 後端：ASP.NET Core Web API
- 資料存取：EF Core
- 資料庫：本機 SQLite，production 可切換 Supabase managed PostgreSQL
- API 形式：REST JSON endpoints

## 已完成的 MVP 功能

- 從後端 API 載入工單清單
- 使用後端支援的 `status`、`priority`、`keyword` query params 篩選工單
- 依照工單 id 載入工單詳細頁
- 建立工單表單
- 編輯工單表單
- 帶有確認步驟的刪除工單操作
- 主要工單流程中的 loading、error、empty 與 not found 狀態

## 資料流

前端透過 `frontend/src/api/tickets.ts` 內的 API service 呼叫後端。這層 service 會把 Vue view 的操作轉成 HTTP request，送到 ASP.NET Core 後端的 `/api/tickets` API。

後端使用 minimal API endpoints 處理工單 CRUD，透過 EF Core 存取資料。本機開發預設使用 SQLite，production 可透過環境變數切換到 Supabase managed PostgreSQL。資料會以 JSON 回傳給前端，再由 Vue routes、views、badge 元件與 table 元件呈現。

## 架構概覽

```text
frontend/
  Vue 3 app、routes、views、components、API client

backend/
  ASP.NET Core Web API、EF Core DbContext、models、migrations
```

目前前端狀態主要保留在各自頁面中，符合 MVP 的簡單需求。後端 API 維持在目前工單模型上：title、description、status、priority、assignee、createdAt、updatedAt。

## API Contract 摘要

- `GET /api/tickets`
  - 取得工單清單
  - 支援 `status`、`priority`、`keyword` query params
- `GET /api/tickets/{id}`
  - 依照 id 取得單一工單
- `POST /api/tickets`
  - 建立工單
- `PUT /api/tickets/{id}`
  - 更新工單
- `DELETE /api/tickets/{id}`
  - 刪除工單

## 本機開發

啟動後端：

```bash
cd backend
dotnet restore
dotnet run
```

本機開發預設使用 `ConnectionStrings:TicketFlow` 的 SQLite connection string，不需要 Supabase 帳號。

production 若要使用 Supabase managed PostgreSQL，請在部署平台設定環境變數，不要提交真實密碼或 connection string：

```bash
ASPNETCORE_ENVIRONMENT=Production
Database__Provider=PostgreSQL
ConnectionStrings__TicketFlowPostgres=<Supabase Session Pooler PostgreSQL connection string>
```

Supabase connection string 請從 Supabase Dashboard 的 Connect 設定取得；部署到 Render 這類外部主機時，優先使用 Session Pooler / Supavisor 連線字串。PostgreSQL 初始 schema SQL 放在 `backend/Migrations/Postgres/20260619000000_initial_create_postgres.sql`，不要把既有 SQLite migration 直接套到 PostgreSQL。

啟動前端：

```bash
cd frontend
npm install
npm run dev
```

建置前端：

```bash
cd frontend
npm run build
```

## 作品集與面試故事

TicketFlow 展示如何把一個產品切片，從規劃推進到可操作的全端 MVP。開發過程以小型 GitHub Issues 為單位，每個 Issue 都有明確 scope、驗證方式與對應 pull request。

這個專案可以用來說明：

- 前後端 API contract 對齊
- 從 mock data 轉向真實 API 整合
- 逐步交付 CRUD workflow
- Vue route、view、component 的簡單邊界
- loading、error、not found 等基本產品狀態處理
- 以 Issue 為單位維持實作範圍紀律

## 目前不在範圍內

目前 MVP 尚未實作以下能力：

- 身分驗證或角色權限
- Docker 設定
- 部署設定
- CI 設定
- 通知功能
- 檔案上傳
- 報表或分析
- 多租戶或計費流程

後續可以在核心工單流程穩定後，再逐步加入這些能力。
