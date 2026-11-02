# 📋 11. 進捗チェックリスト

> 📌 **実装の進み具合**をチェックリストで残す。作業しながら更新する（最終更新: 2026-11-02）
> ✅ = 済み ／ 🚧 = 作業中 ／ ⏳ = 未着手 ／ ❓ = 決めることがある
> 🔗 フェーズの中身は [05 §4](05-feature-scope.md)、画面は [07](07-screen-design.md)、API は [08](08-api-design.md)、DB は [09](09-db-design.md)

---

## ⚡ 要点

| Phase | 内容 | 状態 | メモ |
|:-:|---|:-:|---|
| 🏗️ P0 | 器づくり（テンプレートから派生・PostgreSQL・Aspire） | ✅ | 2026-10-31 |
| 🌱 P1 | コアが動く MVP（3 本柱・仮想デバイス・画面） | ✅ | 2026-11-01〜02。機能・DB の切り替え（SQLite）・見直しと整理まで済んだ（CARD-02 は P4 へ）。**次は P2** |
| 🔌 P2 | 手持ちの機器がつながる | ⏳ | |
| 🤖 P3 | 家が自動で動く | ⏳ | |
| 📈 P4 | 過去が見える | ⏳ | |
| 🔐 P5 | 外から安全に使える | ⏳ | |
| 🚀 P6 | 好きな方向に伸ばす | ⏳ | |

### ✅ P1 の残りと締め（2026-11-02）

| # | 作業 | 状態 |
|:-:|---|:-:|
| 1 | 🧠 Core のサービス（全体設定・初回セットアップ・アカウント・システム情報） | ✅ |
| 2 | 🖥️ 画面（初回セットアップ・プロフィール・設定トップ・全体設定・システム情報） | ✅ |
| 3 | 🧪 テスト（単体・結合・E2E） | ✅ |
| 4 | 🌐 ブラウザでの確認 | ✅ |
| 5 | 📝 ドキュメント（README・06・07・08・09） | ✅ |
| 6 | 💾 コミット（`feature/p1-settings` → main に早送りでマージ） | ✅ |
| 7 | 🏁 P1 の締め（CARD-02 の扱い・DB を SQLite に・見直しと整理・古いブランチとボリュームの片付け） | ✅ |

### 🏁 P1 の締め（P1 が完了したら）

> 🗣️ 2026-11-02 の方針: **P1 を完了まで持っていったら、いったん見直しと整理を行う。そのときに古いブランチなども片付ける**

- [x] ➡️ CARD-02 エンティティ一覧は **P4 に回す**（使う画面がダッシュボードの編集 UI-02 と同じ P4。2026-11-02 決定）
- [x] 🗄️ DB の見直し: PostgreSQL が本当に要るか。**SQLite で済む範囲ならポータブルな方向（SQLite）に方針を変える**（2026-11-02 の方針）
  - [x] 要件ごとに比べた → **SQLite で済む**（HA も既定は SQLite）。まとめは [12 DB の見直し](12-db-review.md)
  - [x] ✅ **SQLite に切り替えると決定**（2026-11-02。データは `data/homemanager.db`、P4 の履歴は別のファイル）
  - [x] ✅ 切り替えた（2026-11-02）: Host（`Microsoft.Data.Sqlite`・起動時に `data/` を作る・WAL）・マイグレーション（`STRICT`・`json_valid`）・SQL・テスト（一時ファイル。コンテナ不要）・Aspire（Mosquitto だけ）・配置（compose は Mosquitto だけ、systemd の `StateDirectory`）・ドキュメント（README・00・04・05・06・07・09・12）
  - [x] 🧪 単体 130・結合 11（約 6 秒 → 1.6 秒）・E2E 2 が成功、警告 0
  - [x] 🌐 ブラウザで確認（使い捨ての SQLite ファイル）: 初回セットアップ → 概要、システム情報に「SQLite 3.53.3」とファイルの場所、強制終了して再起動しても WAL から戻る
