# 🧰 04. .NET テンプレート調査（`D:\GitHubTemplate`）

> 📅 調査日: 2026-10-31
> 🔍 方法: 索引（`tmpl-readme.md`）・ガイド（`tmpl-guide.md`）・状態（`tmpl-status.md`）・各計画書と、各テンプレートの csproj・`Program.cs`・`ApplicationExtensions.cs` などを読んだ。主要な主張は抜き取りで再確認済み。**ビルド・実行・git 操作はしていない**
> 🏷️ 「【推測】」と書いたもの以外は、ファイルで確認した事実

---

## ⚡ 結論（TL;DR）

| # | ポイント |
|:-:|---|
| 🏁 | **ベースは `template-blazor-server`**。足りない部品（外部向けの Hub・時系列の保存・Bearer / API キー）は、**同じ構造**（規則 A〜K）で作られた他のテンプレートから移植する |
| 🧱 | Web 系テンプレートはすべて **net10.0・C# preview・Minimal API・SQLite・Serilog・OpenTelemetry・Aspire 13.6.1** |
| 🗄️ | データアクセスは **EF Core ではなく `Smart.Data.Accessor`**（ソース生成 + SQL ファイル）。EF Core は Identity の保存だけ |
| ⚡ | **SignalR Hub + 常駐ワーカー + 時系列の保存**まで揃っているのは `template-maui-server` だけ（HA に最も近い） |
| 🐳 | **Docker はテンプレートの対象外**（確定方針）。代わりに全 Web ホストが **systemd / Windows サービス**に対応 |
| 🧩 | 実行時のプラグイン読み込みは無い。近いのは「DI の多重登録」と「生成器 + ModuleInitializer での登録」 |
| 📭 | **`template-web-home` は空のフォルダー**（2026-10-30 作成）。実装はこの `incubator-home` に置くと決めたため使わない |

---

## 🗺️ 1. テンプレートの一覧

| テンプレート | 用途 | UI | DB | 認証 | ⚡ リアルタイム | ⚙️ 常駐ワーカー | 📋 カタログ |
|---|---|---|---|---|---|---|---|
| 🟢 **blazor-server** | Blazor Server の業務アプリ | MudBlazor 9.11 | SQLite | 自前の Cookie | 回線へのバス | 1 本（見本） | In progress |
| 🟣 **blazor-wasm** | Hosted WASM + API | FluentUI 5.0.1 | SQLite | 自前の JWT + リフレッシュ | なし | なし | In progress |
| 🔵 **web-api** | Minimal API の基準形 | — | SQLite | JWT + API キー | なし | なし | ✅ **Ready** |
| 🟠 **web-fullstack** | Aspire で PostgreSQL / Valkey / YARP を束ねる見本 | なし（未実装） | PostgreSQL | JWT + API キー | なし | なし | In progress |
| ⚪ **web-home** | **空のフォルダー** | — | — | — | — | — | — |
| 🟡 **maui-server** | MAUI 端末の対向サーバー | MudBlazor 9.11 | SQLite + 端末ごとの時系列 SQLite | 見本の JWT（管理画面は認証なし） | ⭐ **SignalR Hub 2 本** + gRPC | ⭐ **4 本** | In progress |
| 🟢 blazor-server-identity | blazor-server の Identity 変種 | MudBlazor | SQLite + Identity（EF Core） | Identity + **パスキー** | 回線へのバス | 1 本 | In progress |
| 🟣 blazor-wasm-oidc | blazor-wasm の OIDC 変種 | FluentUI 5 | SQLite | OIDC（Keycloak） | なし | なし | In progress |
| ⏰ worker | cron ジョブ / バッチ | — | — | — | — | JobScheduler | In progress |
| 📡 grpc-server | gRPC サーバー | — | SQLite | JWT | gRPC のサーバー配信 | 1 本 | In progress |
| 🧬 library-with-generator | 生成器つき NuGet ライブラリ | — | — | — | — | — | ✅ **Ready** |

そのほか:

