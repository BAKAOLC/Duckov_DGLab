using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DGLabCSharp;
using DGLabCSharp.Enums;
using DGLabCSharp.Structs;
using UnityEngine;

namespace Duckov_DGLab
{
    // ReSharper disable once InconsistentNaming
    public class DGLabController : IDisposable
    {
        private readonly object _lock = new();
        private readonly List<CancellationTokenSource> _sendingTasks = [];
        private DGLabCSharp.DGLabController? _controller;
        private bool _disposed;
        private DGLabWebSocketServer? _server;

        public int StrengthA { get; private set; }

        public int StrengthB { get; private set; }

        public bool IsInitialized { get; private set; }

        public bool HasConnectedApps => _controller?.GetConnectedApps().Count > 0;

        public int ConnectedAppsCount => _controller?.GetConnectedApps().Count ?? 0;

        // ReSharper disable once InconsistentNaming
        public string QRCodePath { get; private set; } = "";

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                if (_server is not null)
                {
                    // ReSharper disable once AsyncApostle.AsyncWait
                    _server.StopAsync().Wait(TimeSpan.FromSeconds(5));
                    _server.Dispose();
                }

                IsInitialized = false;
                _disposed = true;

                ModLogger.Log("DGLabController disposed.");
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Error disposing DGLabController: {ex.Message}");
            }
        }

        public async Task<bool> InitializeAsync(int port = 9999)
        {
            if (IsInitialized)
            {
                ModLogger.LogWarning("DGLabController is already initialized.");
                return true;
            }

            try
            {
                if (!DGLabWebSocketServer.IsPortAvailable(port))
                {
                    ModLogger.LogWarning($"Port {port} is not available. Finding an available port...");
                    port = DGLabWebSocketServer.FindAvailablePort();
                    if (port == -1)
                    {
                        ModLogger.LogError("No available ports found.");
                        return false;
                    }

                    ModLogger.Log($"Using available port {port}.");
                }

                _server = new(port);
                _controller = new(_server);

                SubscribeToServerEvents();

                await _server.StartAsync().ConfigureAwait(false);

                var localIP = NetworkUtils.GetLocalIPAddress();
                if (!string.IsNullOrEmpty(localIP))
                {
                    ModLogger.Log($"DGLab WebSocket Server started at ws://{localIP}:{port}");
                    ModLogger.Log($"Controller ID: ${_server.ControllerClientId}");
                    ModLogger.Log($"Connect URL: ws://{localIP}:{port}/{_server.ControllerClientId}");

                    try
                    {
                        QRCodePath = Path.Combine(Application.persistentDataPath, "dglab_qr.png");
                        QRCodeGenerator.GenerateConnectionQRFile(localIP, port, _server.ControllerClientId, QRCodePath);
                        ModLogger.Log($"QR code generated at: {QRCodePath}");
                    }
                    catch (Exception ex)
                    {
                        ModLogger.LogError($"Error generating QR code: {ex.Message}");
                        try
                        {
                            QRCodeGenerator.GenerateConnectionQRFile(localIP, port, _server.ControllerClientId);
                            QRCodePath = Path.Combine(Directory.GetCurrentDirectory(), "dglab_connection_qr.png");
                            ModLogger.Log($"QR code generated at current directory: {QRCodePath}");
                        }
                        catch (Exception innerEx)
                        {
                            ModLogger.LogError($"Failed to generate QR code in current directory: {innerEx.Message}");
                        }
                    }
                }
                else
                {
                    ModLogger.LogError("Failed to retrieve local IP address.");
                    ModLogger.Log($"Connect URL: ws://YOUR_IP:{port}/{_server.ControllerClientId}");
                }

                IsInitialized = true;
                ModLogger.Log("DGLabController initialized successfully.");
                ModLogger.Log("Waiting for DGLab apps to connect...");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Error initializing DGLabController: {ex.Message}");
                return false;
            }
        }

        private void SubscribeToServerEvents()
        {
            if (_server is null) return;

            _server.ClientConnected += (_, args) =>
            {
                var (clientId, type, ipAddress, port) = args;
                ModLogger.Log($"New {type} connected - ID: {clientId[..8]}... From {ipAddress}:{port}");
            };

            _server.ClientDisconnected += (_, clientId) =>
            {
                ModLogger.Log($"Client disconnected - ID: {clientId[..8]}...");
            };

            _server.ServerError += (_, ex) => { ModLogger.LogError($"Server error: {ex.Message}"); };

            _server.ErrorOccurred += (_, args) =>
            {
                var (clientId, ex) = args;
                ModLogger.LogError($"Error with client {clientId[..8]}... : {ex.Message}");
            };

            _server.MessageHandler.BindingSucceeded += (o, args) =>
            {
                var (clientId, message) = args;
                ModLogger.Log($"Binding succeeded for client {clientId[..8]}... : {message}");
                _ = ApplyCurrentStrengthToAppAsync(clientId);
            };

            _server.MessageHandler.BindingFailed += (_, args) =>
            {
                var (clientId, message) = args;
                ModLogger.LogError($"Binding failed for client {clientId[..8]}... : {message}");
            };
        }

        private async Task ApplyCurrentStrengthToAppAsync(string appId)
        {
            if (!IsInitialized || _server is null || _controller is null) return;

            try
            {
                var apps = _controller.GetConnectedApps();
                var app = apps.FirstOrDefault(a => a.Id == appId);
                if (app == null) return;

                var boundApps = _server.ClientManager.GetControllerBoundApps(_server.ControllerClientId);
                if (!boundApps.Any(boundApp => boundApp.Contains(appId))) return;

                await Task.Delay(500).ConfigureAwait(false);

                var taskA = _controller.SetStrengthAsync(appId, Channel.A, StrengthA);
                var taskB = _controller.SetStrengthAsync(appId, Channel.B, StrengthB);
                await Task.WhenAll(taskA, taskB).ConfigureAwait(false);

                ModLogger.Log(
                    $"Applied current strength (A: {StrengthA}, B: {StrengthB}) to newly connected app {appId[..8]}...");
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Error applying current strength to app {appId[..8]}...: {ex.Message}");
            }
        }

        public async Task SetStrengthAsync(Channel channel, int strength)
        {
            strength = Math.Clamp(strength, 0, 100);
            switch (channel)
            {
                case Channel.A:
                    StrengthA = strength;
                    break;
                case Channel.B:
                    StrengthB = strength;
                    break;
            }

            if (!IsInitialized || _server is null || _controller is null) return;

            var apps = _controller.GetConnectedApps();
            if (apps.Count == 0) return;

            try
            {
                var targetApps = apps.Where(app =>
                    _server.ClientManager.GetControllerBoundApps(_server.ControllerClientId)
                        .Any(boundApp => boundApp.Contains(app.Id))).ToArray();
                if (targetApps.Length == 0) return;

                var tasks = targetApps.Select(app => _controller.SetStrengthAsync(app.Id, channel, strength))
                    .ToArray();
                await Task.WhenAll(tasks).ConfigureAwait(false);

                ModLogger.Log(
                    $"Set strength to {strength} for channel {channel} on {targetApps.Length} connected apps.");
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Error setting strength for channel {channel}: {ex.Message}");
            }
        }

        public async Task<bool> SendCustomWaveAsync(string waveDataJson, Channel channel,
            int duration = 3000, int punishmentTimesInSecond = 1)
        {
            if (!IsInitialized || _server is null || _controller is null)
            {
                ModLogger.LogError("DGLabController is not initialized.");
                return false;
            }

            var apps = _controller.GetConnectedApps();
            if (apps.Count == 0)
            {
                ModLogger.LogWarning("No connected DGLab apps to send wave to.");
                return false;
            }

            var boundApps = _server.ClientManager.GetControllerBoundApps(_server.ControllerClientId);
            if (boundApps.Count == 0)
            {
                ModLogger.LogWarning("No bound DGLab apps to send wave to.");
                return false;
            }

            try
            {
                var targetApps = apps.Where(app => boundApps.Any(boundApp => boundApp.Contains(app.Id))).ToArray();
                if (targetApps.Length == 0)
                {
                    ModLogger.LogWarning("No bound DGLab apps found among connected apps.");
                    return false;
                }

                var messageContent = $"{channel.ToChannelString()}:{waveDataJson}";

                var timeSpace = 1000.0 / punishmentTimesInSecond;
                var totalSends = Convert.ToInt32(Math.Max(1, Math.Floor(duration / timeSpace)));

                var successCount = 0;
                var ctx = new CancellationTokenSource();
                lock (_lock)
                {
                    _sendingTasks.Add(ctx);
                }

                var tasks = targetApps.Select(app => Task.Run(async () =>
                {
                    var message = new ClientMessage(messageContent, duration, channel,
                        _server.ControllerClientId, app.Id);
                    for (var i = 0; i < totalSends; i++)
                    {
                        try
                        {
                            await _server.SendMessageToClientAsync(app.Id, message).ConfigureAwait(false);
                            if (i == 0)
                            {
                                Interlocked.Increment(ref successCount);
                                ModLogger.Log(
                                    $"Successfully sent custom wave to app {app.Id[..8]}... on {channel} channel for {duration} ms");
                            }
                        }
                        catch (Exception ex)
                        {
                            ModLogger.LogError(
                                $"Error sending custom wave to app {app.Id[..8]}...: {ex.Message}");
                            break;
                        }

                        if (i < totalSends - 1) await Task.Delay((int)timeSpace, ctx.Token).ConfigureAwait(false);
                    }
                }, ctx.Token)).ToArray();

                await Task.WhenAll(tasks).ConfigureAwait(false);

                lock (_lock)
                {
                    _sendingTasks.Remove(ctx);
                }

                return successCount > 0;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Error sending custom wave: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendWaveAsync(WaveType waveType, Channel channel,
            int duration = 3000, int punishmentTimesInSecond = 1)
        {
            var waveDataJson = WaveData.GetWaveDataJson(waveType);
            return await SendCustomWaveAsync(waveDataJson, channel, duration, punishmentTimesInSecond)
                .ConfigureAwait(false);
        }

        public async Task<bool> SendCustomWaveToAllChannelsAsync(string waveDataJson,
            int duration = 3000, int punishmentTimesInSecond = 1)
        {
            var taskA = SendCustomWaveAsync(waveDataJson, Channel.A, duration, punishmentTimesInSecond);
            var taskB = SendCustomWaveAsync(waveDataJson, Channel.B, duration, punishmentTimesInSecond);

            var results = await Task.WhenAll(taskA, taskB).ConfigureAwait(false);
            return results.Any(result => result);
        }

        public async Task<bool> SendWaveToAllChannelsAsync(WaveType waveType,
            int duration = 3000, int punishmentTimesInSecond = 1)
        {
            var taskA = SendWaveAsync(waveType, Channel.A, duration, punishmentTimesInSecond);
            var taskB = SendWaveAsync(waveType, Channel.B, duration, punishmentTimesInSecond);

            var results = await Task.WhenAll(taskA, taskB).ConfigureAwait(false);
            return results.Any(result => result);
        }

        public async Task<bool> EmergencyStopAsync()
        {
            lock (_lock)
            {
                _sendingTasks.ForEach(cts => cts.Cancel());
                _sendingTasks.Clear();
            }

            if (!IsInitialized || _server is null || _controller is null) return false;

            var apps = _controller.GetConnectedApps();
            if (apps.Count == 0) return false;

            try
            {
                var tasks = new List<Task<bool>>();

                foreach (var app in apps)
                {
                    var boundApps = _server.ClientManager.GetControllerBoundApps(_server.ControllerClientId);
                    if (!boundApps.Contains(app.Id)) continue;

                    var clearA = new WebSocketMessage
                    {
                        Type = MessageType.Msg.ToTypeString(),
                        ClientId = _server.ControllerClientId,
                        TargetId = app.Id,
                        Message = "clear-1",
                    };
                    var clearB = new WebSocketMessage
                    {
                        Type = MessageType.Msg.ToTypeString(),
                        ClientId = _server.ControllerClientId,
                        TargetId = app.Id,
                        Message = "clear-2",
                    };

                    var strengthA = new StrengthMessage(StrengthOperationType.SetToZero, (int)Channel.A, 0,
                        _server.ControllerClientId, app.Id);
                    var strengthB = new StrengthMessage(StrengthOperationType.SetToZero, (int)Channel.B, 0,
                        _server.ControllerClientId, app.Id);

                    tasks.Add(_server.SendMessageToClientAsync(app.Id, clearA));
                    tasks.Add(_server.SendMessageToClientAsync(app.Id, clearB));
                    tasks.Add(_server.SendMessageToClientAsync(app.Id, strengthA));
                    tasks.Add(_server.SendMessageToClientAsync(app.Id, strengthB));
                }

                var results = await Task.WhenAll(tasks).ConfigureAwait(false);
                var success = Array.TrueForAll(results, result => result);

                if (success)
                    ModLogger.Log("Emergency stop sent to all connected apps successfully.");
                else
                    ModLogger.LogWarning("Emergency stop sent to some apps, but failed for others.");

                return success;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Error during emergency stop: {ex.Message}");
                return false;
            }
        }

        public string GetStatusInfo()
        {
            if (!IsInitialized || _server is null || _controller is null)
                return "DGLabController is not initialized.";

            var apps = _controller.GetConnectedApps();
            var totalClients = _server.ClientManager.GetActiveClientCount();

            return $"Server Port: {_server.Port}, Active Clients: {totalClients}, Connected DGLab Apps: {apps.Count}";
        }
    }
}