- [x] 🔍 見直し: コード・テスト・ドキュメントを読み直す（重複・使っていない部品・古い記述・設計と実装のずれ）。ブランチ `feature/p1-review`
  - [x] 🔍 コード・テストの見直し: 指摘 約 50 件（不具合 12・未使用 10・重複 6・規約 6・テストの穴 7）。仕分け:
    - [x] 🐛 直した: 停止時に全エンティティの状態を消している（E2E の例外の原因。HA は消さない）・停止以外のキャンセルを握りつぶす（背景処理・セットアップ）・重複の判定が広すぎる（NOT NULL なども「重複」）・`SuggestedObjectId` を slug にしていない・状態のツールで 255 文字を超えると落ちる・設定の JSON の形が違うと起動が止まる・最初の読み込みに失敗すると回線が直らない・入れ子の設定（初期パスワード）を検査していない・概要が全体設定の変更を待たない
    - [x] 🧹 消した: パッケージ（`FluentValidation`・`Usa.Smart.Mapper`・`Serilog.Enrichers.Span`・`FeatureManagement`）・`AddMemoryCache`・`RequestHelper`・`DateTimeConverter`・`Extensions.cs`・`PagedResult`・`DialectExtensions`・`Length.Id`・使っていないログ・CSS 3 つ
    - [x] 🧽 そろえた: PostgreSQL 時代のコメント・`AccountAccessor` の `CancellationToken`・`AccountService` の時刻（`TimeProvider` に。起動時の特別なコンテキストも不要に）・`CoreConfigForm` の `required`・パスワード変更の入力欄を部品に（`PasswordChangeForm`）・全体設定の競合の文言を 1 か所に・検索欄の CSS を 1 つに・DB に保存する JSON の書き方を 1 か所に（`StoredJson`）
    - [x] 🧪 足した: 画面を閉じたら購読が外れる・停止しても状態が残る・インテグレーション自身のキャンセルはセットアップの失敗・`object_id` を slug に・長すぎる状態・全体設定の変更で概要を描き直す・読み込みの再試行・壊れた設定の JSON・初期パスワードが空なら起動しない（単体 +7 → 137、結合 +2 → 13）
    - ➡️ 残す（使う予定）: `MessageBox`（P2 の削除の確認）・`DataWriteStatus` の `NotFound` / `Duplicate`（P2 のレジストリ）・`LastLoginAt`（P5）・API 一覧にあるメソッド・`ConnectionStrings__mqtt`（P2）
    - ➡️ P2 に回す: エンティティの書き込みの順序（MQTT と画面の操作が重なるとき）・初回セットアップの手順を増やすときの直列化・homemanager のサービスの登録場所を 1 つに・状態の購読の書き方の共通化・詳細ダイアログと開発者ツールのテスト・`DatabaseService` の守りのテスト
  - [x] 📝 ドキュメントと実装の突き合わせ: 指摘 39 件 → **37 件を直した**（残した 2 件は日付つきの経緯）。主なもの:
    - PostgreSQL の書き残し（06・05・08・09・docs/README の索引）、06 の配置図に残っていた PG の線
    - README の配置の手順の順番（環境変数のファイルを先に置く・ユーザーを作る・鍵もデータフォルダーに入れる）
    - 初回セットアップの権限（全員。家の情報だけ管理者）、全体設定の項目（標高・国）、使うサービスに `IOnboardingService`
    - 名前のずれ（`RefreshTimer` → `RefreshThrottle`・`StateChanged` → `StateChangedEvent`・`SetState`・`/api/v1/...`）と、06 の「P1 で実装した形」に API の名前とサービスの入力の違いを追加
    - Context のユーザーは数値でなくログイン名（08 の例・09 の `ContextUserId` は TEXT。`Account.Id` に引き直すかは P4 で決める）
    - Prometheus への出力の URL を `/api/v1/prometheus` にそろえた（10）、12 §4 を「切り替えでやったこと」に書き換えた、リンク先の § を直した
  - 先に分かっている候補（2026-11-02 に grep で確認）:
    - [x] 📦 使っていないパッケージ: `FluentValidation`・`Usa.Smart.Mapper` → 外した
    - [x] 🚩 `FeatureManagement` は登録だけ（機能フラグは P1 で消した）→ 外した
    - [x] 🧩 使っていないコード: `PagedResult`・`DialectExtensions.Match`・`Length.Id` → 消した（`DataWriteStatus.Duplicate` は P2 のレジストリで使うので残した）
    - [x] ❓ `MessageBox`（確認ダイアログ）は未使用。P2 の削除の確認で使う予定 → 残した
    - [x] 🗓️ `Account.LastLoginAt` は書いていない（P5 のユーザー一覧で使う予定）→ 残した
    - [x] 🧪 E2E の終了時に `ObjectDisposedException`（止める途中で概要が描き直される）→ 直った（停止で状態を消さないようにした。E2E のログで 0 件）