- 🌐 `D:\GitHubTemplate\template` … テンプレート群の紹介サイト（GitHub Pages）。各テンプレートの Ready / In progress を表示
- 📭 `_template-architecture` … README 1 行と LICENSE などだけ。ガイドが参照する設計文書の実体は見つからなかった
- 📚 `__Refs` … 参考実装。`ref-hv-monitor` に「HTTP ポーリングのワーカー → バス → Blazor Server の画面」の実例がある

---

## 📏 2. テンプレート共通の規約（Web 系）

> 💡 自作版もこの規約に**乗る**のが前提。HA を真似る部分と、テンプレートの流儀に合わせる部分を分けて考える。

### 🏗️ 2-1. リポジトリとビルド

- 📁 `src/` と `tests/` の 2 階層。ソリューションは **`.slnx`**
- 📌 **CPM（`Directory.Packages.props`）は使わない**。版は各 csproj に固定で書く（浮動の版は禁止）
- ⚙️ `Directory.Build.props` は 7 本で同一: `LangVersion=preview`・Nullable（警告をエラーに）・`AnalysisMode=All`・StyleCop・日本語コメントのアナライザー
- ✅ 完了条件は **警告 0・ReSharper 0**。各プロジェクトに `GlobalUsing.cs` / `Assembly.cs` / `GlobalSuppressions.cs` を置く

### 🧱 2-2. 命名と層

- 🏷️ プロジェクト名は `Template.<ベース名>.Host` / `.Core`。「Shared」「Common」という名前は禁止
- 🧅 層は **Core ← Host の 2 層**。Core は `Accessors / Services / Models / Infrastructure / Domain` で、**ASP.NET に依存しない**（DI と Logging の抽象と Smart.Data.Accessor だけ）
- 🚫 **Mediator・CQRS・Result 型は使わない**。Endpoint や Page から Service を直接呼ぶ。結果は列挙型（`DataWriteStatus` など）
- 📂 Host のフォルダー規則（規則 A〜K）:

| フォルダー | 置くもの |
|---|---|
| `Application/` | DI 登録の `ApplicationExtensions`・`EndpointExtensions`・`Log`・`Policies` など |
| `Application/{Telemetry, HealthChecks, Authentication, ExceptionHandling, Circuits, Context}` | 計測・ヘルスチェック・トークン発行・例外処理・回線の追跡・操作コンテキスト |
| `Infrastructure/` | 他に依存しない、持ち出せる部品だけ |
| `Endpoints/` | Minimal API（DTO はファイルの先頭） |
| `Workers/` | 常駐処理 |
| `Settings/` | パイプラインの設定 |

### 🔧 2-3. 実装の流儀

| 項目 | 規約 |
|---|---|
| 📜 `Program.cs` | **目次として書く**（`builder.ConfigureXxx()` / `app.UseXxx()` を並べるだけ。実体は `ApplicationExtensions.cs`） |
| 💉 DI | コンストラクター注入（プライマリコンストラクターは使わない）・既定は **Singleton**・登録順は Setting → Provider → Component → Service → Worker。`*Service` は生成器（BunnyTail.ServiceRegistration）が登録 |
| 🧭 操作コンテキスト | `ServiceContextProvider.Current`。境界ごとにスコープを開く（API = EndpointFilter、Blazor = `AppPageBase`、SignalR = HubFilter、ワーカー = `Begin(...)`） |
| ⚙️ 設定 | `*Option` と `Settings/*Setting`。どちらも `ValidateDataAnnotations().ValidateOnStart()` |
| 📝 ログ | Serilog（appsettings で設定）+ 名前空間ごとの `Log.cs` に `[LoggerMessage]`。文字列補間のログは禁止 |
| 🔭 テレメトリ | OpenTelemetry 1.20.0。OTLP は環境変数があるときだけ。Prometheus は既定で `0.0.0.0:9464`。`/health` と `/alive` |
| 🛡️ セキュリティ | 自前の `SecurityHeadersMiddleware`（画面ありは CSP の nonce も） |
| 🎨 Blazor | InteractiveServer（`prerender:false`）・`@if` / `@foreach` の代わりに Smart.Blazor の `Condition` / `ListItem`・`*.razor.css` と `style=` は禁止（`app.css` に集める）・日本語固定・**アプリ全体の UI 状態ストアは持たない** |
| 🧪 テスト | xunit.v3 + Microsoft Testing Platform（**`dotnet run --project <Tests>`** で実行）・bUnit・NSubstitute・WebApplicationFactory・Playwright |

