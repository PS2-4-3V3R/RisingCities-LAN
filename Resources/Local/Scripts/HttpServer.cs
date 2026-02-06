using System.IO;
using System.Net;
using System.Text.Json;

//namespace WpfServerApp.Server
namespace RisingCitiesOffline.Resources.Local.Scripts
{
    public class HttpServer
    {
        private HttpListener? _listener;
        private CancellationTokenSource? _cts;

        public void Start()
        {
            if (_listener != null)
                return;

            _cts = new CancellationTokenSource();
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:8000/");
            //_listener.Prefixes.Add("http://*:8000/");
            _listener.Start();

            Task.Run(() => ListenLoop(_cts.Token));
        }

        public void Stop()
        {
            try
            {
                _cts?.Cancel();
                _listener?.Stop();
                _listener = null;
            }
            catch { }
        }

        private async Task ListenLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                HttpListenerContext ctx;

                try
                {
                    ctx = await _listener!.GetContextAsync();
                }
                catch
                {
                    break;
                }

                _ = Task.Run(() => HandleRequest(ctx));
            }
        }

        private async Task HandleRequest(HttpListenerContext ctx)
        {
            string path = ctx.Request.Url!.AbsolutePath.TrimStart('/');

            // Caso especial RCApi
            if (path.StartsWith("RCApi"))
            {
                ctx.Response.ContentType = "application/json";
                await JsonSerializer.SerializeAsync(ctx.Response.OutputStream,
                    new { host = "127.0.0.1", port = 8080 });
                ctx.Response.Close();
                return;
            }

            // Quitar query params
            path = path.Split("?")[0];

            string filePath = Path.Combine("templates", path);

            if (File.Exists(filePath))
            {
                byte[] data = await File.ReadAllBytesAsync(filePath);
                ctx.Response.ContentType = GetMimeType(filePath);
                ctx.Response.ContentLength64 = data.Length;
                await ctx.Response.OutputStream.WriteAsync(data);
            }
            else
            {
                ctx.Response.StatusCode = 404;
                using var writer = new StreamWriter(ctx.Response.OutputStream);
                await writer.WriteAsync("Not Found");
            }

            ctx.Response.Close();
        }

        private string GetMimeType(string file)
        {
            string ext = Path.GetExtension(file).ToLower();

            return ext switch
            {
                ".html" => "text/html",
                ".htm" => "text/html",
                ".js" => "application/javascript",
                ".css" => "text/css",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".json" => "application/json",
                _ => "application/octet-stream"
            };
        }
    }
}
