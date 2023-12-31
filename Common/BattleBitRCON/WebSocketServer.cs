using BattleBitAPI;
using BattleBitAPI.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BattleBitRCON {

    public class WebSocketServer<TPlayer> : IDisposable
        where TPlayer : Player<TPlayer> {

        private static readonly JsonSerializerOptions jsonSerializationOptions = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IgnoreReadOnlyFields = false,
        };

        private HttpListener? listener = null;

        private HashSet<WebSocket> clients = new HashSet<WebSocket>();

        private ConcurrentDictionary<WebSocket, ConcurrentQueue<object>> pendingMessages =
            new ConcurrentDictionary<WebSocket, ConcurrentQueue<object>>();

        private ConcurrentDictionary<WebSocket, bool> sendingMessages =
            new ConcurrentDictionary<WebSocket, bool>();

        // Map all lowercase command name => namespace
        private Dictionary<string, Type> commandNames;

        private string listenIP;
        private int listenPort;
        private string password;

        private GameServer<TPlayer> gameServer;

        public WebSocketServer(
            GameServer<TPlayer> gameServer,
            string listenIP,
            int listenPort,
            string password
        ) {
            this.gameServer = gameServer;

            // Find all BattleBitRCON.Commands.*.Request classes
            var commandNamespaces = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(
                    t =>
                        t.IsClass
                        && t.Namespace?.StartsWith("BattleBitRCON.Commands.") == true
                        && t.Name.StartsWith("Request")
                )
                .ToList();

            commandNames = new Dictionary<string, Type>();

            foreach (var cmd in commandNamespaces) {
                // Getting command name from namespace
                var name = cmd.Namespace?.Split(".").Last().ToLower();
                if (name != null) {
                    commandNames.TryAdd(name, cmd);
                }
            }

            this.listenIP = listenIP;
            this.listenPort = listenPort;
            this.password = password;
        }

        public void Dispose() {
            Stop();
        }

        public async Task Start() {
            listener = new HttpListener();
            var prefix = $"http://{listenIP}:{listenPort}/";
            listener.Prefixes.Add(prefix);

            listener.Start();
            Console.WriteLine($"RCON Server started: {listenIP}:{listenPort}");

            while (listener.IsListening) {
                HttpListenerContext listenerContext;
                try {
                    listenerContext = await listener.GetContextAsync();
                } catch {
                    break;
                }

                if (
                    listenerContext.Request.IsWebSocketRequest
                    && listenerContext.Request.Headers.Get("x-password") == password
                ) {
                    _ = ProcessRequest(listenerContext);
                } else {
                    listenerContext.Response.StatusCode = 401;
                    listenerContext.Response.Close();
                }
            }
        }

        public void Stop() {
            if (listener != null) {
                Console.WriteLine($"RCON Server stopped: {listenIP}:{listenPort}");
                listener.Stop();
                listener = null;
            }
        }

        private async Task ProcessRequest(HttpListenerContext listenerContext) {
            WebSocketContext? webSocketContext;
            try {
                webSocketContext = await listenerContext.AcceptWebSocketAsync(subProtocol: null);
            } catch (Exception e) {
                listenerContext.Response.StatusCode = 500;
                listenerContext.Response.Close();
                Console.WriteLine("Exception: {0}", e);
                return;
            }

            WebSocket webSocket = webSocketContext.WebSocket;
            clients.Add(webSocket);

            try {
                // Commands should be pretty short.
                byte[] receiveBuffer = new byte[1024];

                while (webSocket.State == WebSocketState.Open) {
                    WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(receiveBuffer),
                        CancellationToken.None
                    );

                    if (receiveResult.MessageType == WebSocketMessageType.Text) {
                        await ProcessCommand(webSocket, receiveResult, receiveBuffer);
                    } else if (receiveResult.MessageType == WebSocketMessageType.Close) {
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "",
                            CancellationToken.None
                        );
                    } else {
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.InvalidMessageType,
                            "Only text frames are supported.",
                            CancellationToken.None
                        );
                    }
                }
            } catch (WebSocketException e)
                  when (e.InnerException is HttpListenerException exception
                      && exception.ErrorCode == 995
                  ) {
                // The HTTP server was shutdown.
                // We'll cleanup below and can ignore this error.
            } catch (WebSocketException) {
                // Unrecoverable error. Log for monitoring purposes, but client should just open a
                // new connection.
                Console.WriteLine(
                    "RCON connection error. Closing connection for: {0}",
                    listenerContext.Request.RemoteEndPoint.Address.ToString()
                );
            } finally {
                // Clean up by disposing the WebSocket once it is closed/aborted.
                if (webSocket != null) {
                    webSocket.Dispose();
                    clients.Remove(webSocket);

                    pendingMessages.TryRemove(webSocket, out _);
                    sendingMessages.TryRemove(webSocket, out _);
                }
            }
        }

        private async Task ProcessCommand(WebSocket ws, WebSocketReceiveResult result, byte[] buff) {
            try {
                // Deserialize just enough to get the type
                var cmd = JsonSerializer.Deserialize<Commands.CommandType>(
                    new ArraySegment<byte>(buff, 0, result.Count),
                    jsonSerializationOptions
                );

                if (cmd != null) {
                    // Validate message type and create generic version of type
                    // to use when invoking parse and execute.
                    if (cmd.Command != null && commandNames.ContainsKey(cmd.Command.ToLower())) {
                        var commandType = commandNames[cmd.Command.ToLower()];
                        var genericType = commandType?.MakeGenericType(typeof(TPlayer));
                        var parse = genericType?.GetMethod("Parse");
                        var execute = genericType?.GetMethod("Execute");

                        if (
                            commandType == null
                            || parse == null
                            || execute == null
                            || genericType == null
                        ) {
                            throw new Exception("Unable to get Request class for command");
                        }

                        var parsedCommand = parse.Invoke(
                            genericType,
                            new object[1] { new ArraySegment<byte>(buff, 0, result.Count) }
                        );

                        if (parsedCommand == null) {
                            throw new Exception("Unable to get Request class for command");
                        }

                        var response = execute.Invoke(
                            null,
                            new object[2] { gameServer, parsedCommand }
                        );
                        if (response != null) {
                            await SendMessage(ws, response);
                        }
                    } else {
                        throw new Commands.InvalidCommand(cmd.Command);
                    }
                }
            } catch (Commands.InvalidCommand e) {
                await SendMessage(ws, e);
            }
        }

        private async Task processPendingMessages(WebSocket ws) {
            if (sendingMessages.GetValueOrDefault(ws, false) == true) {
                return;
            }

            sendingMessages[ws] = true;
            try {
                var list = pendingMessages.GetValueOrDefault(ws, new ConcurrentQueue<object>());

                while (list.Count > 0 && ws.State == WebSocketState.Open) {
                    object? msg;
                    if (list.TryDequeue(out msg)) {
                        await ws.SendAsync(
                            JsonSerializer.SerializeToUtf8Bytes(
                                msg,
                                msg.GetType(),
                                jsonSerializationOptions
                            ),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None
                        );
                    }
                }
            } catch {
                // Suppress errors sending messages. There is nothing for the user
                // to do and it shouldn't matter.
            } finally {
                sendingMessages[ws] = false;
            }
        }

        public async Task SendMessage(WebSocket ws, object msg) {
            var list = pendingMessages.GetOrAdd(ws, new ConcurrentQueue<object>());
            list.Enqueue(msg);

            await processPendingMessages(ws);
        }

        public async Task BroadcastMessage(object msg) {
            await Task.WhenAll(
                clients
                    .ToList()
                    .Where(ws => ws.State == WebSocketState.Open)
                    .Select(ws => SendMessage(ws, msg))
                    .Union(new[] { Task.CompletedTask })
            );
        }
    }
}