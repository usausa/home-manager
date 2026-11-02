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
| 🔌 | **外とつなぐ** | REST・リアルタイム API、長期アクセストークン、Webhook、（AI 連携） |

👉 機能の一覧と採用の決定は [docs/00-system-features.md](docs/00-system-features.md)、調査と設計は [docs/README.md](docs/README.md)。

---

## 🚦 現在の状態

| Phase | 内容 | 状態 |
|:-:|---|:-:|
| 🏗️ P0 | `template-blazor-server` から派生・見本の機能を削除・番号付き SQL のマイグレーション・Aspire に Mosquitto・配置用ファイル（2026-10-31〜11-01。DB は P0 で PostgreSQL にしたが、2026-11-02 の見直しで **SQLite** に変更） | ✅ |
| 🌱 P1 | コアの 3 本柱（EventBus・StateMachine・ServiceRegistry）・エンティティ（7 ドメイン）・インテグレーション基盤・仮想デバイス・概要ダッシュボード・エンティティ詳細・開発者ツール。**ブラウザ 2 枚で「片方の操作がもう片方に即座に反映」を確認済み（2026-11-01）**。2026-11-02 に初回セットアップ・プロフィール・設定（全体設定・システム情報）を追加して機能がそろった（CARD-02 は P4 へ）。DB を SQLite に変え、見直しと整理も済んだ（2026-11-02）。次は P2 | ✅ |

👉 細かい進み具合は [docs/11-progress.md](docs/11-progress.md)（チェックリスト。作業しながら更新する）。

### 🖥️ いま使える画面（P1）

| 画面 | URL | できること |
|---|---|---|
| 🏠 概要 | `/` | 全エンティティをドメインごとのタイルで表示（開いている全員の画面が即座に変わる）。アイコンで ON / OFF・ボタンを押す、タイルで詳細、名前・entity_id で検索 |
| 🪟 エンティティ詳細 | （タイルから） | 操作（ON / OFF・明るさ・数値・選択肢・押す）と属性、最終変更の時刻と「誰が操作したか」 |
| 🗃️ 開発者ツール: 状態 👑 | `/developer-tools/state` | 全状態と属性の一覧・絞り込み、デバッグ用の状態の上書き |
| 🛎️ 開発者ツール: アクション 👑 | `/developer-tools/action` | サービス（`light.turn_on` など）を選び、対象とデータ（JSON）を入れて実行 |
| 📣 開発者ツール: イベント 👑 | `/developer-tools/event` | イベントの流れを購読して見る・任意の名前のイベントを発火 |
| 🚀 初回セットアップ | `/onboarding` | 初回のログインで、初期パスワードの変更 → 家の情報（全体設定）→ 完了。済むまでほかの画面には進めない |
| 👤 プロフィール | `/profile`（上部バーの名前） | 表示名・テーマ（ライト／ダーク／自動）・パスワードの変更 |
| ⚙️ 設定 👑 | `/config` | 全体設定・システム情報へのメニュー |
| 🌍 全体設定 👑 | `/config/general` | 家の名前（概要の見出し）・位置・タイムゾーン（時刻の表示）・単位系・通貨・国。ほかの画面が先に保存していたら最新の値を読み直す |
| ℹ️ システム情報 👑 | `/config/info` | 版・稼働時間・DB（SQLite の版・ファイルの場所・大きさ）・インテグレーションの状態・接続中の回線（その場で変わる） |

> 🧪 仮想デバイス（`HomeManager.Integration.Demo`）: 照明 3・スイッチ 2・センサー 4（温度・湿度・CO2・洗濯機の電力）・2 値センサー 2（人感・ドア）・ボタン・数値・選択肢。センサーは 5 秒ごとに値が動く。👑 = 管理者だけ

---

## 🗂️ 構成

