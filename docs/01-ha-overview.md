# 🏠 01. Home Assistant の全体像

> 📅 2026-10-31 時点。GitHub・公式ブログ・参考記事を調べた結果（サブエージェントの調査を整理したもの）
> 🏷️ 確度: ✅ 一次情報で確認 ／ 📝 リリースノートなどの要約経由で確認 ／ ❓ 未確認（一般知識。使う前に確認）

---

## 🎯 1. Home Assistant で実現している機能

> 🏠 **メーカーの垣根を越えて、家の機器を 1 か所で管理・自動化するオープンソースのソフトウェア。** 家の中のサーバーで動き、クラウドに頼らない（ローカルファースト）。

| 分類 | 実現している機能 | 例 |
|---|---|---|
| 📡 **集める** | 1,500 を超えるインテグレーションで、各社の機器・サービスとつながる（[02](02-ha-core-architecture.md) で数えた core 内のインテグレーションは 1,563） | Hue・SwitchBot・Zigbee・Z-Wave・Matter・MQTT・ESPHome・クラウドサービス |
| 🖥️ **見る・操作する** | **部屋（エリア）ごとに自動生成**される既定のダッシュボード「Overview」（2026.2 から）、自分で作るダッシュボード、スマホのアプリ | 照明だけ・空調だけといった絞り込み、タブ付きのダッシュボード |
| 🤖 **自動化** | トリガー → 条件 → アクションのオートメーション、スクリプト、シーン、ブループリント、**用途別トリガー**（2026.7 から既定） | 「日没後に人感センサーで照明」 |
| 📈 **記録** | 履歴、**Activity**（旧ログブック）、長期統計、エネルギーダッシュボード（電気・ガス・水） | 電力量の日・月の比較 |
| 🗣️ **音声・AI** | Assist（会話・音声認識・音声合成・ウェイクワード）、MCP のサーバーとクライアント、AI Task | 「リビングの電気を消して」 |
| 🔌 **外とつなぐ** | WebSocket・REST の API、HomeKit Bridge（Apple Home・Siri から操作）、Google・Alexa 連携（有料のクラウド経由） | Siri で HA の機器を操作 |
| 🛠️ **運用** | 自動バックアップ（暗号化・保存先の差し替え）、ワンクリック更新、**Apps**（旧アドオン。横で動く別プログラム） | Mosquitto・Z-Wave JS・Matter Server を App として動かす |

---

## 📰 2. 参考記事の要点

