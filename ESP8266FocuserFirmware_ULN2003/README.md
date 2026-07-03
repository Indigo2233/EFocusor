# EFucoser ESP8266 ULN2003 Firmware Variant

This sketch is the ESP8266 firmware variant for a 35BYJ46 12V geared stepper
motor driven through a ULN2003 or compatible 4-channel low-side driver board.

The ASCOM driver, TCP protocol, serial protocol, WiFi control page, WebSocket
status updates, saved position, and DS18B20 temperature reading are kept
compatible with the main EFucoser firmware. The motor control layer uses
four phase outputs instead of STEP/DIR/ENABLE.

## Intended Hardware

- ESP8266 board: Wemos D1 mini or NodeMCU
- 35BYJ46 12V geared stepper motor
- ULN2003, ULN2803, or equivalent 4-channel driver board
- 12V motor power supply
- DS18B20 temperature sensor
- Optional CW/CCW manual buttons
- Optional Hall sensor, disabled by default in firmware

## Wiring

| ESP8266 pad | GPIO | Connect to | Notes |
| --- | ---: | --- | --- |
| D1 | GPIO5 | ULN2003 IN1 | Motor phase output. |
| D2 | GPIO4 | ULN2003 IN2 | Motor phase output. |
| D5 | GPIO14 | ULN2003 IN3 | Motor phase output. |
| D6 | GPIO12 | ULN2003 IN4 | Motor phase output. |
| D7 | GPIO13 | CW manual button | Wire the other side of the button to GND. |
| D3 | GPIO0 | CCW manual button | Wire the other side of the button to GND. GPIO0 must be HIGH during boot. |
| D4 | GPIO2 | DS18B20 DATA | Add a 4.7k pull-up from DATA to 3V3. GPIO2 must be HIGH during boot. |
| D0 | GPIO16 | Optional Hall sensor output | Disabled by default. Requires external 10k pull-up to 3V3 when enabled. |
| 3V3 | - | DS18B20 VCC, sensor logic VCC | Use for low-current logic and sensors only. |
| GND | - | ULN2003 GND, sensor GND, 12V negative | Common ground is required. |
| 12V supply + | - | ULN2003 motor VCC | Motor power input. |

Connect the 35BYJ46 motor wires to the ULN2003 motor output connector or output
terminals according to the driver board documentation. If the motor vibrates or
moves in an incorrect sequence, swap the IN2/IN3 phase mapping in the
`AccelStepper` constructor or adjust the physical motor wire order.

## Pin Constraints

GPIO0 and GPIO2 are ESP8266 boot strap pins. They must be HIGH during reset.
The DS18B20 pull-up keeps GPIO2 HIGH. The CCW button on GPIO0 should remain
released during boot.

GPIO16 has no normal internal pull-up on common ESP8266 boards. When the
optional Hall sensor is enabled, add an external 10k pull-up from D0/GPIO16 to
3V3 and use an active-low Hall output.

## Optional Hall Sensor

Hall homing is disabled by default:

```cpp
#define USE_HALL_SENSOR 0
```

To enable Hall homing, change it to:

```cpp
#define USE_HALL_SENSOR 1
```

With Hall disabled, the protocol command `H#` returns:

```text
ERR:home_unavailable#
```

The web page Home button still moves to the saved logical zero position. Use
Set 0 after mechanically placing the focuser at the desired zero point.

## Temperature

The DS18B20 is read every 5 seconds without blocking the stepper loop. The
latest value is exposed as `lastTemp` in `/api/status` and `I#`.

Device-side temperature compensation movement is intentionally disabled. NINA
should read the ASCOM `Temperature` property and handle temperature drift or
autofocus behavior.

## Default Motion Settings

Defaults are tuned for a typical 35BYJ46 described as `7.5 deg / 85` and driven
in half-step mode:

```text
stepsPerRev = 8160
maxSteps = 816000
maxSpeed = 800
acceleration = 1000
manualMoveStepSize = 100
findHomeStepSize = 200
```

Actual 35BYJ46 gear ratios and coil order can vary. Calibrate `stepsPerRev`,
`maxSteps`, speed, and direction on the real focuser mechanism.

## Build

Install these Arduino libraries:

```text
AccelStepper
WebSockets
OneWire
DallasTemperature
```

Compile for Wemos D1 mini:

```powershell
arduino-cli compile --fqbn esp8266:esp8266:d1_mini ESP8266FocuserFirmware_ULN2003
```

Compile for NodeMCU 1.0:

```powershell
arduino-cli compile --fqbn esp8266:esp8266:nodemcuv2 ESP8266FocuserFirmware_ULN2003
```

## Protocol Compatibility

The following public identities and protocol surfaces are retained:

- Mobile control URL: `http://192.168.4.1`
- ASCOM TCP port: `4030`
- WebSocket port: `81`
- AP SSID pattern: `Focuser-<chipid>`
- AP password: `012345678`
- ASCOM driver can use the same TCP or serial protocol

The firmware version for this variant is `1101`, and the identification string
is:

```text
EFucoser ESP8266 ULN2003 Focuser ver 1101
```
