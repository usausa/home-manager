# 🗄️ 12. DB の見直し（PostgreSQL は必要か）

> 📌 **きっかけ**: P1 の締めの見直し（2026-11-02）で「PostgreSQL が必要なのかも見直す。**SQLite で済む範囲であるのなら、ポータブルな方向に方針を変える**」
> 🔗 関連: [06 §5・§6-5](06-dotnet-architecture.md)（DB を選んだ理由と保存設計）・[09](09-db-design.md)（DB 設計）・[02](02-ha-core-architecture.md)（HA の Recorder）・[04](04-template-survey.md)（テンプレートは SQLite）

---

## ⚡ 結論

| # | ポイント |
|:-:|---|
| ✅ | **SQLite で済む**。家 1 軒・アプリ 1 プロセス・履歴は 1 日 30 万行ほど（P4）で、SQLite（WAL）の得意な範囲に収まる |
| 🏠 | お手本の **Home Assistant も既定は SQLite**（`home-assistant_v2.db`）。Raspberry Pi で何年も動いている構成と同じになる |
| 🎒 | **ポータブルになる**: DB のサーバーもコンテナも要らない。データは 1 つのファイル。テストもコンテナ無しで動く |
| 💰 | **切り替えるなら今が一番安い**: P1 のテーブルは 3 つだけ（`SchemaVersion`・`Setting`・`Account`）。P2 で 10、P4 で履歴の 11 が増える前 |
| ⚠️ | **失うもの**: パーティションで古い履歴を一瞬で消す方法と TimescaleDB の道（→ HA と同じ「少しずつ DELETE + 時々 VACUUM」に変える）。PostgreSQL そのものの学習 |
| 👉 | **おすすめ: SQLite に切り替える**（データは `data/homemanager.db`。P4 の履歴は別ファイル `data/recorder.db`） |

---

## 📋 1. 要件ごとの比較

> SQLite は .NET の `Microsoft.Data.Sqlite` 10.0 に入っている **3.53.3**（2026-06）で比べる。Raspberry Pi（linux-arm64）のネイティブライブラリも同梱。

| 要件（どこで使うか） | 🐘 PostgreSQL（元の設計） | 🪶 SQLite 3.53 | 判定 |
|---|---|---|:-:|
| 🗂️ 設定・レジストリ・認証の表（core 27 表・小さい） | 普通の表・外部キー・UNIQUE | 同じ（外部キーは `PRAGMA foreign_keys = ON`） | ✅ |
| 🧾 JSON（設定・属性・自動化の定義・ダッシュボード） | `JSONB` | `TEXT` + JSON 関数（`json_extract`・`->>`）、バイナリの JSONB も 3.45 から | ✅ |
| 🔁 楽観ロック | `Version` 列 | 同じ | ✅ |
| 🕒 時刻 | `TIMESTAMPTZ` | `TEXT`（ISO 8601・UTC）。`Microsoft.Data.Sqlite` が `DateTime` を読み書きする | ✅ |
| 🆔 ID | `IDENTITY`・`uuidv7()` | `INTEGER PRIMARY KEY`・UUIDv7 は .NET の `Guid.CreateVersion7()` で作る | ✅ |
| 🧩 UPSERT・`RETURNING` | あり | あり（UPSERT は 3.24、`RETURNING` は 3.35 から） | ✅ |
| 🗂️ スキーマの分割（`core` / `recorder`） | スキーマ | **ファイルを分ける**（`homemanager.db` / `recorder.db`）。書き込みのロックも別々になる | ✅ |
| 🧬 マイグレーション | DDL もトランザクションで巻き戻せる・アドバイザリーロック | DDL もトランザクションで巻き戻せる。1 プロセスなのでロックは要らない | ✅ |
| ✍️ 履歴の書き込み（P4） | 5 秒ごとにまとめて INSERT | 同じ。平均は毎秒 3〜4 行（1 日 30 万行）。まとめて 1 トランザクションなら余裕 | ✅ |
| 🗑️ 保持期間を過ぎた履歴（P4） | 日ごとのパーティションを DROP（一瞬） | **少しずつ DELETE + 時々 VACUUM**（HA の purge / repack と同じ） | 🟡 やり方が変わる |
| 👥 同時に書く | 何本でも（MVCC） | 書くのは 1 本ずつ（WAL なら読むのは止まらない）。アプリ 1 プロセスなので、書き込みをアプリの中で並べれば足りる | ✅ |
| 💾 バックアップ | `pg_dump` | **ファイルをコピー**（`VACUUM INTO` か `SqliteConnection.BackupDatabase`。動かしたままでよい） | ✅ もっと簡単 |
| 🔍 中身を見る道具 | pgweb・psql | DB Browser for SQLite・`sqlite3` | ✅ |
| 📈 Grafana | Prometheus 経由（DB は見ない。[10](10-prometheus-exporter.md)） | 同じ | ✅ |
| 🚀 TimescaleDB（P6 の発展） | 使える | 使えない | ⛔ |
| 🌐 ほかのマシンから DB に直接つなぐ・アプリを複数台 | できる | できない（ファイル） | — 家では不要 |

---

## 🎒 2. ポータブルになると何が変わるか

