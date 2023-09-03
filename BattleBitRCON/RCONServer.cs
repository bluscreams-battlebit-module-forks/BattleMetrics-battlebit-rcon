using BattleBitAPI;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using System.Data;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace BattleBitRCON
{
    public class RCONServer<TPlayer> : GameServer<TPlayer>, IDisposable
        where TPlayer : Player<TPlayer>
    {
        static readonly JsonSerializerOptions jsonSerializationOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IgnoreReadOnlyFields = false,
        };

        private HttpListener? listener = null;

        private HashSet<WebSocket> clients = new HashSet<WebSocket>();

        // Map all lowercase command name => namespace
        private Dictionary<string, Type> commandNames;

        private IConfigurationSection config;

        public RCONServer()
        {
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

            foreach (var cmd in commandNamespaces)
            {
                // Getting command name from namespace
                var name = cmd.Namespace?.Split(".").Last().ToLower();
                if (name != null)
                {
                    commandNames.TryAdd(name, cmd);
                }
            }

            config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build()
                .GetSection("BattleBitRCON");
        }

        public new void Dispose()
        {
            if (listener != null)
            {
                listener.Stop();
            }
            base.Dispose();
        }

        public override Task OnConnected()
        {
            if (listener == null)
            {
                startWebsocketServer();
            }

            return base.OnConnected();
        }

        public override Task OnDisconnected()
        {
            if (listener != null)
            {
                listener.Stop();
                listener = null;
            }

            return base.OnDisconnected();
        }

        public override async Task OnPlayerConnected(TPlayer player)
        {
            await BroadcastMessage(new Messages.OnPlayerConnected<TPlayer>(player));
            await base.OnPlayerConnected(player);
        }

        public override async Task OnPlayerDisconnected(TPlayer player)
        {
            await BroadcastMessage(new Messages.OnPlayerDisconnected<TPlayer>(player));
            await base.OnPlayerDisconnected(player);
        }

        public override async Task<bool> OnPlayerTypedMessage(
            TPlayer player,
            ChatChannel channel,
            string msg
        )
        {
            await BroadcastMessage(
                new Messages.OnPlayerTypedMessage<TPlayer>(player, channel, msg)
            );
            return await base.OnPlayerTypedMessage(player, channel, msg);
        }

        public override async Task OnPlayerChangedRole(TPlayer player, GameRole role)
        {
            await BroadcastMessage(new Messages.OnPlayerChangedRole<TPlayer>(player, role));
            await base.OnPlayerChangedRole(player, role);
        }

        public override async Task OnPlayerJoinedSquad(TPlayer player, Squad<TPlayer> squad)
        {
            await BroadcastMessage(new Messages.OnPlayerJoinedSquad<TPlayer>(player, squad));
            await base.OnPlayerJoinedSquad(player, squad);
        }

        public override async Task OnSquadLeaderChanged(Squad<TPlayer> squad, TPlayer newLeader)
        {
            await BroadcastMessage(new Messages.OnSquadLeaderChanged<TPlayer>(squad, newLeader));
            await base.OnSquadLeaderChanged(squad, newLeader);
        }

        public override async Task OnPlayerLeftSquad(TPlayer player, Squad<TPlayer> squad)
        {
            await BroadcastMessage(new Messages.OnPlayerLeftSquad<TPlayer>(player, squad));
            await base.OnPlayerLeftSquad(player, squad);
        }

        public override async Task OnPlayerChangeTeam(TPlayer player, Team team)
        {
            await BroadcastMessage(new Messages.OnPlayerChangeTeam<TPlayer>(player, team));
            await base.OnPlayerChangeTeam(player, team);
        }

        public override async Task OnSquadPointsChanged(Squad<TPlayer> squad, int newPoints)
        {
            await BroadcastMessage(new Messages.OnSquadPointsChanged<TPlayer>(squad, newPoints));
            await base.OnSquadPointsChanged(squad, newPoints);
        }

        public override async Task OnPlayerSpawned(TPlayer player)
        {
            await BroadcastMessage(new Messages.OnPlayerSpawned<TPlayer>(player));
            await base.OnPlayerSpawned(player);
        }

        public override async Task OnPlayerDied(TPlayer player)
        {
            await BroadcastMessage(new Messages.OnPlayerDied<TPlayer>(player));
            await base.OnPlayerDied(player);
        }

        public override async Task OnPlayerGivenUp(TPlayer player)
        {
            await BroadcastMessage(new Messages.OnPlayerGivenUp<TPlayer>(player));
            await base.OnPlayerGivenUp(player);
        }

        public override async Task OnAPlayerDownedAnotherPlayer(OnPlayerKillArguments<TPlayer> args)
        {
            await BroadcastMessage(new Messages.OnAPlayerDownedAnotherPlayer<TPlayer>(args));
            await base.OnAPlayerDownedAnotherPlayer(args);
        }

        public override async Task OnAPlayerRevivedAnotherPlayer(TPlayer from, TPlayer to)
        {
            await BroadcastMessage(new Messages.OnAPlayerRevivedAnotherPlayer<TPlayer>(from, to));
            await base.OnAPlayerRevivedAnotherPlayer(from, to);
        }

        public override async Task OnPlayerReported(
            TPlayer from,
            TPlayer to,
            ReportReason reason,
            string additional
        )
        {
            await BroadcastMessage(
                new Messages.OnPlayerReported<TPlayer>(from, to, reason, additional)
            );
            await base.OnPlayerReported(from, to, reason, additional);
        }

        public override async Task OnGameStateChanged(GameState oldState, GameState newState)
        {
            await BroadcastMessage(new Messages.OnGameStateChanged(oldState, newState));
            await base.OnGameStateChanged(oldState, newState);
        }

        public override async Task OnRoundStarted()
        {
            await BroadcastMessage(new Messages.OnRoundStarted());
            await base.OnRoundStarted();
        }

        public override async Task OnRoundEnded()
        {
            await BroadcastMessage(new Messages.OnRoundEnded());
            await base.OnRoundEnded();
        }

        private async void startWebsocketServer()
        {
            var serverConfig = config.GetSection($"{GameIP}:{GamePort}");
            var ip = serverConfig["ip"] ?? "0.0.0.0";

            int port;
            if (!int.TryParse(serverConfig["port"], out port))
            {
                port = GamePort + 1;
            }

            var password = serverConfig["password"];
            if (password == null)
            {
                // This should probably just be a fatal error, but it's useful for testing.
                password = Guid.NewGuid().ToString();
                Console.WriteLine(
                    $"No RCON password found. Please set a secure password. Using: {password}"
                );
            }

            listener = new HttpListener();
            var prefix = $"http://{ip}:{port}/";
            listener.Prefixes.Add(prefix);

            listener.Start();
            Console.WriteLine($"Listening on {ip}:{port} for game server {GameIP}:{GamePort}");

            while (listener.IsListening)
            {
                HttpListenerContext listenerContext;
                try
                {
                    listenerContext = await listener.GetContextAsync();
                }
                catch
                {
                    break;
                }

                if (
                    listenerContext.Request.IsWebSocketRequest
                    && listenerContext.Request.Headers.Get("x-password") == password
                )
                {
                    ProcessRequest(listenerContext);
                }
                else
                {
                    listenerContext.Response.StatusCode = 401;
                    listenerContext.Response.Close();
                }
            }
        }

        private async void ProcessRequest(HttpListenerContext listenerContext)
        {
            WebSocketContext? webSocketContext;
            try
            {
                webSocketContext = await listenerContext.AcceptWebSocketAsync(subProtocol: null);
            }
            catch (Exception e)
            {
                listenerContext.Response.StatusCode = 500;
                listenerContext.Response.Close();
                Console.WriteLine("Exception: {0}", e);
                return;
            }

            WebSocket webSocket = webSocketContext.WebSocket;
            clients.Add(webSocket);

            try
            {
                // Commands should be pretty short.
                byte[] receiveBuffer = new byte[1024];

                while (webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(receiveBuffer),
                        CancellationToken.None
                    );

                    if (receiveResult.MessageType == WebSocketMessageType.Text)
                    {
                        await ProcessCommand(webSocket, receiveResult, receiveBuffer);
                    }
                    else if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "",
                            CancellationToken.None
                        );
                    }
                    else
                    {
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.InvalidMessageType,
                            "Only text frames are supported.",
                            CancellationToken.None
                        );
                    }
                }
            }
            catch (Exception e)
            {
                // Just log any exceptions to the console. Pretty much any exception that occurs when calling `SendAsync`/`ReceiveAsync`/`CloseAsync` is unrecoverable in that it will abort the connection and leave the `WebSocket` instance in an unusable state.
                Console.WriteLine("Exception: {0}", e);
            }
            finally
            {
                // Clean up by disposing the WebSocket once it is closed/aborted.
                if (webSocket != null)
                {
                    webSocket.Dispose();
                    clients.Remove(webSocket);
                }
            }
        }

        private async Task ProcessCommand(WebSocket ws, WebSocketReceiveResult result, byte[] buff)
        {
            try
            {
                // Deserialize just enough to get the type
                var cmd = JsonSerializer.Deserialize<Commands.CommandType>(
                    new ArraySegment<byte>(buff, 0, result.Count),
                    jsonSerializationOptions
                );

                if (cmd != null)
                {
                    // Validate message type and create generic version of type
                    // to use when invoking parse and execute.
                    if (cmd.Command != null && commandNames.ContainsKey(cmd.Command.ToLower()))
                    {
                        var commandType = commandNames[cmd.Command.ToLower()];
                        var genericType = commandType?.MakeGenericType(typeof(TPlayer));
                        var parse = genericType?.GetMethod("Parse");
                        var execute = genericType?.GetMethod("Execute");

                        if (
                            commandType == null
                            || parse == null
                            || execute == null
                            || genericType == null
                        )
                        {
                            throw new Exception("Unable to get Request class for command");
                        }

                        var parsedCommand = parse.Invoke(
                            genericType,
                            new object[1] { new ArraySegment<byte>(buff, 0, result.Count) }
                        );

                        if (parsedCommand == null)
                        {
                            throw new Exception("Unable to get Request class for command");
                        }

                        var response = execute.Invoke(null, new object[2] { this, parsedCommand });
                        if (response != null)
                        {
                            await SendMessage(ws, response);
                        }
                    }
                    else
                    {
                        throw new Commands.InvalidCommand(cmd.Command);
                    }
                }
            }
            catch (Commands.InvalidCommand e)
            {
                await ws.SendAsync(
                    JsonSerializer.SerializeToUtf8Bytes(
                        new { type = Commands.InvalidCommand.Type, message = e.Message, },
                        jsonSerializationOptions
                    ),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }
        }

        private async Task SendMessage(WebSocket ws, object msg)
        {
            await ws.SendAsync(
                JsonSerializer.SerializeToUtf8Bytes(msg, msg.GetType(), jsonSerializationOptions),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }

        private async Task BroadcastMessage(object msg)
        {
            await Task.WhenAny(
                clients
                    .ToList()
                    .Where(ws => ws.State == WebSocketState.Open)
                    .Select(ws => SendMessage(ws, msg))
            );
        }
    }
}
