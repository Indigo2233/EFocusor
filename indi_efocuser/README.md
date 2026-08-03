# EFucoser INDI Focuser Driver

INDI driver for **EFucoser Arduino Nano ULN2003 Focuser**, compatible with
EkOS / KStars, PHD2, and any INDI-compatible client on Linux.

## Features

| Property | INDI Name | R/W | Description |
|---|---|---|---|
| **Speed** | `FOCUS_SPEED` | R/W | Max speed in steps/sec (1-2000) |
| **Acceleration** | `ACCELERATION` | R/W | Acceleration in steps/sec^2 (10-10000) |
| **Max Position** | `FOCUS_MAX` | R/W | Maximum position limit (steps) |
| **Absolute Position** | `ABS_FOCUS_POSITION` | R/W | Current position and absolute target |
| **Relative Move** | `REL_FOCUS_POSITION` | R/W | Relative move amount in the selected direction |
| **Direction** | `FOCUS_MOTION` | R/W | Inward / Outward for timer moves |
| **Timer** | `FOCUS_TIMER` | R/W | Timer-based move duration (ms) |
| **Reverse** | `FOCUS_REVERSE_MOTION` | R/W | Invert motor direction |
| **Hold** | `HOLD_MODE` | R/W | Continuous hold on/off |
| **Temperature** | `FOCUS_TEMPERATURE` | R/O | DS18B20 temperature (deg C) |
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
# Start the driver (wrapper resolves paths automatically)
indiserver -v indi_efocuser/indi_efocuser_focuser

# Or use the Python driver directly
indiserver -v indi_efocuser/indi_efocuser_focuser.py
```

### Method 2: Using the launch script

```bash
cd indi_efocuser
chmod +x start_indiserver.sh
./start_indiserver.sh
```

### Method 3: Install as system driver (manual)

```bash
cd indi_efocuser

# Copy XML to INDI drivers directory
sudo cp drivers.xml /usr/share/indi/

# Make wrapper and driver executable
chmod +x indi_efocuser_focuser indi_efocuser_focuser.py

# Symlink wrapper to a PATH directory so indiserver can find it
sudo ln -sf "$(pwd)/indi_efocuser_focuser" /usr/local/bin/
```

### Method 4: Install with CMake (recommended)

```bash
cd indi_efocuser
mkdir build && cd build
cmake -DCMAKE_INSTALL_PREFIX=/usr ..
sudo make install
```

After installation, start with:
```bash
indiserver -v indi_efocuser_focuser
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

Driver 1.1.1 uses the INDI definition widget tags required by KStars
(`defNumber`, `defSwitch`, and `defText`).

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

**Driver not found by indiserver**: Use the full path to the wrapper script,
or install it to `/usr/share/indi/scripts/`.

**"Driver not found" when using `indiserver -v indi_efocuser_focuser`**:
Make sure the wrapper `indi_efocuser_focuser` is in your PATH or use Method 3/4 above.
