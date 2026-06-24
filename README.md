# TicketFlow

## 面試展示版重點

TicketFlow 是一個作品集導向的小型全端工單管理系統，重點不是做大而全的客服平台，而是展示如何把一個可理解的產品切片完整交付：前端可操作、後端有 API contract、資料庫可切換、Auth 有作品級基線、部署路徑能用免費方案說清楚。

可以在面試中用這個專案說明：

- 如何用 GitHub Issue 拆分 scope，維持一個 Issue、一個 branch、一個 PR。
- 如何讓 Vue 前端從登入、Dashboard、列表、詳情、建立、編輯一路串到 ASP.NET Core API。
- 如何用 ASP.NET Core minimal API、EF Core、SQLite / PostgreSQL provider 建立可測試的後端。
- 如何自建 register / login / JWT，而不是把 Auth 交給 Supabase Auth。
- 如何用 Netlify、Render、Supabase Free 組成可展示但不宣稱 production SLA 的部署方案。

## 架構說明

### 系統架構

```text
使用者瀏覽器 ──HTTPS──▶ Netlify (Vue3 SPA)
                              │
                              ├── API 呼叫 ──▶ Render (ASP.NET Core Web API)
                              │                    │
                              │                    ├── EF Core ──▶ Supabase PostgreSQL
                              │                    └── JWT Auth
                              │
                              └── 路由(Nginx/Catch-all) ──▶ /index.html
```

前端部署於 Netlify，後端部署於 Render，資料庫使用 Supabase Free PostgreSQL。

### 資料流向

```text
Browser
  -> Netlify Vue 3 SPA (Vite build)
  -> Render ASP.NET Core 8 Web API
  -> Supabase managed PostgreSQL (EF Core 8)
```

本機開發時，前端 Vite dev server 透過 `/api` proxy 呼叫後端；後端預設使用 SQLite。Production 展示時，前端透過 `VITE_API_BASE_URL` 指向 Render API；後端透過環境變數切到 Supabase PostgreSQL，並用 `Cors__AllowedOrigins` 限制允許的 Netlify origin。

## 資料流

1. 使用者在前端註冊或登入。
2. 後端驗證帳密，使用 password hash 儲存密碼，登入成功回傳 JWT 與目前使用者資料。
3. 前端把 token 儲存在 session state/localStorage，呼叫 ticket API 時自動帶 `Authorization: Bearer <token>`。
4. 後端保護 `/api/tickets`，未登入會回 401；`/health` 保持公開供 Render health check 使用。
5. 工單 CRUD 透過 EF Core 寫入資料庫，本機是 SQLite，部署是 Supabase PostgreSQL。

## 測試策略

目前測試分三層：

- 後端 integration tests：涵蓋 health、Auth register/login、重複 email、未登入 ticket API 401、登入後 CRUD、validation error。
- 前端 unit tests：涵蓋 query string、API error message、Auth error message。
- 手動 smoke test：部署後驗證 Render `/health`、Netlify route refresh、線上註冊、登入、登入後 CRUD。

PR 前基線命令：

```bash
dotnet test
cd frontend
npm ci
npm run lint
npm run test
npm run build
```

## Demo 與部署網址

目前 README 不提交真實帳號、密碼、JWT secret、資料庫密碼或 connection string。部署完成後可在此段補上實際網址：

```text
Frontend demo URL: https://ticket-flow-harry-0824.netlify.app/
Backend health URL: <Render URL>/health
Demo account: <部署後手動建立的 demo email>
Demo password: <面試前私下準備，不提交到 repo>
```

建議 demo 帳號在正式展示前手動建立，並只放入非敏感示範資料。

## 技術決策與取捨

- 不使用 Supabase Auth：此作品要展示 ASP.NET Core Auth 基礎能力，因此後端自建 register / login / JWT。
- 不做 refresh token：目前是面試作品級基線，access token 到期後重新登入即可，避免引入 token rotation 與撤銷狀態。
- 不做 OAuth：OAuth 會增加第三方 provider 設定與 callback 維護成本，超出目前 CRUD + Auth 展示重點。
- 不使用付費 DB：免費方案是硬限制，因此使用 Supabase Free PostgreSQL，並在 README 明確標示 quota、inactivity 與 SLA 限制。
- 不保證 production SLA：Netlify / Render / Supabase Free 適合展示，不適合作為正式營運承諾。

## 面試展示流程

1. 先打開首頁 Dashboard，說明工單摘要與最近更新。
2. 展示註冊、登入、登出與 route guard，未登入不能進入工單頁。
3. 建立一筆高優先級工單，回到列表用 status / priority / keyword 篩選。
4. 進入詳情頁，編輯狀態與指派人，再刪除工單。
5. 說明後端 Auth、validation、integration tests，以及 deployment env vars 如何把本機 SQLite 切到 Supabase PostgreSQL。
6. 最後主動說明未做 refresh token、OAuth、email verification 的原因，避免被誤認為遺漏。

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

- `POST /api/auth/register`
  - 註冊新使用者
  - 欄位：`email`、`displayName`、`password`
  - email 唯一，password 使用 hash 儲存
- `POST /api/auth/login`
  - 使用 email / password 登入
  - 成功後回傳 JWT 與目前使用者資料
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

除 `/health` 與 `/api/auth/*` 外，ticket API 需要 JWT。

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

- Docker 設定
- 通知功能
- 檔案上傳
- 報表或分析
- 多租戶或計費流程
- email verification
- refresh token
- OAuth
- 多角色權限

後續可以在核心工單流程穩定後，再逐步加入這些能力。
