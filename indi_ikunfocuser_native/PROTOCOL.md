# IKunFocuser controller protocol

## Transport

- Serial: 9600 baud, 8 data bits, no parity, 1 stop bit
- TCP: raw TCP on port 4030 for ESP8266 controllers
- Encoding: ASCII-compatible UTF-8
- Frame terminator: `#`
- One command is processed at a time
- Recommended response timeout: 3 seconds

The controller responds to every valid command. Responses also end with `#`.
Errors use `ERR:<reason>#`.

## Identification and compatibility

| Command | Response |
| --- | --- |
| `#` | `IKunFocuser <controller> Focuser ver <version>#` |

The INDI driver also accepts the legacy `EFucoser` identification response.
| `V#` | `V <version>#` |

Supported release families:

| Version range | Controller |
| --- | --- |
| 1005–1099 | ESP8266 STEP/DIR |
| 1103–1199 | ESP8266 ULN2003 |
| 1201–1299 | Arduino Nano ULN2003 |

The INDI driver performs both identification and version checks during
connection. Generic USB bridge VID/PID values are not used for identification.

## Motion commands

| Command | Description | Successful response |
| --- | --- | --- |
| `G#` | Read logical position and moving state | `P <steps>;M <true\|false>#` |
| `M <steps>#` | Move to an absolute logical position | Current `G#` status |
| `P <steps>#` | Synchronize the current logical position | Current `G#` status |
| `S#` | Stop motion | `S#` |
| `H#` | Start Hall homing when available | `H false#` |

Positions are integer motor steps in the inclusive range `0..maxSteps`.
Out-of-range moves return `ERR:out_of_range#`. Controllers without Hall homing
return `ERR:home_unavailable#` for `H#`.

## Configuration commands

| Command | Range | Description | Successful response |
| --- | ---: | --- | --- |
| `R <0\|1>#` | Boolean | Reverse physical direction | Controller state |
| `C <0\|1>#` | Boolean | Disable/enable motor hold | Controller state |
| `D <steps>#` | 100–9,999,999 | Set maximum travel | `D <steps>#` |
| `X <steps/s>#` | 1–2,000 | Set maximum motor speed | `X <value>#` |
| `A <steps/s²>#` | 1–10,000 | Set acceleration | `A <value>#` |
| `T <coefficient*1000>#` | Signed integer | Set temperature coefficient | `T <value>#` |
| `E <temperature*100>#` | Signed integer | Update controller temperature | `E <value>#` |

Settings changed by `R`, `C`, `D`, `X`, `A`, and `T` are stored in controller
EEPROM.

## Full status

`I#` returns one JSON object followed by `#`. Fields used by the INDI driver:

```json
{
  "positionSteps": 0,
  "targetSteps": 0,
  "isMoving": false,
  "maxSteps": 816000,
  "maxSpeed": 800,
  "acceleration": 1000,
  "reversed": false,
  "hold": false,
  "lastTemp": 20.0,
  "tempSensorPresent": true,
  "firmware": 1103
}
```

Additional fields may be added while preserving these names and value types.

## Error responses

| Response | Meaning |
| --- | --- |
| `ERR:out_of_range#` | Requested position is outside the configured range |
| `ERR:max_steps#` | Invalid maximum travel |
| `ERR:max_steps_below_position_or_target#` | Maximum travel is below the current position or active target |
| `ERR:speed#` | Speed is outside the supported range |
| `ERR:acceleration#` | Acceleration is outside the supported range |
| `ERR:home_unavailable#` | This hardware has no enabled Hall homing input |
| `ERR:<command>#` | Unknown command |