| 記事 | 年 | 何の話か | HA について読み取れること |
|---|---|---|---|
| 📰 [ZDNET Japan](https://japan.zdnet.com/article/35253722/) | 2026-10 | Google Home 中心の家を、週末で Home Assistant に移行し、Siri で操作できるようにした体験記（海外記事の翻訳） | 専用機（Home Assistant Green）なら開封から移行まで約 10 分。HomeKit Bridge で HomeKit 非対応の機器も Siri から操作。エリアごとの Overview が使いやすい。非営利財団のプロジェクトで、互換性は大手と同等 |
| 📰 [DIY Smart Matter](https://diysmartmatter.com/archives/341) | 2022 | 古い NUC に Ubuntu を入れ、Homebridge と HA を Docker で同居させた導入記録 | 「Container」形態の典型例（`network_mode: host`・`/config` のボリューム）。Mosquitto などはホストに直接入れる方針。Container では Apps（旧アドオン）は使えない |
| 📰 [GIGAZINE](https://gigazine.net/news/20240423-open-home-foundation-iot/) | 2024-04 | HA の開発陣が非営利の **Open Home Foundation** を設立 | 2013 年に Hue を操作する数行の Python から始まった。燃え尽き・営利企業（Nabu Casa）との関係を経て、**買収や資本の干渉を避ける**ために財団化。対応ブランドは 2,758（当時） |

> 💡 3 本を並べると、HA は「マニア向けの OSS（2022）」→「財団で統治を固める（2024）」→「一般メディアが乗り換え先として紹介する製品（2026）」へ移ってきている。

---

## 🐙 3. GitHub のリポジトリ構成（✅ 2026-10-31・GitHub API）

org の公開リポジトリは **131**。主なもの:

| リポジトリ | 役割 | 言語 | ⭐ Stars |
|---|---|---|---:|
| `core` | 本体（[02](02-ha-core-architecture.md) で分析） | Python | 91,855 |
| `home-assistant.io` | ユーザー向けの文書と公式ブログ | HTML | 10,070 |
| `operating-system` | HA OS（Buildroot ベース） | — | 7,606 |
| `frontend` | Web の画面 | TypeScript | 5,771 |
| `android` / `iOS` | Companion アプリ | Kotlin / Swift | 4,012 / 2,395 |
| `supervisor` | HA OS 上で本体と Apps を管理 | Python | 2,253 |
| `apps`（旧 `addons`） | 公式の Apps | Shell | 2,249 |
| `home-assistant-js-websocket` | JS 用の WebSocket クライアント | TypeScript | 415 |
| `brands` | インテグレーションのロゴ・アイコン | — | 421 |
| `contributors.home-assistant.io` | **開発者向け文書の新しい置き場所** | JavaScript | 55 |
| `developers.home-assistant` | 旧開発者サイト（**2026-10-16 にアーカイブ**） | JavaScript | 521 |
| `cli` / `os-agent` | `ha` コマンド / D-Bus 経由で OS を操作するデーモン | Go | 274 / 345 |
| `app-*`（約 22 個） | App ごとのリポジトリ（Matter Server・Z-Wave JS・Mosquitto・Whisper・Piper …） | — | — |

> 📘 **開発者向け文書は 2026-10-16 に `contributors.home-assistant.io` へ移転**した（✅）。旧 `developers.home-assistant.io` の URL は転送される。

---

## 🏛️ 4. Open Home Foundation（OHF）

| 区分 | プロジェクト | HA との関係 |
|---|---|---|
| 🏠 財団が所有 | **Home Assistant**・**ESPHome**・**Music Assistant** | ESPHome は HA の `esphome` インテグレーションが機器と直接通信（✅ プロキシ機能 / ❓ プロトコルの詳細） |
| 🤝 提携 | Rhasspy・WLED・**Zigbee2MQTT**・OpenDisplay | Zigbee2MQTT は MQTT 経由で HA とつながる |
| 📚 標準・ドライバ・ライブラリ（250 以上） | Z-Wave JS・zigpy・Matter.js・Piper・microWakeWord・ESP Web Tools・Improv Wi-Fi・HACS など（📝） | Z-Wave JS / Matter Server は **別プロセスの WebSocket サーバー**として HA とつながる（✅） |

- 🧭 原則は **プライバシー・選択・持続可能性**（📝）
- 🗣️ 音声（音声認識・音声合成・ウェイクワード）は **Wyoming プロトコル**で別プロセスにする（✅ 各 App の説明）

---

## 📦 5. インストール形態と Supervisor / Apps

### 5-1. インストール形態（✅）

| 形態 | 状態 | 内容 |
|---|---|---|
| 🍓 **HA OS**（推奨） | ✅ 公式 | 専用の最小 OS。Apps・ワンクリック更新・バックアップ。Green / Raspberry Pi 4・5 / x86-64 / VM |
| 🐳 **Container** | ✅ 公式 | Docker などで本体だけを動かす。**Apps は使えない**（ブローカーなどは自分で用意） |
| 🐍 Core（Python 環境） | ⛔ 2025-05 に非推奨 → **2025.12 でサポート終了** | 2026.9 からは開発用の「Virtual Environment」と呼ぶ（📝） |
| 🧩 Supervised | ⛔ 同上 | 任意の Linux に Supervisor を載せる形 |
| 32 ビット（i386・armhf・armv7） | ⛔ 同上 | 利用率は 1% 未満だった |

### 5-2. Supervisor と Apps

- 🧱 Supervisor 自身もコンテナで動き、**本体・Apps・プラグイン（DNS・音声・マルチキャスト・監視）のコンテナを管理**する（❓ 仕組みの詳細）
- 🔁 **Add-on は 2026.2 で「Apps」に改称**（✅）: 初心者が「インテグレーション（本体内のプラグイン）」と混同するため。例: Mosquitto **App** がブローカーを動かし、MQTT **インテグレーション**がそこへ接続する
- 🔒 2026.4 に Supervisor の脆弱性（CVSS 9.7）が修正済みと告知（📝）

> 💡 **HomeManager との対応**: HA の 4 層（本体 / Supervisor / Apps / インテグレーション）のうち、HomeManager が作るのは**本体とインテグレーション**だけ。Supervisor と Apps の役割は **systemd + Docker Compose** が担う（[06](06-dotnet-architecture.md) §8）。

---

## 📊 6. 規模感

| 項目 | 値 | 確度・出典 |
|---|---|---|
| ⭐ core の Stars / Forks | 91,855 / 39,147 | ✅ GitHub API |
| 👥 core のコントリビューター | 5,406 | ✅ GitHub |
| 🏷️ core のリリース数 | 1,662（最新 2026.10.4） | ✅ GitHub |
| 🏠 アクティブインストール | **200 万**（2025-05 の公表） | ✅ 2025.5 のリリース投稿。現在の値は ❓ |
| 🍓 Raspberry Pi で動いている割合 | 約 3 分の 1 | 📝 2026.7 のリリース投稿 |
| 🏭 対応ブランド | 2,758（2024-04） | ✅ GIGAZINE |
| 🧩 core 内のインテグレーション | 1,563 | ✅ [02](02-ha-core-architecture.md) で manifest を数えた値 |
| 🗓️ リリース | **毎月**（`YYYY.M`、第 1 水曜前後）+ ほぼ毎週のパッチ（`YYYY.M.x`） | ✅ リリースノート一覧 |

---

## 🆕 7. 2025〜2026 年の大きな変化

| 分野 | 変化 | 確度 |
|---|---|:-:|
| 📦 インストール | Core / Supervised・32 ビットのサポート終了（2025.12）。新規の HA OS は `:8123` なしの URL が既定（2026.8） | ✅ / 📝 |
| 🔁 名称 | Add-on → **Apps**（2026.2）、ログブック → **Activity**（2025.10）、開発者ツール → **Tools**（2026.8）、Home Assistant Cloud → **Home Assistant Link**（2026-12 予定） | ✅ / 📝 |
| 🧱 ダッシュボード | エリアから**自動生成する Overview** が既定に（2026.2）。照明・空調・セキュリティ・メンテナンス・地図の用途別ダッシュボード。ダッシュボードの生成（strategy）をカスタムで登録可能に（2026.5） | ✅ / 📝 |
| 🤖 自動化 | **用途別トリガー・条件**（エリア・フロア・ラベルを対象に「人感」「温度」などを書く）が既定に（2026.7）。トリガー ID の自動付与（2026.10）。Labs（プレビュー機能）の新設（2025.12） | 📝 |
| 🤖 AI | MCP のサーバー・クライアント（2025.2）、AI Task（2025.8）、LiteLLM / llama.cpp（2026.8）、MCP を設定画面から有効化（2026.10） | 📝 |
| 💾 バックアップ | 全面刷新（2025.1）: 自動化・暗号化が既定・保存先を差し替え可能（Google Drive・OneDrive・S3 など） | 📝 |
| 📡 物理層 | Bluetooth の可視化（2025.6）、赤外線（2026.4）、sub-GHz RF・シリアル（2026.5）、接続設定の画面を集約（2026.9） | 📝 |
| 🔐 厳格化 | 管理者だけの操作が増加、古い認証モードの削除・ユーザー名は小文字必須（2026.10） | 📝 |
| 📘 文書 | 開発者向け文書を `contributors.home-assistant.io` に一本化（2026-10-16） | ✅ |

> 🔍 [02](02-ha-core-architecture.md) のソース分析でも、用途別トリガー（`motion` / `temperature` などのインテグレーション）・`labs`・GitHub からカスタムインテグレーションなどを入れる `marketplace`（HACS 相当の公式化）が確認できた。

---

## 🎓 8. HomeManager への示唆

| # | HA の動向 | HomeManager でどうするか |
|:-:|---|---|
| 1 | 🧱 **エリアから自動生成する Overview** が既定に | 概要（SCR-101）を同じ考え方で作る（[07](07-screen-design.md)）。レジストリ（エリア）を早めに整える |
| 2 | 🎯 用途別トリガー（「リビングで人感」） | まずは普通のトリガーで作り、P6 で追加（[00](00-system-features.md) F-38） |
| 3 | 📡 Bluetooth の可視化 | Bluetooth 診断（SCR-318）を P2 で作る（手持ちの BLE 機器の登録に必要） |
| 4 | 🔁 Apps と インテグレーションの区別 | 「横で動く別プログラム」（Mosquitto・Zigbee2MQTT）は Docker Compose、「本体内のプラグイン」は `Integration.*` と、同じ区別で設計する |
| 5 | 🤖 AI を部品として組み込む（MCP・AI Task） | P6 の AI 連携は MCP サーバーから始める（[00](00-system-features.md) F-54） |
| 6 | 🔐 管理者権限の厳格化 | 操作系の API・画面の権限を最初から分けておく（[08](08-api-design.md) §2） |
