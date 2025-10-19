using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DGLabCSharp;
using DGLabCSharp.Enums;
using DGLabCSharp.Structs;
using UnityEngine;

namespace Duckov_DGLab
{
    public class DGLabController : IDisposable
    {
        private DGLabCSharp.DGLabController? _controller;
        private bool _disposed;
        private DGLabWebSocketServer? _server;

        public bool IsInitialized { get; private set; }

        public bool HasConnectedApps => _controller?.GetConnectedApps().Count > 0;

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

                Debug.Log("DGLabController disposed.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error disposing DGLabController: {ex.Message}");
            }
        }

        public async Task<bool> InitializeAsync(int port = 9999)
        {
            if (IsInitialized)
            {
                Debug.LogWarning("DGLabController is already initialized.");
                return true;
            }

            try
            {
                if (!DGLabWebSocketServer.IsPortAvailable(port))
                {
                    Debug.LogWarning($"Port {port} is not available. Finding an available port...");
                    port = DGLabWebSocketServer.FindAvailablePort();
                    if (port == -1)
                    {
                        Debug.LogError("No available ports found.");
                        return false;
                    }

                    Debug.Log($"Using available port {port}.");
                }

                _server = new(port);
                _controller = new(_server);

                SubscribeToServerEvents();

                await _server.StartAsync();

                var localIP = NetworkUtils.GetLocalIPAddress();
                if (!string.IsNullOrEmpty(localIP))
                {
                    Debug.Log($"DGLab WebSocket Server started at ws://{localIP}:{port}");
                    Debug.Log($"Controller ID: ${_server.ControllerClientId}");
                    Debug.Log($"Connect URL: ws://{localIP}:{port}/{_server.ControllerClientId}");

                    try
                    {
                        QRCodePath = Path.Combine(Application.persistentDataPath, "dglab_qr.png");
                        QRCodeGenerator.GenerateConnectionQRFile(localIP, port, _server.ControllerClientId, QRCodePath);
                        Debug.Log($"QR code generated at: {QRCodePath}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error generating QR code: {ex.Message}");
                        try
                        {
                            QRCodeGenerator.GenerateConnectionQRFile(localIP, port, _server.ControllerClientId);
                            QRCodePath = Path.Combine(Directory.GetCurrentDirectory(), "dglab_connection_qr.png");
                            Debug.Log($"QR code generated at current directory: {QRCodePath}");
                        }
                        catch (Exception innerEx)
                        {
                            Debug.LogError($"Failed to generate QR code in current directory: {innerEx.Message}");
                        }
                    }
                }
                else
                {
                    Debug.LogError("Failed to retrieve local IP address.");
                    Debug.Log($"Connect URL: ws://YOUR_IP:{port}/{_server.ControllerClientId}");
                }

                IsInitialized = true;
                Debug.Log("DGLabController initialized successfully.");
                Debug.Log("Waiting for DGLab apps to connect...");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error initializing DGLabController: {ex.Message}");
                return false;
            }
        }

        private void SubscribeToServerEvents()
        {
            if (_server is null) return;

            _server.ClientConnected += (_, args) =>
            {
                var (clientId, type, ipAddress, port) = args;
                Debug.Log($"New {type} connected - ID: {clientId[..8]}... From {ipAddress}:{port}");
            };

            _server.ClientDisconnected += (_, clientId) =>
            {
                Debug.Log($"Client disconnected - ID: {clientId[..8]}...");
            };

            _server.ServerError += (_, ex) => { Debug.LogError($"Server error: {ex.Message}"); };

            _server.ErrorOccurred += (_, args) =>
            {
                var (clientId, ex) = args;
                Debug.LogError($"Error with client {clientId[..8]}... : {ex.Message}");
            };

            _server.MessageHandler.BindingSucceeded += (_, args) =>
            {
                var (clientId, message) = args;
                Debug.Log($"Binding succeeded for client {clientId[..8]}... : {message}");
            };

            _server.MessageHandler.BindingFailed += (_, args) =>
            {
                var (clientId, message) = args;
                Debug.LogError($"Binding failed for client {clientId[..8]}... : {message}");
            };
        }

        public async Task<bool> SendWaveAsync(WaveType waveType, Channel channel, int duration = 3)
        {
            if (!IsInitialized || _server is null || _controller is null)
            {
                Debug.LogError("DGLabController is not initialized.");
                return false;
            }

            var apps = _controller.GetConnectedApps();
            if (apps.Count == 0)
            {
                Debug.LogWarning("No connected DGLab apps to send wave to.");
                return false;
            }

            var boundApps = _server.ClientManager.GetControllerBoundApps(_server.ControllerClientId);
            if (boundApps.Count == 0)
            {
                Debug.LogWarning("No bound DGLab apps to send wave to.");
                return false;
            }

            try
            {
                var targetApps = apps.Where(app => boundApps.Any(boundApp => boundApp.Contains(app.Id))).ToArray();
                if (targetApps.Length == 0)
                {
                    Debug.LogWarning("No bound DGLab apps found among connected apps.");
                    return false;
                }

                const int punishmentTime = 1;
                var totalSends = punishmentTime * duration;
                const int timeSpace = 1000 / punishmentTime;

                var waveData = WaveData.GetWaveDataJson(waveType);
                var messageContent = $"{channel.ToChannelString()}:{waveData}";

                var successCount = 0;

                for (var i = 0; i < totalSends; i++)
                {
                    var tasks = targetApps.Select(app =>
                    {
                        var message = new ClientMessage(messageContent, duration, channel, _server.ControllerClientId,
                            app.Id);
                        return _server.SendMessageToClientAsync(app.Id, message);
                    }).ToArray();

                    var results = await Task.WhenAll(tasks).ConfigureAwait(false);
                    var currentSuccessCount = results.Count(result => result);

                    if (i == 0)
                    {
                        successCount = currentSuccessCount;
                        Debug.Log(
                            $"Sent {waveType} wave to {successCount}/{targetApps.Length} apps on {channel} channel for {duration} seconds");
                    }

                    if (i < totalSends - 1) await Task.Delay(timeSpace).ConfigureAwait(false);
                }

                return successCount > 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error sending wave: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendWaveToAllChannelsAsync(WaveType waveType, int duration = 3)
        {
            var taskA = SendWaveAsync(waveType, Channel.A, duration);
            var taskB = SendWaveAsync(waveType, Channel.B, duration);

            var results = await Task.WhenAll(taskA, taskB).ConfigureAwait(false);
            return results.Any(result => result);
        }

        public async Task<bool> EmergencyStopAsync()
        {
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
                    Debug.Log("Emergency stop sent to all connected apps successfully.");
                else
                    Debug.LogWarning("Emergency stop sent to some apps, but failed for others.");

                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during emergency stop: {ex.Message}");
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