### 🚫 2-4. テンプレート群として採用しないもの（派生アプリが従う義務は無い）

Http.Resilience・OutputCache / HybridCache・JSON ソース生成・Native AOT・SSE・**コンテナ / PWA / CI**、CancellationToken の伝播は一覧と出力の読み取りだけ

> ⚠️ **自作版で見直すべき点**: デバイスとの通信が中心なので、**Http.Resilience（再試行）** と **CancellationToken の伝播**（停止・オートメーションの中断に必須）は採用するのが妥当【推測】。

---

## 🔎 3. 候補テンプレートの詳細

### 🟢 3-1. template-blazor-server（ベース候補 No.1）

```text
template-blazor-server/
├── src/
│   ├── Template.BlazorServer.AppHost/   🚀 Aspire（Host を登録し、ヘルスチェックだけ）
│   ├── Template.BlazorServer.Core/      🧠 Accessors(+Sql/*.sql) / Services / Models / Infrastructure / Domain
│   └── Template.BlazorServer.Host/      🖥️ Application / Components / Endpoints / Infrastructure / Settings / Workers / Assets/Data/Schema.sql
└── tests/  UnitTests(bUnit) / IntegrationTests / E2ETests(Playwright)
```

| 観点 | 内容 |
|---|---|
| 📦 主なパッケージ | MudBlazor 9.11.0 / FluentValidation 12.2.0 / Usa.Smart.Blazor 2.5.0 / Smart.Data.Accessor 3.0.0-beta16 / Microsoft.Data.Sqlite 10.0.13 / Serilog.AspNetCore 10.0.0 / OpenTelemetry 1.20.0 / NSwag（OpenAPI の UI）/ FeatureManagement / Hosting.Systemd・WindowsServices |
| 🔐 認証 | 自前の **Cookie 認証**（Account テーブル + PBKDF2 31 万回）。ログインは静的 SSR のフォームから `POST /auth/login`。`/api` は未認証なら 401。ロールは Administrator だけ。**Bearer・API キーは無い** |
| ⚡ リアルタイム | `Workers/NotificationWorker`（`PeriodicTimer`）→ `NotificationBus`（Singleton の event）→ 画面が購読して `InvokeAsync(StateHasChanged)`。`CircuitTracker` で接続中の回線を追跡。再接続は .NET 10 の ReconnectModal + `reconnect.js` |
| 🌐 API | Minimal API（`/api/data` など）。**画面は API を通さず Service を直接呼ぶ** |
| 🗄️ データ | SQLite（`data.db`）。起動時に `Schema.sql`（`CREATE TABLE IF NOT EXISTS`）を流す。**マイグレーションの履歴は無い** |
| 🚢 配置 | Docker なし。systemd / Windows サービス。待ち受けは 8080 |
| 📋 成熟度 | In progress。残っている共通アイデアに「SignalR のリアルタイム通知（P-3）」「権限ベースの認可（H-5）」 |

> 💡 **HA との相性**: 「ワーカーが状態を変える → バス → 画面を描き直す」の型がすでにある。これは HA の `state_changed` → フロントエンドの流れそのもの。

### 🟡 3-2. template-maui-server（部品の移植元 No.1）

MAUI 端末の対向サーバーだが、**HA に最も近い機能**を持つ。2026-09-22 の再構成で Web 系の「最新構造」の手本になった（規則 A〜K の出どころ）。