- [x] 🧽 整理: 見直しで見つけたものを直した。クリーンビルドで警告 0、単体 137・結合 13・E2E 2 が成功
- [x] 🧹 古いブランチを消した: `feature/p0-solution`・`feature/p1-core`・`feature/p1-settings`・`feature/p1-sqlite`・`feature/p1-review`（どれも main に取り込み済み。リモートには無い。2026-11-02）
- [x] 🧹 使わなくなった Aspire のボリュームを消した: PostgreSQL のデータ（`homemanager.apphost-…-postgres-data`。SQLite に変えたので不要。2026-11-02）
- [x] 🚀 ここまで（P1 の機能・CARD-02 の移動・DB の切り替え）を `feature/p1-sqlite` にコミットし、main に早送りでマージして push（2026-11-02）
- [x] 🚀 見直しと整理の分を `feature/p1-review` にコミットし、main に早送りでマージして push（2026-11-02）

---

## 🌱 P1 の残り（詳細）

### 🧠 Core

- [x] 🌍 全体設定 `ICoreConfigService`（`core.Setting` のキー `core.config`・楽観ロック・`core_config_updated`）
- [x] 🔁 `homemanager.reload_core_config`（DB から読み直す）
- [x] 🚀 初回セットアップの記録 `IOnboardingService`（`core.Setting` のキー `core.onboarding`）
- [x] 👤 アカウント `IAccountService`（プロフィール・テーマ・パスワードの変更。パスワードは 8 文字以上・今と違うもの）
- [x] ℹ️ システム情報 `ISystemInfoService`（版・稼働時間・DB の大きさ・インテグレーション）
- [x] 🕒 タイムゾーン（IANA の ID。Windows の ID は変換して一覧にする）

### 🖥️ 画面

- [x] 🚀 SCR-002 初回セットアップ（`/onboarding`）: パスワードの変更 → 家の情報 → 完了。終わるまでほかの画面に進めない
- [x] 👤 SCR-003 プロフィール（`/profile`）: 表示名・テーマ・パスワード。上部バーに表示名
- [x] ⚙️ SCR-301 設定トップ（`/config`）: 全体設定・システム情報へのメニュー
- [x] 🌍 SCR-361 全体設定（`/config/general`）: 家の名前・位置・タイムゾーン・単位系・通貨・国
- [x] ℹ️ SCR-362 システム情報（`/config/info`）: 稼働時間と接続中の回線はその場で変わる
- [x] 🏠 概要の見出しを家の名前に（HA の概要と同じ）
- [x] 🕒 時刻の表示を全体設定のタイムゾーンに合わせる（タイル・詳細・イベント）
- [x] 🌙 テーマ「自動」が開いたままの OS の切り替えに追従しなかった → `ObserveSystemDarkModeChange` を「自動」のときだけ有効にして直した（MudBlazor は見張りが有効なときしか知らせない）
- [x] 🔠 上部バーの表示名が大文字になっていた（MudButton の既定）→ 大文字にしない
- [x] ➡️ CARD-02 エンティティ一覧は P4 に回した（使う画面がダッシュボード）

### 🧪 テスト

- [x] 単体: 130 件（新規 51: 入力の決まり・タイムゾーン・各画面・初回セットアップへの誘導・テーマの自動の追従）
- [x] 結合（当時は PostgreSQL 18）: 11 件（新規 6: パスワードの変更・楽観ロック・JSONB・`reload_core_config`・初回セットアップの記録・DB の情報）
- [x] E2E（Playwright）: 2 件（初回ログイン → 初回セットアップ → 概要）

### 🌐 ブラウザでの確認（当時は使い捨ての PostgreSQL コンテナ）

- [x] 🚀 初回セットアップ: 今のパスワード違い → エラー、変更 → 家の情報 → 完了 → 概要（見出し「🏠 我が家」）
- [x] 🔒 ログイン済みでも初回セットアップが済むまでは `/onboarding` に戻される
- [x] 👤 プロフィール: 表示名を保存すると上部バーがすぐ変わる
- [x] 🌙 テーマ: ライト／ダークがすぐ変わる、「自動」は開いたときの OS の設定に合う
- [x] 🌗 テーマ「自動」: 開いたまま OS を切り替えても追従する。「ライト」を選んでいれば OS がダークでもライトのまま
- [x] ⚙️ 設定トップ → 全体設定: 家の名前とタイムゾーン（Europe/London）を保存 → 概要の見出しと時刻の表示が変わる
- [x] 📣 全体設定を保存すると、別のタブの開発者ツールに `core_config_updated` が届く（変えた 2 項目と操作者）
- [x] ℹ️ システム情報: タブを増やすと接続中の回線がその場で増える。閉じたタブは「再接続待ち」（Blazor が 3 分残す）。稼働時間は 1 秒ごとに進む

