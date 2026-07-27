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
- Hall sensor homing support.
- Manual CW/CCW button support.

## Repository Layout

- `ESP8266FocuserFirmware/`
  - Main ESP8266 firmware sketch with embedded web UI.
- `driver/EFucoserFocuserDriver/`
  - ASCOM focuser driver source. Public ASCOM identity: `ASCOM.EFucoser.Focuser`.
- `driver/FocuserTest/`
  - Simple Windows Forms ASCOM test client.
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

Default wiring (compatible with `electric-caa` rotator):

| Board pad | Connect to | Notes |
| --- | --- | --- |
| `D1` / GPIO5 | Stepper driver `STEP`, `PUL`, or `CLK` | 3.3V logic pulse output. |
| `D2` / GPIO4 | Stepper driver `DIR` | Direction output. Reverse in firmware or web UI if direction is inverted. |
| `D5` / GPIO14 | Stepper driver `ENABLE` or `ENA` | Active low. The firmware pulls it low when the driver should be enabled. |
| `D6` / GPIO12 | Hall sensor output | Active low. Sensor output must stay at 3.3V or lower. |
| `D7` / GPIO13 | CW / Inward manual button | Wire the other side of the button to `GND`. |
| `D0` / GPIO16 | CCW / Outward manual button | Wire the other side of the button to `GND`; add a 10k pull-up from `D0` to `3V3`. |
| `3V3` | Hall sensor VCC, or driver logic VDD when supported | Use only for low-current 3.3V logic or sensors. |
| `GND` | Stepper driver logic GND, Hall GND, button common, and 12V supply negative | Common ground is required for STEP/DIR/ENABLE to be valid. |
| USB or `Vin` | ESP8266 board power | Prefer USB during testing. |

## Text Protocol (TCP Port 4030 / Serial)

All commands are `#`-terminated:

| Command | Description |
| --- | --- |
| `#` | Device identification |
| `G#` | Get current position and moving state: `P <steps>;M <true\|false>#` |
| `M <steps>#` | Move to absolute step position |
| `P <steps>#` | Set current logical position (offset adjustment) |
| `H#` | Start homing sequence (seeks Hall sensor) |
| `S#` | Stop movement immediately |
| `R <0\|1>#` | Set direction inversion |
| `C <0\|1>#` | Set continuous hold |
| `D <maxSteps>#` | Set max steps range |
| `T <coeff * 1000>#` | Set temperature compensation coefficient |
| `E <temp * 100>#` | Update current temperature |
| `V#` | Firmware version |
| `I#` | JSON status with all fields |

## Build Requirements

### Firmware
- Arduino IDE with ESP8266 core
- Libraries: `AccelStepper`, `WebSocketsServer`

### ASCOM Driver
- Visual Studio with .NET Framework 4.8
- ASCOM Platform 6 Developer Components

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
