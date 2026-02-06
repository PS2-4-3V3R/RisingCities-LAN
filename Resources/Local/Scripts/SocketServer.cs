using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

//namespace WpfServerApp.Server
namespace RisingCitiesOffline.Resources.Local.Scripts
{
    public class SocketServer
    {
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        public bool IsRunning { get; private set; }

        public event Action<string>? Log;

        public void Start(int port = 8080)
        {
            if (IsRunning)
                return;

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            IsRunning = true;
            Log?.Invoke($"Combined socket+policy server running on port {port}");

            Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        var client = await _listener.AcceptTcpClientAsync(token);
                        _ = HandleClient(client, token);
                    }
                }
                catch (OperationCanceledException) { }
                finally
                {
                    IsRunning = false;
                    _listener.Stop();
                    Log?.Invoke("Socket server stopped");
                }
            }, token);
        }

        public void Stop()
        {
            try
            {
                _cts?.Cancel();
            }
            catch { }
        }

        private async Task HandleClient(TcpClient client, CancellationToken token)
        {
            using var stream = client.GetStream();
            var buffer = new byte[4096];

            int read = await stream.ReadAsync(buffer, 0, buffer.Length, token);
            if (read <= 0)
            {
                client.Close();
                return;
            }

            var peek = buffer[..read];
            var peekText = Encoding.UTF8.GetString(peek);

            if (peekText.Contains("<policy-file-request/>"))
            {
                string policy =
                    "<?xml version=\"1.0\"?>" +
                    "<cross-domain-policy>" +
                    "<allow-access-from domain=\"*\" to-ports=\"*\" />" +
                    "</cross-domain-policy>\0";

                byte[] bytes = Encoding.UTF8.GetBytes(policy);
                await stream.WriteAsync(bytes, token);
                await stream.FlushAsync(token);

                Log?.Invoke("Handled policy request");
                client.Close();
                return;
            }

            Log?.Invoke($"New socket connection from {client.Client.RemoteEndPoint}");

            var dataBuffer = new List<byte>(peek);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    while (dataBuffer.Count < 4)
                        dataBuffer.AddRange(await ReadMore(stream, token));

                    int messageLength = BitConverter.ToInt32(dataBuffer.Take(4).Reverse().ToArray(), 0);
                    dataBuffer.RemoveRange(0, 4);

                    while (dataBuffer.Count < 2)
                        dataBuffer.AddRange(await ReadMore(stream, token));

                    int headerLength = BitConverter.ToUInt16(dataBuffer.Take(2).Reverse().ToArray(), 0);
                    dataBuffer.RemoveRange(0, 2);

                    while (dataBuffer.Count < headerLength)
                        dataBuffer.AddRange(await ReadMore(stream, token));

                    string header = Encoding.UTF8.GetString(dataBuffer.Take(headerLength).ToArray());
                    dataBuffer.RemoveRange(0, headerLength);

                    while (dataBuffer.Count < messageLength)
                        dataBuffer.AddRange(await ReadMore(stream, token));

                    byte[] messageBytes = dataBuffer.Take(messageLength).ToArray();
                    dataBuffer.RemoveRange(0, messageLength);

                    string jsonText = Encoding.UTF8.GetString(messageBytes);
                    object? messageObj;

                    try
                    {
                        messageObj = JsonSerializer.Deserialize<JsonElement>(jsonText);
                    }
                    catch
                    {
                        messageObj = new { raw = jsonText };
                    }

                    Log?.Invoke($"Received -> Header: {header}, Message: {jsonText}");

                    if (header == "LOGIN")
                    {
                        var loginPayload = BuildLoginSuccess(messageObj);
                        await SendPacket(stream, "INITIAL_LOGIN_SUCCESS", loginPayload, token);
                        Log?.Invoke("Sent INITIAL_LOGIN_SUCCESS");

                        // Give client time to process the login config before sending city update
                        await Task.Delay(1000, token);

                        var cityPayload = BuildCityUpdate(messageObj);
                        await SendPacket(stream, "PLAYER_CITY_UPDATE", cityPayload, token);
                        Log?.Invoke("Sent PLAYER_CITY_UPDATE");

                        // --- Step 3: PLAYER_CITY_READY ---
                        var cityReady = new { ready = true };
                        await SendPacket(stream, "PLAYER_CITY_READY", cityReady, token);
                        Log?.Invoke("Sent PLAYER_CITY_READY");

                        // --- Step 4: MAP_DATA ---
                        var mapData = new
                        {
                            width = 50,
                            height = 50
                        };
                        await SendPacket(stream, "MAP_DATA", mapData, token);
                        Log?.Invoke("Sent MAP_DATA");

                        // --- Step 5: MAP_TILES ---
                        var mapTiles = new
                        {
                            r = new object[]
                            {
                                new { c = 1, id = 1, a = 100, ac = 0, imagePath = "" },
                                new { c = 2, id = 2, a = 50, ac = 0, imagePath = "" },
                                new { c = 3, id = 3, a = 25, ac = 0, imagePath = "" },

                                new { c = 4, id = 4, a = 10, ac = 0, imagePath = "" },
                                new { c = 5, id = 5, a = 5, ac = 0, imagePath = "" },
                                new { c = 6, id = 6, a = 40, ac = 0, imagePath = "" },
                                new { c = 7, id = 7, a = 20, ac = 0, imagePath = "" },

                                new { c = 8, id = 8, a = 1000, ac = 0, imagePath = "" },
                                new { c = 9, id = 9, a = 50, ac = 0, imagePath = "" },
                                new { c = 10, id = 10, a = 0, ac = 0, imagePath = "" },
                                new { c = 11, id = 11, a = 1, ac = 0, imagePath = "" },

                                new { c = 12, id = 12, a = 175, ac = 0, imagePath = "" },
                                new { c = 13, id = 13, a = 60, ac = 0, imagePath = "" },
                                new { c = 14, id = 14, a = 15, ac = 0, imagePath = "" },

                                new { c = 15, id = 15, a = 0, ac = 0, imagePath = "" },
                                new { c = 16, id = 16, a = 0, ac = 0, imagePath = "" },
                                new { c = 17, id = 17, a = 0, ac = 0, imagePath = "" },
                                new { c = 18, id = 18, a = 0, ac = 0, imagePath = "" },
                                new { c = 19, id = 19, a = 0, ac = 0, imagePath = "" },
                                new { c = 20, id = 20, a = 0, ac = 0, imagePath = "" },
                                new { c = 21, id = 21, a = 0, ac = 0, imagePath = "" }
                            }
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Log?.Invoke($"Client error: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }

        private async Task<byte[]> ReadMore(NetworkStream stream, CancellationToken token)
        {
            var temp = new byte[4096];
            int read = await stream.ReadAsync(temp, 0, temp.Length, token);
            if (read <= 0)
                throw new IOException("Client disconnected");
            return temp[..read];
        }

        private async Task SendPacket(NetworkStream stream, string header, object payload, CancellationToken token)
        {
            byte[] headerBytes = Encoding.UTF8.GetBytes(header);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

            // Log payload for debugging
            try
            {
                var text = Encoding.UTF8.GetString(jsonBytes);
                Log?.Invoke($"-> Sending {header}: {text}");
            }
            catch { }

            byte[] msgLen = BitConverter.GetBytes(jsonBytes.Length).Reverse().ToArray();
            byte[] headerLen = BitConverter.GetBytes((ushort)headerBytes.Length).Reverse().ToArray();

            await stream.WriteAsync(msgLen, token);
            await stream.WriteAsync(headerLen, token);
            await stream.WriteAsync(headerBytes, token);
            await stream.WriteAsync(jsonBytes, token);
            await stream.FlushAsync(token);
        }

        private object BuildLoginSuccess(object msg)
        {
            // Prefer to load the server's JSON config (Rising-Cities-Server project's minimal config)
            var configPath = Path.Combine("Rising-Cities-Server-main", "static", "game", "config_minimal.json");
            if (File.Exists(configPath))
            {
                try
                {
                    var json = File.ReadAllText(configPath);
                    var doc = JsonDocument.Parse(json);
                    // Clone the RootElement so it remains valid after disposing the document
                    var root = doc.RootElement.Clone();
                    doc.Dispose();
                    return new
                    {
                        config = root,
                        player = new
                        {
                            id = 0,
                            name = "TestUser",
                            level = 1,
                            ui = new
                            {
                                viewedQuest = (string?)null,
                                viewedPlayfieldItemConfigIds = Array.Empty<int>()
                            }
                        }
                    };
                }
                catch (Exception ex)
                {
                    Log?.Invoke($"Failed to load config_minimal.json: {ex.Message}");
                }
            }

            // Fallback: minimal inline config (only resource list) if file missing
            return new
            {
                config = new
                {
                    r = new object[] { }
                },
                player = new
                {
                    id = 0,
                    name = "TestUser",
                    level = 1,
                    ui = new
                    {
                        viewedQuest = (string?)null,
                        viewedPlayfieldItemConfigIds = Array.Empty<int>()
                    }
                }
            };
        }

        private object BuildCityUpdate(object msg)
        {
            // Try to include the resource config inline so client doesn't fail if GameConfigProxy isn't ready
            var configPath = Path.Combine("Rising-Cities-Server-main", "static", "game", "config_minimal.json");
            Dictionary<int, JsonElement>? resourceMap = null;
            if (File.Exists(configPath))
            {
                try
                {
                    var json = File.ReadAllText(configPath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("r", out var rArr) && rArr.ValueKind == JsonValueKind.Array)
                    {
                        resourceMap = new Dictionary<int, JsonElement>();
                        foreach (var item in rArr.EnumerateArray())
                        {
                            if (item.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.Number && idProp.TryGetInt32(out var id))
                            {
                                resourceMap[id] = item.Clone();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log?.Invoke($"Failed to parse config for city update: {ex.Message}");
                }
            }

            object makeRes(int id, int amount)
            {
                if (resourceMap != null && resourceMap.TryGetValue(id, out var cfg))
                {
                    return new { c = id, id = id, a = amount, ac = 0, imagePath = "", resourceConfig = cfg };
                }
                return new { c = id, id = id, a = amount, ac = 0, imagePath = "" };
            }

            return new
            {
                c = new
                {
                    id = 0,
                    n = "TestCity",

                    // Recursos
                    r = new object[]
                    {
                        makeRes(1008, 100),
                        makeRes(1009, 50),
                        makeRes(1010, 25),

                        makeRes(1005, 10),
                        makeRes(1006, 5),
                        makeRes(1003, 40),
                        makeRes(1004, 20),

                        makeRes(1000, 1000),
                        makeRes(1001, 50),
                        makeRes(1011, 0),
                        makeRes(1012, 1),

                        makeRes(1007, 175)
                    },

                    // Estos campos SON OBLIGATORIOS aunque estén vacíos
                    p = Array.Empty<object>(),      // production
                    ph = Array.Empty<object>(),     // production history
                    w = Array.Empty<object>(),      // workers
                    c = Array.Empty<object>(),      // construction
                    imp = Array.Empty<object>(),    // impacts
                    b = Array.Empty<object>(),      // buildings
                    con = Array.Empty<object>(),    // construction queue
                    prod = Array.Empty<object>(),   // production queue
                    s = new { },                    // stats
                    cfg = new { }                   // city config
                }
            };
        }
    }
}