### 📝 ドキュメント・コミット

- [x] 📝 README（使える画面）・06（実装した形）・07（画面の実装状況）・08（サービス・イベント）・09（`core.Setting` のキー・`Preferences`・JSONB）
- [x] 🧹 クリーンビルドで警告 0・エラー 0、単体 130・結合 11・E2E 2 がすべて成功
- [x] 💾 コミット（ブランチ `feature/p1-settings` の `7e9d740`）→ main に早送りでマージ → push（2026-11-02）

### 📝 気づいたこと

- 🧪 E2E の終了時に `ObjectDisposedException` のログが出る: サーバーを止める途中（`Worker stop` の後）に概要が描き直されるため。P1 の前半からあり、テストは成功する
- 🌐 ふだんの開発用 DB は SQLite のファイル（Host の実行フォルダーの `data/homemanager.db`）になった → 最初に起動したときに作られ、ログインすると初回セットアップが出る

---

## ✅ 済んだもの

### 🌱 P1 の前半（2026-11-01・コミット `4b56081`）

- [x] 📣 EventBus（購読ごとの `Channel<T>`・溢れたら古いものを捨てる）
- [x] 🗃️ StateMachine（`state_changed` / `state_reported`・Context で「誰が変えたか」）
- [x] 🛎️ ServiceRegistry（`call_service`・入力の誤りは画面に出す）
- [x] 💡 エンティティ 7 ドメイン（light・switch・sensor・binary_sensor・button・number・select）と EntityManager
- [x] 🧩 インテグレーション基盤（`IIntegration`・`IntegrationManager`・`IntegrationWorker`）
- [x] 🧪 仮想デバイス 14 台（`HomeManager.Integration.Demo`）
- [x] 🏠 概要・🪟 エンティティ詳細・🔲 タイル
- [x] 🛠️ 開発者ツール（状態・アクション・イベント）
- [x] 🎯 **完了の目安「ブラウザ 2 枚で片方の操作がもう片方に即座に反映」を確認**

### 🏗️ P0（2026-10-31・コミット `303e303`）

- [x] `template-blazor-server` から派生・見本の機能を削除
- [x] DB を PostgreSQL に変更・番号付き SQL のマイグレーション
- [x] Aspire（Host + PostgreSQL + pgweb + Mosquitto）・配置用ファイル
- [x] PostgreSQL 18 での結合テストと Aspire での起動を確認

---

## 🔜 これから（P2 以降の大きな項目）

### 🔌 P2 手持ちの機器がつながる

- [ ] ⚙️ 接続設定（Config Entry）と追加のウィザード（Config Flow）
- [ ] 🗂️ レジストリ（デバイス・エンティティ・エリア・フロア・ラベル）
- [ ] 🧹 P1 の見直しで回したもの: エンティティの書き込みの順序（MQTT と画面の操作が重なるとき）・homemanager のサービスの登録場所を 1 つに・状態の購読の書き方の共通化・詳細ダイアログと開発者ツールのテスト・`DatabaseService` の守りのテスト
- [ ] 📡 SwitchBot → ワットチェッカー → オムロン → Ping（MQTT は機器があれば）
- [ ] 📶 Bluetooth 診断
- [ ] 🏠 概要をエリアごとに並べる

### 🤖 P3 家が自動で動く

- [ ] 🤖 オートメーション（トリガー・条件・アクション）とトレース
- [ ] 📜 スクリプト・🎬 シーン・🎛️ ヘルパー

### 📈 P4 過去が見える

- [ ] 📜 履歴・📒 ログブック・📊 統計
- [ ] 📅 履歴を日ごとのファイルに分けて切り替える案を比べる（1 ファイル + 少しずつ DELETE と比べる。[12 §5](12-db-review.md)）
- [ ] 🧱 ダッシュボードの編集・📋 CARD-02 エンティティ一覧（P1 から移した）・⚡ エネルギー
- [ ] 📤 Prometheus への出力

### 🔐 P5 外から安全に使える

- [ ] 🌐 REST API・SignalR・長期アクセストークン
- [ ] 👥 ユーザー管理・💾 バックアップ・📜 ログ
- [ ] 🍓 Raspberry Pi で常駐

### 🚀 P6 好きな方向に伸ばす

- [ ] 天気・ブループリント・修復 など（[05 §4](05-feature-scope.md)）