| 部品 | 内容 | HA で言うと |
|---|---|---|
| 📡 `Hubs/MonitorHub` | 型付き Hub。サーバー → 端末へ状態を配信、端末 → サーバーへ報告 | WebSocket API |
| 🔔 `Hubs/PushHub` + `PushRetentionWorker` | 端末ごとのグループ、未達の通知を DB に溜めて再送、既読の確認 | notify |
| 🚌 `Telemetry/TelemetryBus` | 購読者ごとに例外を隔離して呼び出す | EventBus |
| ⏱️ `Components/RefreshTimer` | 通知を 1 秒単位でまとめて描き直す（間引き） | フロントの描画の最適化 |
| 💾 `TelemetrySchema.sql` | `MetricSeries`（系列のメタ）+ `MetricPoint`（`PRIMARY KEY(SeriesId, TimeUnixNano) WITHOUT ROWID`）、WAL、区間ごとの集計 SQL | **Recorder**（`states_meta` + `states`） |
| 🧹 `TelemetryRetentionWorker` | 保持期間を過ぎたデータの削除（60 分ごと） | purge |
| 📈 `TimeSeriesChart` / `Sparkline` | **依存ゼロの SVG** チャート | 履歴グラフ |
| 🔀 その他 | Kestrel の多ポート化（`RequirePort`）、ForwardedHeaders を設定から読む | http |

> ⚠️ 管理画面に認証が無く、JWT は見本だけ。MAUI のセッションが担当しているテンプレートなので、**ベースにはせず部品を持ってくる**のがよい。

### 🟣 3-3. template-blazor-wasm

```text
src/  Domain(BCLのみ) / Contracts(DTO の record を共有) / Backend.Core / Backend.Host / Frontend.App / AppHost
```

- 🔐 自前の JWT。**リフレッシュトークンは乱数 32 バイトを返し、DB には SHA-256 だけを保存し、1 回使うと入れ替える** → 長期トークンの設計に流用できる
- 🗝️ トークンはブラウザの sessionStorage（タブを閉じるとログアウト。XSS 対策は未着手）
- 🧰 NSwag で型付きの API クライアントを生成（ビルドの前に作り直す）
- ❌ リアルタイムも常駐ワーカーも無い

### 🔵 3-4. template-web-api（✅ Ready）

- 🔐 **JWT Bearer と API キーの 2 スキーム**（既定のポリシーが両方を受け付ける）。利用者は appsettings、API キーは 1 本
- 🌐 URL の版（`/api/v1/...`）・版ごとの OpenAPI・ETag による楽観ロック・RateLimiter（ログイン専用のポリシーあり）・圧縮
- ⚠️ API キーの比較が `String.Equals(Ordinal)` で、ガイドの「FixedTimeEquals」と食い違っている → 移植するときに直す

### 🟠 3-5. template-web-fullstack

- 🐘 Aspire で **PostgreSQL（+ pgweb）・Valkey・YARP・初期化プロジェクト**を起動。Testcontainers のテスト
- ❌ UI・リアルタイム・ワーカーが無く、未コミットの作業も残っている → **PostgreSQL に移すときの参考**として使う

### 🟢 3-6. template-blazor-server-identity

- 🔑 **ASP.NET Core Identity + パスキー（WebAuthn、.NET 10）**。Identity の保存だけ EF Core + SQLite（`identity.db`、起動時に `MigrateAsync`）
- 📱 TOTP は未実装（H-9）。パスキーで MFA 相当のことがしたいならこちら

### ⏰ 3-7. template-worker / 📡 grpc-server / 🧬 library-with-generator

| テンプレート | 本件で使えるもの |
|---|---|
| ⏰ worker | **Mofucat.JobScheduler**（cron）+ `IAction` の DI 多重登録 → 定期ジョブ（統計の集計・保持期間の削除） |
| 📡 grpc-server | `Subscriptions/BroadcastSubscription`: **購読者ごとの上限つきキューで、遅い購読者は古いものから捨てる** → イベント配信の背圧の手本 |
| 🧬 library-with-generator | 生成器が **`[ModuleInitializer]` で「名前 → デリゲート」の表に登録** → インテグレーションの自動登録に応用できる |

### ⚪ 3-8. template-web-home

- 📭 中身 0 件・.git なし（2026-10-30 22:11 作成）。ドキュメントのどこにも言及が無い
- ➡️ 利用者の判断（2026-10-31）: **HomeManager はこの `D:\GitHubService\incubator-home` に作り、web-home は使わない**

