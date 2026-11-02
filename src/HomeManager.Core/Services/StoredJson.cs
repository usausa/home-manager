namespace HomeManager.Services;

using System.Text.Json;

// DBに保存するJSON (全体設定・初回セットアップ・表示の設定) の書き方。キーはcamelCase
internal static class StoredJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web);
}
