// ReSharper disable StringLiteralTypo
var builder = DistributedApplication.CreateBuilder(args);

// MQTTブローカー(Mosquitto)。機器との接続はP2から使う
var mqtt = builder.AddMosquitto("mqtt")
    .WithDataVolume();

// DBはSQLite(Hostのプロセスの中)なので起動しない。DBファイルはappsettingsの既定値(data/homemanager.db)
// MQTTの接続文字列はConnectionStrings__mqttとして注入される
builder.AddProject<Projects.HomeManager_Host>("homemanager")
    .WithReference(mqtt)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
