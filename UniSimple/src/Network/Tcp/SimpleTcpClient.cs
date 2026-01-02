using System;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UniSimple.Network.Tcp
{
    public class SimpleTcpClient : IDisposable
    {
        // 发送锁：防止多线程同时调用 SendAsync 导致 socket 数据写入交错
        private readonly SemaphoreSlim _sendLock = new(1, 1);

        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;

        public bool IsConnected => _tcpClient?.Connected == true && _stream != null;
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<byte[]> OnDataReceived;

        public async UniTask ConnectAsync(string ip, int port)
        {
            Close();

            try
            {
                _tcpClient = new TcpClient();
                _tcpClient.ReceiveBufferSize = 64 * 1024;
                _tcpClient.SendBufferSize = 64 * 1024;
                _tcpClient.NoDelay = true;

                _cts = new CancellationTokenSource();

                await _tcpClient.ConnectAsync(ip, port);
                // var connectTask = _tcpClient.ConnectAsync(ip, port).AsUniTask();
                // var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(5), cancellationToken: _cts.Token);
                // var result = await UniTask.WhenAny(connectTask, timeoutTask);
                // if (result == 1)
                //     throw new TimeoutException("Connect timeout!");

                _stream = _tcpClient.GetStream();
                Debug.Log($"[TCP] Connect to {ip}:{port}");
                OnConnected?.Invoke();

                ReceiveLoop(_cts.Token).Forget();
            }
            catch (Exception e)
            {
                Debug.LogError($"[TCP] Connect error: {e.Message}");
                Close();
                throw;
            }
        }

        private async UniTaskVoid ReceiveLoop(CancellationToken token)
        {
            // 确保接收逻辑在后台线程跑，避免解析大量数据卡顿 UI
            await UniTask.SwitchToThreadPool();

            try
            {
                while (!token.IsCancellationRequested)
                {
                    // 读取数据
                    byte[] data = await _stream.ReceiveMessageAsync(token);
                    if (data == null)
                        break; // 远程断开

                    OnDataReceived?.Invoke(data);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                // 忽略对象被释放的错误
                if (_cts != null && !_cts.IsCancellationRequested)
                    Debug.LogWarning($"[TCP] Receive loop warning: {e.Message}");
            }
            finally
            {
                // 切回主线程退出
                await UniTask.SwitchToMainThread();
                Close();
                OnDisconnected?.Invoke();
                Debug.Log("[TCP] Disconnected");
            }
        }

        public async UniTask SendAsync(byte[] data)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            try
            {
                await _sendLock.WaitAsync();
                try
                {
                    await _stream.SendMessageAsync(data);
                }
                finally
                {
                    _sendLock.Release();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TCP] Failed to send: {e.Message}");
                Close();
                throw;
            }
        }

        public void Close()
        {
            if (_cts == null)
                return; // 防止重复调用

            try
            {
                _cts?.Cancel();
                _tcpClient?.Close(); // Close 会自动 Dispose Stream
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TCP] Closing warning: {ex.Message}");
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
                _stream = null;
                _tcpClient = null;
            }
        }

        public void Dispose()
        {
            Close();
            _sendLock?.Dispose();
        }
    }
}