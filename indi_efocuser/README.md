# EFucoser INDI Focuser Driver

INDI driver for **EFucoser Arduino Nano ULN2003 Focuser**, compatible with
EkOS / KStars, PHD2, and any INDI-compatible client on Linux.

## Features

| Property | INDI Name | R/W | Description |
|---|---|---|---|
| **Speed** | `FOCUS_SPEED` | R/W | Max speed in steps/sec (1–2000) |
| **Acceleration** | `ACCELERATION` | R/W | Acceleration in steps/sec² (10–10000) |
| **Max Position** | `FOCUS_MAX` | R/W | Maximum position limit (steps) |
| **Absolute Position** | `ABS_FOCUS_POSITION` | R/W | Current position and absolute target |
| **Relative Move** | `REL_FOCUS_POSITION` | R/W | Relative move amount in the selected direction |
| **Direction** | `FOCUS_MOTION` | R/W | Inward / Outward for timer moves |
| **Timer** | `FOCUS_TIMER` | R/W | Timer-based move duration (ms) |
| **Reverse** | `FOCUS_REVERSE_MOTION` | R/W | Invert motor direction |
| **Hold** | `HOLD_MODE` | R/W | Continuous hold on/off |
| **Temperature** | `FOCUS_TEMPERATURE` | R/O | DS18B20 temperature (°C) |
| **Abort** | `FOCUS_ABORT_MOTION` | W | Halt movement immediately |

## Hardware

- Arduino Nano (ATmega328P, old bootloader)
- ULN2003 driver board
- 35BYJ46 12V geared stepper motor
- DS18B20 temperature sensor (optional)
- USB serial connection at 9600 baud

## Requirements

```bash
# Python dependencies
pip install pyindi-client pyserial

# INDI server (system package)
# Arch: sudo pacman -S indi
# Ubuntu: sudo apt install indi-full
```

## Usage

### Method 1: Direct with indiserver

```bash
# Start the driver
indiserver -v indi_efocuser/indi_efocuser_focuser.py

# Then connect from KStars EkOS or any INDI client
```

### Method 2: Using the launch script

```bash
cd indi_efocuser
chmod +x start_indiserver.sh
./start_indiserver.sh
```

### Method 3: Install as system driver

```bash
# Copy XML to INDI drivers directory
sudo cp drivers.xml /usr/share/indi/

# Make driver executable
chmod +x indi_efocuser_focuser.py

# Symlink to INDI scripts directory (optional)
sudo ln -s $(pwd)/indi_efocuser_focuser.py /usr/share/indi/scripts/
```

## Connection

1. In EkOS: open the INDI Control Panel
2. Select "EFucoser Focuser" from the devices list
3. In the "Connection" tab, enter the serial port (e.g. `/dev/ttyUSB0`)
4. Click "Connect"

The driver will auto-detect the focuser on the selected port.

The driver follows the standard INDI focuser vector and element names used by
Ekos and other clients. It also answers every matching `getProperties` request,
so clients that connect after the driver starts receive the complete property
definitions.

## Firmware Protocol

The serial protocol uses `#`-terminated text commands:

| Command | Description |
|---|---|
| `#` | Device identity |
| `G#` | Poll position & moving state |
| `M <steps>#` | Move to absolute position |
| `P <steps>#` | Set current position |
| `S#` | Stop movement |
| `R <0\|1>#` | Set direction inversion |
| `C <0\|1>#` | Hold on/off |
| `D <max>#` | Set max position range |
| `X <speed>#` | Set max speed |
| `A <accel>#` | Set acceleration |
| `V#` | Firmware version |
| `I#` | Full JSON status |
| `H#` | Home (unavailable) |

## Troubleshooting

**"No serial port selected"**: Make sure you've entered the correct port
in the Connection tab before clicking Connect.

**Permission denied on /dev/ttyUSB0**: Add your user to the `dialout` group:
```bash
sudo usermod -a -G dialout $USER
# Log out and back in
```

**Driver not found by indiserver**: Use the full path or install the
driver to `/usr/share/indi/scripts/`.
