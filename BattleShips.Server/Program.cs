// SITAM PROJEKTE B US SERVERIO PUSES LOGIKA
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BattleShips.Core;
using System;
using System.IO;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();

// Load environment variables from .env if present
var envFilePath = Path.Combine(builder.Environment.ContentRootPath, ".env");
if (File.Exists(envFilePath))
{
    DotNetEnv.Env.Load(envFilePath);
}

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";

app.MapHub<GameHub>("/game");
app.Urls.Add($"http://localhost:{port}");

Console.WriteLine($"BattleShips server listening on http://localhost:{port}/game");
await app.RunAsync();

public class GameHub : Hub
{
    public async Task Ping(string who) => await Clients.Caller.SendAsync("Pong", $"hi {who}");
    public async Task SendHello(HelloMessage msg)
    {
        Console.WriteLine($"Received from {msg.From}: {msg.Text}");
         
        await Clients.All.SendAsync("ReceiveHello", msg);
    }
}
