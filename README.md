# EFucoser Universal Focuser

EFucoser is an electronic focuser project for telescope automation using
Arduino Nano serial or ESP8266 TCP controllers. It includes controller firmware,
an ASCOM focuser driver for PC astronomy software, and a simple ASCOM test client.

This project is based on the `electric-caa` rotator architecture, adapted for
focuser use with linear position control.

## Features

- ESP8266 firmware with AP+STA WiFi.
- Mobile control page served by the ESP8266 at `http://192.168.4.1`.
- ASCOM-compatible TCP text protocol on port `4030`.
- WebSocket status updates on port `81`.
- ASCOM .NET Framework driver implementing `IFocuserV3` with Serial and TCP transport options.
- Temperature compensation support.
- Optional Hall sensor homing support.
- Manual CW/CCW button support.

## Repository Layout

- `ESP8266FocuserFirmware/`
  - Main ESP8266 + ULN2003 firmware sketch with embedded web UI.
- `ESP8266FocuserFirmware_STEP_DIR/`
  - Compatibility firmware for A4988, DRV8825, TMC2208, and other
    STEP/DIR/ENABLE drivers.
- `ArduinoNanoFocuserFirmware_ULN2003/`
  - Arduino Nano + ULN2003 firmware.
- `driver/EFucoserFocuserDriver/`
  - ASCOM focuser driver source. Public ASCOM identity: `ASCOM.EFucoser.Focuser`.
- `driver/FocuserTest/`
  - Simple Windows Forms ASCOM test client.
- `indi_ikunfocuser_native/`
  - Native C++ INDI focuser driver, protocol tests, upstream staging tool, and
    submission documentation.
- `AGENTS.md`
  - Detailed implementation notes for development agents.

## ASCOM Identity

- **ProgID**: `ASCOM.EFucoser.Focuser`
- **Chooser Name**: `EFucoser Universal Focuser`
- **Description**: `ASCOM Focuser Driver for EFucoser Arduino Nano and ESP8266 controllers.`
- **DLL**: `ASCOM.EFucoser.Focuser.dll`
- **Interface**: `IFocuserV3`

## Default WiFi

- AP SSID: `Focuser-<chipid>`
- AP password: `012345678`
- Mobile control page: `http://192.168.4.1`
- ASCOM TCP port: `4030`
- WebSocket port: `81`

## Hardware Summary

Default ESP8266 board target:

- Wemos D1 mini or NodeMCU

Primary ESP8266 firmware wiring:

| Board pad | Connect to | Notes |
| --- | --- | --- |
| `D1` / GPIO5 | ULN2003 `IN1` | Motor phase output. |
| `D2` / GPIO4 | ULN2003 `IN2` | Motor phase output. |
| `D5` / GPIO14 | ULN2003 `IN3` | Motor phase output. |
| `D6` / GPIO12 | ULN2003 `IN4` | Motor phase output. |
| `D7` / GPIO13 | CW / Inward manual button | Wire the other side of the button to `GND`. |
| `D3` / GPIO0 | CCW / Outward manual button | Wire the other side to `GND`; keep released during boot. |
| `D4` / GPIO2 | DS18B20 data | Add a 4.7k pull-up to `3V3`. |
| `D0` / GPIO16 | Optional Hall sensor | Disabled by default; requires an external 10k pull-up to `3V3`. |
| `GND` | ULN2003 GND, sensor GND, and 12V supply negative | Common ground is required. |
| 12V supply positive | ULN2003 motor VCC | Intended for a 35BYJ46 12V motor. |
| USB or 5V input | ESP8266 board power | Keep 12V away from ESP8266 power and GPIO pins. |

The STEP/DIR wiring is documented in
`ESP8266FocuserFirmware_STEP_DIR/README.md`.

## Text Protocol (TCP Port 4030 / Serial)

All commands are `#`-terminated:

| Command | Description |
| --- | --- |
| `#` | Device identification |
| `G#` | Get current position and moving state: `P <steps>;M <true\|false>#` |
| `M <steps>#` | Move to absolute step position |
| `P <steps>#` | Set current logical position (offset adjustment) |
| `H#` | Start optional Hall homing; returns `ERR:home_unavailable#` when disabled |
| `S#` | Stop movement immediately |
| `R <0\|1>#` | Set direction inversion |
| `C <0\|1>#` | Set continuous hold |
| `D <maxSteps>#` | Set max steps range |
| `X <steps/s>#` | Set maximum motor speed |
| `A <steps/s²>#` | Set motor acceleration |
| `T <coeff * 1000>#` | Set temperature compensation coefficient |
| `E <temp * 100>#` | Update current temperature |
| `V#` | Firmware version |
| `I#` | JSON status with all fields |

## Build Requirements

### Firmware
- Arduino IDE with ESP8266 core
- Libraries: `AccelStepper`, `WebSocketsServer`, `OneWire`,
  `DallasTemperature`

### ASCOM Driver
- Visual Studio with .NET Framework 4.8
- ASCOM Platform 6 Developer Components

### INDI Driver
- Linux or macOS
- libindi development files
- CMake and a C++17 compiler

## ASCOM Driver Installer

Build the distributable Windows installer from PowerShell:

```powershell
.\installer\Build-Installer.ps1
```

The build requires Visual Studio Build Tools, ASCOM Platform Developer Components,
and Inno Setup 6. The generated file is `dist\EFucoserASCOMSetup.exe`.

The installer requires administrator privileges and ASCOM Platform 6 or later. It
registers the driver for 32-bit and 64-bit ASCOM clients and removes both
registrations during uninstall.
