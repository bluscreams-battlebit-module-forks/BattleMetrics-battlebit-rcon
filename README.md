# BattleBit RCON

This implements a WebSocket based protocol for the BattleBit Community API to allow for a BattleMetrics RCON connection.

To use this you will need to extend the `BattleBitRCON.RCONServer` class instead of `BattleBitAPI.Server.GameServer`.

## Configuration

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