---

## 🧮 4. 機能の保有マトリクス

| 機能 | blazor-server | maui-server | blazor-wasm | web-api | fullstack | 備考 |
|---|:-:|:-:|:-:|:-:|:-:|---|
| ⚙️ BackgroundService | ✅ 1 | ✅ 4 | — | — | — | grpc-server 1、worker は JobScheduler |
| 📡 SignalR Hub | — | ✅ 2 | — | — | — | 素の WebSocket エンドポイントはどこにも無い |
| 🚌 プロセス内バス → 画面 | ✅ | ✅（間引きあり） | — | — | — | |
| 💾 時系列の保存 | — | ✅ | — | — | — | |
| 🗄️ SQLite | ✅ | ✅ | ✅ | ✅ | — | 全部 Smart.Data.Accessor |
| 🐘 PostgreSQL | — | — | — | — | ✅ | DB の切り替え機構は無い |
| 🍪 Cookie 認証 | ✅ | — | — | — | — | bs-identity は Identity + パスキー |
| 🎫 JWT | — | 見本 | ✅（+リフレッシュ） | ✅ | ✅ | |
| 🗝️ API キー | — | — | — | ✅ | ✅ | 長期トークンはどこにも無い |
| 🚀 Aspire AppHost | ✅ | ✅ | ✅ | ✅ | ✅（複数リソース） | 13.6.1 |
| 🐳 Docker | — | — | — | — | — | **全テンプレートでゼロ（方針）** |
| 🐧 systemd | ✅ | ✅ | ✅ | ✅ | ✅ | `AddWindowsService().AddSystemd()` |
| 🧩 実行時プラグイン | — | — | — | — | — | DI 多重登録 / 生成器での登録のみ |
| 📡 MQTT・mDNS | — | — | — | — | — | **新しく作る** |

---

## ⚖️ 5. 構成パターンの比較

| 案 | 構成 | 👍 長所 | 👎 短所 |
|---|---|---|---|
| **(a)** | blazor-server だけ | 常駐・バス → 画面の即時更新・Cookie 認証・MudBlazor・OTel・テストが揃う。プロジェクトが少なく改造しやすい | 外部向けの認証（Bearer / API キー / 長期トークン）と Hub、時系列の保存が無い |
| **(b)** | blazor-wasm + web-api | UI も API のクライアントになり、HA と同じ「全員が同じ API」の分離。サーバーが軽い | リアルタイム・ワーカーを全部作る必要。2 つのソリューションの手作業での合体。トークンが sessionStorage。低性能タブレットで初回の読み込みが重い |
| **(c)** | web-fullstack | PostgreSQL・Valkey・YARP・Testcontainers の手本 | UI もリアルタイムもワーカーも無い。Valkey・YARP は自宅の 1 台構成には過剰 |
| **(d-1)** ⭐ | **blazor-server + 部品の移植** | 足りない部品は、どれも**同じ構造の既存テンプレートに実装済み** | 移植は手作業 |
| (d-2) | maui-server を土台 | Hub・ワーカー・時系列・チャートが最初から揃う | 管理画面の認証が無い。モバイル監視の固有機能（Chat・QR・OTLP 受信 …）を大量に削る必要 |

---

## 🏁 6. 推奨: (d-1) `template-blazor-server` をベースに部品を移植

### 👍 理由

1. ⚡ **画面へのプッシュが標準で手に入る**: Blazor Server の回線は WebSocket。「ワーカーが状態を変える → バス → `InvokeAsync(StateHasChanged)`」だけでダッシュボードが即時に更新される
2. 🧩 **外部向けの部品は maui-server に実装済み**で、フォルダーの置き場（`Hubs/` / `Workers/` / `Services/` …）が規則 A〜K で揃っている
3. 🔐 **認証の部品が揃っている**: 画面は Cookie（パスキーが欲しければ bs-identity）、API は web-api の JWT + API キー、長期トークンは blazor-wasm のハッシュ保存の方式を流用
4. 🔄 外部 API を最初から外向けに設計しておけば、**後から WASM や MAUI のフロントも足せる**

