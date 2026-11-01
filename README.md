# 🏠 HomeManager

> 家の中の機器の状態を 1 か所に集め、ブラウザでリアルタイムに見て・操作し、条件に応じて自動で動かし、記録を残す ── Raspberry Pi で動くホームオートメーションの Web アプリ（.NET・個人学習用）。
> [Home Assistant](https://github.com/home-assistant) の設計を手本にしている。

---

## 🎯 実現する機能

| | 実現する機能 | 主な例 |
|:-:|---|---|
| 📡 | **集める** | SwitchBot の温湿度計・CO2 センサー・プラグミニ、ワットチェッカー（BLE・Wi-Fi）、オムロン環境センサー、Ping、（MQTT の機器） |
| 🖥️ | **見る・操作する** | 部屋ごとに自動で作るダッシュボード（開いている全員の画面が即座に変わる）、機器の詳細と操作、自分好みのダッシュボード |
| 🤖 | **自動で動かす** | 「CO2 が高ければ換気を通知」「洗濯機の電力が下がったら終了を通知」「日没後の人感で照明」、スクリプト・シーン・ヘルパー |
| 📈 | **記録する** | 履歴・ログブック・長期統計・家電ごとの電力量、Prometheus / Grafana への出力 |
| 🔌 | **外とつなぐ** | REST・リアルタイム API、長期アクセストークン、Webhook |

👉 機能の一覧と採用の決定は [docs/00-system-features.md](docs/00-system-features.md)、調査と設計は [docs/README.md](docs/README.md)。

---

## 🚦 現在の状態

| Phase | 内容 | 状態 |
|:-:|---|:-:|
| 🏗️ P0 | `template-blazor-server` から派生・見本の機能を削除・DB を PostgreSQL に変更（マイグレーション）・Aspire に PostgreSQL と Mosquitto・配置用ファイル。PostgreSQL 18 での結合テストと Aspire での起動・再起動を確認済み（2026-11-01） | ✅ |
| 🌱 P1 | コア（EventBus・StateMachine・ServiceRegistry）・仮想デバイス・概要ダッシュボード | ⏳ |

---

## 🗂️ 構成

```text
HomeManager.slnx
├── src/
│   ├── HomeManager.AppHost/   🚀 Aspire（開発時: Host + PostgreSQL + pgweb + Mosquitto）
│   ├── HomeManager.Core/      🧠 ASP.NET に依存しない部分（Accessors / Services / Models / Infrastructure / Domain）
│   └── HomeManager.Host/      🖥️ Blazor Server + Minimal API（Assets/Data/Migrations に番号付きの SQL）
├── tests/
│   ├── HomeManager.UnitTests/         🧪 単体（xUnit v3・bUnit）
│   ├── HomeManager.IntegrationTests/  🔌 結合（WebApplicationFactory + PostgreSQL のコンテナ）
│   └── HomeManager.E2ETests/          🎭 E2E（Playwright）
├── deploy/   🍓 本番用（compose.yaml・systemd のユニット・設定の例）
└── docs/     📚 調査と設計
```

---

## 🛠️ 開発

### 必要なもの

| もの | 版・備考 |
|---|---|
| .NET SDK | 10.0 |
| Aspire CLI | 13.5 以上（AppHost が `AspireUseCliBundle=true` のため必須） |
| コンテナの実行環境 | Docker か Podman（PostgreSQL・Mosquitto・結合テストで使う） |

### 起動

```powershell
aspire run
```

- 🐧 Podman を使うときは、先に `$env:ASPIRE_CONTAINER_RUNTIME = "podman"` を設定する
- 🧱 Podman の仮想マシンからレジストリにつながらない環境では、ホストでイメージを取得して `podman load` で読み込んでおく（使うイメージ: `postgres:18-alpine`・`sosedoff/pgweb:0.17.0`・`eclipse-mosquitto:2.1.2-alpine`）

- AppHost が PostgreSQL（+ pgweb）・Mosquitto・HomeManager.Host を起動し、ダッシュボードが開く
- 接続文字列は Aspire が `ConnectionStrings__Default` として渡す（`appsettings.json` の既定値は `localhost:5432` の PostgreSQL）
- 初期ユーザーは `admin` / `admin`（P1 で初回にパスワードの変更を必須にする）
- DB のスキーマは起動時に `src/HomeManager.Host/Assets/Data/Migrations/*.sql` を番号順に適用する（適用した版は `core.SchemaVersion`）

### テスト

テストは Microsoft Testing Platform で動かす（`dotnet test` ではなく `dotnet run --project`）。

```powershell
# 単体
dotnet run --project tests/HomeManager.UnitTests

# 結合（PostgreSQL のコンテナを使う。Docker / Podman が無ければスキップする）
dotnet run --project tests/HomeManager.IntegrationTests
```

- コンテナの実行環境は自動で検出する。明示するなら環境変数 `TEST_CONTAINER=docker|podman|none`
- Podman の仮想マシンが Ryuk（後片付け用）のイメージを取得できないときは `TESTCONTAINERS_RYUK_DISABLED=true` で実行する
- E2E（Playwright）は日常の実行に含めない。初回はブラウザを入れてから実行する

```powershell
dotnet build tests/HomeManager.E2ETests
pwsh tests/HomeManager.E2ETests/bin/Debug/net10.0/playwright.ps1 install
dotnet run --project tests/HomeManager.E2ETests
```

---

## 🚢 配置（Raspberry Pi など）

```bash
dotnet publish src/HomeManager.Host -c Release -r linux-arm64 --self-contained -o publish/linux-arm64
```

1. 🐳 ミドルウェア: `deploy/` で `.env.example` を `.env` にコピーしてパスワードを変え、`docker compose up -d`
2. 🐧 アプリ: 発行物を `/opt/homemanager` に置き、`deploy/homemanager.service` を `/etc/systemd/system/` にコピーして `systemctl enable --now homemanager`
3. 🔐 秘密: `deploy/homemanager.env.example` を `/etc/homemanager/homemanager.env` にコピーし、接続文字列と初期パスワードを書き換える

> ⚠️ `deploy/` のファイルは Linux で使うので改行は LF（`.gitattributes` で指定）。
