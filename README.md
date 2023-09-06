# BattleBit RCON

This implements a WebSocket based protocol for the BattleBit Community API to allow for a BattleMetrics RCON connection.

# Methods

## BattleBit API Runner

A module compatible with the BattleBit API Runner is available for users who want the easiest setup method. Both the `BattleMetricsRCON.cs` module and `BattleMetricsRCON.dll` dependency are required.

You can use the following link to download the latest release:
[https://gitlab.com/BattleMetrics/battlebit-rcon/-/releases/permalink/latest/downloads/BattleMetricsRCONModule.zip](https://gitlab.com/BattleMetrics/battlebit-rcon/-/releases/permalink/latest/downloads/BattleMetricsRCONModule.zip)

A server configuration for the RCON IP, port, and password is required. If one is not provided a config file will be generated with default values. By default the RCON server will listen on all IPs on the connecting game port + 1. For example if the game port is 8000, the RCON port will default to 8001 unless specified by you. A random 32 character password will be generated and saved.

The RCON configuration file should be located at `BattleBitAPIRunner/configurations/<game_server_ip>_<game_server_port>/BattleMetricsRCON/BattleMetricsRCONConfiguration.json`.

Example config:

```json
{
  "RCONIP": "+",
  "RCONPort": 8000,
  "Password": "password"
}
```

For more information see the [BattleBit API Runner Hosting Guide](https://github.com/BattleBit-Community-Servers/BattleBitAPIRunner/wiki/Hosting-Guide).

Past releases are listed under [Deploy -> Releases](https://gitlab.com/BattleMetrics/battlebit-rcon/-/releases).

## Manually

To use this manually you will need to extend the `BattleBitRCON.RCONServer` class instead of `BattleBitAPI.Server.GameServer`.

The class is available under [BattleMetrics.BattleBitRCON on NuGet](https://www.nuget.org/packages/BattleMetrics.BattleBitRCON/).

### Manual Configuration

To configure the RCON server's IP, port, and password add the following to your `appsettings.json`. Customize as needed.

```json
"BattleBitRCON": {
  "<GameServerIP>:<GamePort>": {
    "ip": "0.0.0.0",
    "port": "8001",
    "password": "password"
  },
  "127.0.0.1:8000": {
    "ip": "0.0.0.0",
    "port": "8001",
    "password": "password"
  }
}
```
