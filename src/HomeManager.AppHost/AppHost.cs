// ReSharper disable StringLiteralTypo
var builder = DistributedApplication.CreateBuilder(args);

// データベース(PostgreSQL)。データはボリュームに残し、pgwebで中身を見られるようにする
// イメージは結合テスト(Testcontainers)と本番(deploy/compose.yaml)に合わせて18-alpineにする
var postgres = builder.AddPostgres("postgres")
    .WithImageTag("18-alpine")
    .WithDataVolume()
    .WithPgWeb();
var database = postgres.AddDatabase("database", "homemanager");

// MQTTブローカー(Mosquitto)。機器との接続はP2から使う
var mqtt = builder.AddMosquitto("mqtt")
    .WithDataVolume();

// 接続文字列はConnectionStrings__Default / ConnectionStrings__mqttとして注入され、appsettingsの既定値を上書きする
builder.AddProject<Projects.HomeManager_Host>("homemanager")
    .WithReference(database, connectionName: "Default")
    .WithReference(mqtt)
    .WaitFor(database)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