```text
HomeManager.slnx
├── src/
│   ├── HomeManager.AppHost/   🚀 Aspire（開発時: Host + Mosquitto。DB は SQLite で Host の中）
│   ├── HomeManager.Core/      🧠 ASP.NET に依存しない部分
│   │   ├── Domain/            EntityState・EventContext・イベント・定数（ドメイン・属性・device_class）
│   │   ├── Services/          EventBus・StateMachine・ServiceRegistry（HA の 3 本柱）・アカウント・DB
│   │   ├── Platforms/         エンティティの基底（HomeEntity・LightEntity …）・EntityManager・ドメインのサービス（light.turn_on …）
│   │   ├── Integrations/      IIntegration・IntegrationManager（インテグレーションの読み込みと停止）
│   │   └── Accessors/ Models/ Infrastructure/
│   ├── HomeManager.Integration.Demo/  🧪 仮想デバイス（1 インテグレーション = 1 プロジェクト）
│   └── HomeManager.Host/      🖥️ Blazor Server + Minimal API（Assets/Data/Migrations に番号付きの SQL・Workers/IntegrationWorker）
├── tests/
│   ├── HomeManager.UnitTests/         🧪 単体（xUnit v3・bUnit）
│   ├── HomeManager.IntegrationTests/  🔌 結合（WebApplicationFactory + 一時ファイルの SQLite）
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
| コンテナの実行環境 | Docker か Podman（Aspire で Mosquitto を動かすときだけ。DB とテストには要らない） |

### 起動

```powershell
aspire run
```

- 🐧 Podman を使うときは、先に `$env:ASPIRE_CONTAINER_RUNTIME = "podman"` を設定する
- 🧱 Podman の仮想マシンからレジストリにつながらない環境では、ホストでイメージを取得して `podman load` で読み込んでおく（使うイメージ: `eclipse-mosquitto:2.1.2-alpine`）

- AppHost が Mosquitto と HomeManager.Host を起動し、ダッシュボードが開く（`dotnet run --project src/HomeManager.Host` で Host だけを動かしてもよい）
- 🪶 DB は **SQLite のファイル**。既定は実行フォルダーの `data/homemanager.db`（`ConnectionStrings__Default` で変えられる）。フォルダーは起動時に作る
- 初期ユーザーは `admin` / `admin`。最初にログインすると初回セットアップ（パスワードの変更・家の情報）が出る
- 起動するとインテグレーション（P1 は仮想デバイスだけ）が読み込まれ、概要にタイルが並ぶ。ブラウザを 2 つ開いて片方で照明を切り替えると、もう片方も変わる
- DB のスキーマは起動時に `src/HomeManager.Host/Assets/Data/Migrations/*.sql` を番号順に適用する（適用した版は `SchemaVersion` の表）

### テスト

テストは Microsoft Testing Platform で動かす（`dotnet test` ではなく `dotnet run --project`）。

```powershell
# 単体
dotnet run --project tests/HomeManager.UnitTests

# 結合（一時ファイルの SQLite を使う。コンテナは要らない）
dotnet run --project tests/HomeManager.IntegrationTests
```

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

1. 👤 ユーザー: `sudo useradd --system --home-dir /var/lib/homemanager --shell /usr/sbin/nologin homemanager`（ホームをデータフォルダーにすると、ASP.NET の鍵（Data Protection）もそこに入る）
2. 🔐 秘密: `deploy/homemanager.env.example` を `/etc/homemanager/homemanager.env` にコピーし、初期パスワードを書き換える（**サービスを動かす前に置く**。無いと既定の `data/homemanager.db` と admin / admin で起動してしまう）
3. 🐧 アプリ: 発行物を `/opt/homemanager` に置き、`deploy/homemanager.service` を `/etc/systemd/system/` にコピーして `systemctl enable --now homemanager`（DB のファイルは `/var/lib/homemanager/homemanager.db`。フォルダーは systemd が作る）
4. 🐳 MQTT の機器をつなぐときだけ: `deploy/` で `docker compose up -d`（Mosquitto）
5. 💾 バックアップ: `/var/lib/homemanager` をコピーする（DB と鍵が入っている。動かしたままなら SQLite の `VACUUM INTO` で書き出す）

> ⚠️ `deploy/` のファイルは Linux で使うので改行は LF（`.gitattributes` で指定）。
