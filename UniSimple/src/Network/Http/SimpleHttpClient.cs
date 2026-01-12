using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace UniSimple.Network.Http
{
    public class SimpleHttpException : Exception
    {
        public long Code { get; }
        public string Body { get; }

        public SimpleHttpException(string message, long code, string body) : base(message)
        {
            Code = code;
            Body = body;
        }
    }

    public class SimpleHttpClient
    {
        public int DefaultTimeout { get; set; } = 10;
        private readonly Dictionary<string, string> _headers = new(StringComparer.OrdinalIgnoreCase);

        public void SetHeaders(Dictionary<string, string> headers)
        {
            _headers.Clear();
            foreach (var (name, value) in headers)
            {
                _headers[name] = value;
            }
        }

        public void SetHeader(string name, string value)
        {
            _headers[name] = value;
        }

        public async UniTask<string> GetAsync(string url, CancellationToken token = default)
        {
            return await SendRequestAsync(url, "GET", null, token);
        }

        public async UniTask<string> PostAsync(string url, string jsonData, CancellationToken token = default)
        {
            return await SendRequestAsync(url, "POST", jsonData, token);
        }

        public async UniTask<string> PutAsync(string url, string jsonData, CancellationToken token = default)
        {
            return await SendRequestAsync(url, "PUT", jsonData, token);
        }

        public async UniTask<string> DeleteAsync(string url, string jsonData = null, CancellationToken token = default)
        {
            return await SendRequestAsync(url, "DELETE", jsonData, token);
        }

        #region 内部方法

        private async UniTask<string> SendRequestAsync(
            string url,
            string method,
            string jsonData = null,
            CancellationToken token = default)
        {
            using var request = new UnityWebRequest(url, method);
            request.timeout = DefaultTimeout;
            request.downloadHandler = new DownloadHandlerBuffer();

            foreach (var (name, value) in _headers)
            {
                request.SetRequestHeader(name, value);
            }

            if (!string.IsNullOrEmpty(jsonData))
            {
                var bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);

                if (string.IsNullOrEmpty(request.GetRequestHeader("Content-Type")))
                {
                    request.uploadHandler.contentType = "application/json";
                    request.SetRequestHeader("Content-Type", "application/json");
                }
            }

            await request.SendWebRequest().WithCancellation(token);
            return HandleResponse(request);
        }

        private static string HandleResponse(UnityWebRequest request)
        {
            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError ||
                request.result == UnityWebRequest.Result.DataProcessingError)
            {
                var error = $"{request.method} {request.url}\nCode: {request.responseCode}\nMsg: {request.error}\nBody: {request.downloadHandler?.text}";
                Debug.LogError($"[HTTP Error] {error}");
                throw new SimpleHttpException(request.error, request.responseCode, request.downloadHandler?.text);
            }

            return request.downloadHandler.text;
        }

        #endregion
    }
}