using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;

// 需要 .NET Standard 2.1

namespace UniSimple.Network.Tcp
{
    public static class TcpExtension
    {
        // 增加最大包体限制 防止非法包导致 Out of memery
        private const int MAX_MESSAGE_SIZE = 5 * 1024 * 1024; // 5MB

        public static async UniTask SendMessageAsync(this NetworkStream stream, byte[] data)
        {
            // 申请一个 4字节的临时内存用于放包头，避免 new byte[4]
            // 实际上可以直接申请 4 + data.Length 的数组一次性发送，减少系统调用，但这里为了逻辑清晰分两步
            byte[] headerBuffer = ArrayPool<byte>.Shared.Rent(4);

            try
            {
                // 写入长度 (使用 BigEndian 网络标准序)
                BinaryPrimitives.WriteInt32BigEndian(headerBuffer, data.Length);

                // 写入包头
                await stream.WriteAsync(headerBuffer, 0, 4);
                // 写入包体
                if (data.Length > 0)
                {
                    await stream.WriteAsync(data, 0, data.Length);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(headerBuffer);
            }
        }

        public static async UniTask<byte[]> ReceiveMessageAsync(this NetworkStream stream, CancellationToken token)
        {
            // 读取4字节长度头
            byte[] headerBuffer = ArrayPool<byte>.Shared.Rent(4);
            int bodyLength;

            try
            {
                if (await ReadFullyAsync(stream, headerBuffer, 4, token) == 0)
                    return null;

                // 解析长度 (BigEndian)
                bodyLength = BinaryPrimitives.ReadInt32BigEndian(headerBuffer);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(headerBuffer);
            }

            if (bodyLength < 0 || bodyLength > MAX_MESSAGE_SIZE)
            {
                throw new Exception($"[TCP] The packet size is abnormal: {bodyLength}, disconnect to protect memory");
            }

            if (bodyLength == 0)
                return Array.Empty<byte>();

            byte[] dataBuffer = new byte[bodyLength];
            if (await ReadFullyAsync(stream, dataBuffer, bodyLength, token) == 0)
                return null;

            return dataBuffer;
        }

        private static async UniTask<int> ReadFullyAsync(NetworkStream stream, byte[] buffer, int goal, CancellationToken token)
        {
            int received = 0;
            while (received < goal)
            {
                int read = await stream.ReadAsync(buffer, received, goal - received, token);
                if (read == 0)
                    return 0; // 连接断开

                received += read;
            }

            return received;
        }
    }
}