| 場面 | 🐘 PostgreSQL | 🪶 SQLite |
|---|---|---|
| 🍓 Raspberry Pi に置く | アプリ + PostgreSQL のコンテナ（メモリも食う） | **アプリだけ**（DB はプロセスの中） |
| 🧑‍💻 開発（Aspire） | PostgreSQL + pgweb のコンテナを起動 | DB のコンテナは無し（Mosquitto だけ起動。使うのは P2 から） |
| 🧪 結合テスト・E2E | Testcontainers（Docker / Podman が要る。無ければスキップ） | **一時ファイルの DB**（どこでも動く。テンプレートと同じ） |
| 💾 引っ越し・バックアップ | `pg_dump` / `pg_restore` | `data/` フォルダーをコピー |
| 🔐 秘密 | DB のパスワードを環境変数で渡す | DB のパスワードは無い（ファイルの権限で守る） |

---

## ⚠️ 3. SQLite で気をつけること

| 点 | 対策 |
|---|---|
| ✍️ 書くのは 1 本ずつ | WAL モード + `busy_timeout`。履歴はキューにためて 5 秒ごとにまとめて書く（HA の `commit_interval`）。長いトランザクションを作らない |
| ⚡ 停電 | WAL + `synchronous = NORMAL` なら**壊れない**（最後の数秒が消えることはある）。SD カードより **SSD**（PostgreSQL でも同じ） |
| 📦 消してもファイルが縮まない | `auto_vacuum = INCREMENTAL` で少しずつ返す。月 1 回くらい `VACUUM`（HA の `auto_repack`） |
| 🕒 時刻の型が無い | すべて **UTC の ISO 8601 文字列**にそろえる（並べ替えも比べるのも文字列のままでよい） |
| 🧮 統計の集計 SQL | `::DOUBLE PRECISION`・`INTERVAL`・`pg_input_is_valid` は使えない → `CAST`・時刻は .NET で計算して渡す |
| 🔤 型がゆるい | 表は **`STRICT`**（3.37 から）で作り、違う型の値を入れたらエラーにする |

---

## 🛠️ 4. 切り替えでやったこと（2026-11-02）

| 範囲 | やったこと |
|---|---|
| 🖥️ Host | `Npgsql` → `Microsoft.Data.Sqlite`。接続文字列は `Data Source=data/homemanager.db;Foreign Keys=True`（外部キーは接続文字列で有効にする）。起動時に DB ファイルのフォルダーを作る。一意制約の違反の判定はテンプレートの `SqliteException` の判定 |
| 🧬 マイグレーション | 起動時に `journal_mode = WAL`（DB ファイルに残る）。`0001_core_initial.sql` を SQLite 用（`STRICT`・`json_valid`）に書き直した。スキーマの接頭辞 `core.` を外した。1 プロセスなのでロックは取らない（アドバイザリーロックは削除） |
| 🧾 SQL | `CAST(… AS JSONB)` を外した。DB の大きさは `page_count × page_size`、版は `sqlite_version()`、場所は `pragma_database_list` |
| 🧪 テスト | 結合・E2E は Testcontainers をやめて一時ファイル（テンプレートと同じ）。E2E は Playwright だけが要る |
| 🚀 Aspire | PostgreSQL・pgweb を外した（Mosquitto は残す） |
| 📦 配置 | `compose.yaml` は Mosquitto だけ。`homemanager.env` の接続文字列は `/var/lib/homemanager/homemanager.db`。systemd の `StateDirectory` でフォルダーを作る |
| 📝 ドキュメント | [06](06-dotnet-architecture.md)・[09](09-db-design.md)・README・00・04・05・07・[11](11-progress.md)（残っていた古い記述は P1 の見直しで直した） |
| 🧹 開発用のデータ | Aspire の PostgreSQL のボリュームは使わなくなった（P1 の締めで確かめてから消す）。新しい DB では初回セットアップからやり直し |

> ⏱️ `busy_timeout` は設定していない（`Microsoft.Data.Sqlite` は書き込みが重なるとコマンドのタイムアウトまで待ち直す）。P4 で履歴を書き始めたら様子を見る。

---

## 🔮 5. 今後の検討事項

| 項目 | 内容 | いつ |
|---|---|:-:|
| 📅 **履歴を日ごとのファイルに分ける** | `recorder/2026-11-02.db` のように 1 日 1 ファイルにし、日付が変わったら新しいファイルに切り替える。保持期間を過ぎた日は**ファイルを消すだけ**（PostgreSQL のパーティションの DROP と同じ速さ・VACUUM も要らない）。古い日だけを別の場所に移して残すこともできる | P4 で履歴を作るときに比べる |
| 🔗 └ 気をつけること | 何日もまたがる検索は `ATTACH` でつなぐ（同時につなげる数には上限がある）・entity_id と属性の共有表（`EntityMeta`・`AttributeSet`）をどのファイルに置くか・0 時をまたぐ書き込みの切り替え・統計の集計 | |
| 🧩 └ 比べる相手 | 1 ファイル + 少しずつ DELETE + 時々 VACUUM（HA と同じ）。テンプレートの maui-server も端末ごとに時系列の SQLite を分けている（[04](04-template-survey.md)） | |

---

## 🔁 6. 決定

| 日付 | 決定 |
|---|---|
| 2026-11-02 | ✅ **SQLite に切り替える**（利用者の判断。データは `data/homemanager.db`、P4 の履歴は別のファイル。履歴を日ごとのファイルに分ける案は P4 で比べる） |
| 2026-11-02 | ✅ **切り替えた**: `Microsoft.Data.Sqlite`・`0001_core_initial.sql` を SQLite 用（`STRICT`・`json_valid`）に・結合と E2E は一時ファイル（コンテナ不要。結合テストは約 6 秒 → 1.6 秒）・Aspire と配置から PostgreSQL を外した。単体 130・結合 11・E2E 2 が成功、ブラウザでも確認（強制終了して再起動しても WAL から戻る） |