### 🚚 移植元のマップ

| 必要な機能 | 取り込み元 | 備考 |
|---|---|---|
| 🚌 状態の変化 → 画面 | blazor-server `NotificationBus` + maui-server `TelemetryBus`（例外の隔離）+ `RefreshTimer`（間引き） | |
| 📡 外部への配信 | maui-server `Hubs/MonitorHub` + `ServerStatusWorker` + `ServiceContextHubFilter` | gRPC で流すなら grpc-server の `BroadcastSubscription` |
| 🔔 通知（再送・既読） | maui-server `PushHub` + `PushNotifier` + `PushRetentionWorker` | HA の notify |
| ⚙️ 常駐のインテグレーション | blazor-server `NotificationWorker`（有効化のスイッチ、1 周の例外で死なない）、`__Refs/ref-hv-monitor`（HTTP ポーリング） | **MQTT / mDNS は新しく作る** |
| ⏰ 定期ジョブ | worker（Mofucat.JobScheduler + `IAction`） | 統計の集計・保持期間の削除 |
| 💾 状態の履歴 | maui-server `TelemetrySchema.sql` / 区間集計 SQL / `TelemetryRetentionWorker` / `TimeSeriesChart.razor` | WAL + WITHOUT ROWID + 保持期間 |
| 🔐 API の認証 | web-api `Application/Authentication/*` | API キーの比較は FixedTimeEquals に直す |
| 🗝️ 長期トークン | blazor-wasm `RefreshTokenService` | 名前・有効期限・失効の列を足す【推測】 |
| 🌐 外部 API の作法 | web-api（版の管理・ETag・RateLimiter） | |
| 🔀 リバースプロキシの背後 | maui-server `Settings/ForwardedHeadersSetting`（設定から CIDR を読む） | 他の 5 本は全面的に信頼している（ガイドと食い違い） |
| 🐘 PostgreSQL へ移す | web-fullstack | |
| 🧩 インテグレーションの登録 | DI の多重登録、または library-with-generator の ModuleInitializer | |
| 📱 ネイティブのクライアント | template-maui `Services/MonitorConnection.cs`（SignalR クライアント） | |

---

## ⚠️ 7. 派生するときの注意点

- 🔁 **`dotnet new` のテンプレート化は無い**: リポジトリを複製して `Template.BlazorServer` を手で置き換える（**112 ファイル** + フォルダー名・csproj 名・AppHost の `Projects.Template_BlazorServer_Host`・ログのファイル名）
- 🔑 AppHost の **`UserSecretsId` が 4 本のテンプレートで同じ値** → 作り直す
- 📊 Prometheus が既定で **`0.0.0.0:9464`** に公開される → 家の LAN でも絞る
- 🍓 PBKDF2 31 万回は Raspberry Pi でのログインの CPU 時間に効く【推測】
- 🐳 Docker・CI は自前で用意する（テンプレートの方針で対象外）

---

## 📱 8. 将来のクライアント候補

| テンプレート | 概要 | 本件との接点 |
|---|---|---|
| 📱 template-maui | maui-server の対向。REST / JWT・**SignalR クライアント**・gRPC・BLE / NFC など | モバイルアプリの実例 |
| 🧩 template-maui-blazor | MAUI の Blazor Hybrid（FluentUI 5）。SQLite・QR・ローカル通知 | Razor 部品の共有を狙える【推測】 |
| 🔒 template-maui-kiosk | Android の専用端末（Lock task・Device owner・自己更新・QR でのプロビジョニング） | 壁掛けタブレット。ただし **WebView は無い** |
| 🐧 template-avalonia-linux | Linux x64 のキオスク（Avalonia 12） | 据え置きパネル。ネットワークのクライアントは無い |
| 🍓 template-avalonia-embedded | Raspberry Pi（Framebuffer / DRM・GPIO・systemd） | 組み込みパネル。ネットワークのクライアントは無い |
| 🖥️ template-command-tool | CLI（System.CommandLine）・出力形式の切り替え | 運用 CLI（`home states list` など） |
