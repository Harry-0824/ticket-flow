# TicketFlow

TicketFlow 是一個作品集導向的工單管理專案，規劃使用 Vue 3 前端與 ASP.NET Core Web API 後端。

## 專案目標

建立一個小型、可審查、範圍清楚的 MVP，展示自由接案與作品集情境中的基本客服工單流程。第一版實作應專注在工單建立、清單瀏覽、詳細內容檢視與狀態更新。

## 規劃技術棧

- 前端：Vue 3
- 後端：ASP.NET Core Web API
- 資料庫：SQL Server 或 SQLite，於後續實作階段決定
- API 形式：REST JSON endpoints

## MVP 範圍

- 建立包含標題、描述、狀態、優先程度與請求人姓名的工單。
- 查看工單清單。
- 查看單一工單詳細頁。
- 更新工單狀態。
- 保持資料模型與介面簡單明確。

## 不在範圍內

MVP 明確排除：

- 身分驗證
- 使用者角色或權限
- Docker
- CI/CD
- 通知功能
- 檔案上傳
- 正式 production 部署
- 進階搜尋、報表或分析
- 多租戶或計費功能

## 規劃中的 Repository 結構

```text
/frontend
  Vue 3 application

/backend
  ASP.NET Core Web API application
```

以上結構僅為規劃。應由後續明確指定的實作 Issue 建立應用程式資料夾。

## 規劃中的 API Routes

- `GET /api/tickets` - 取得工單清單
- `GET /api/tickets/{id}` - 取得工單詳細內容
- `POST /api/tickets` - 建立工單
- `PATCH /api/tickets/{id}/status` - 更新工單狀態

## 規劃中的前端 Routes

- `/` - 工單清單
- `/tickets/new` - 建立工單
- `/tickets/:id` - 工單詳細頁

## 本機設定 Placeholder

本機設定說明會在前端與後端專案實際建立後補上。本次 planning step 不包含任何應用程式程式碼或 package 檔案。

## MVP 限制

- MVP 不是 production-ready 版本。
- 安全性、身分驗證、部署與營運相關事項會刻意延後處理。
- 第一版應優先呈現可讀性高的程式碼與清楚的作品集價值，而不是完整企業級流程覆蓋。

## 作品集定位

TicketFlow 用來展示端到端產品思考、API 設計、前後端整合，以及在小型工單管理系統中維持紀律化範圍控制的能力。
