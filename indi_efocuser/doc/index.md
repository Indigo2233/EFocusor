# EFucoser INDI Focuser Driver

## Device Overview

The **EFucoser INDI Focuser Driver** provides INDI-compatible control for the
EFucoser Arduino Nano ULN2003 electronic focuser. It communicates with the
Arduino Nano firmware over USB serial (9600 baud, 8N1) using a simple
`#`-terminated text protocol.

- **Manufacturer**: EFucoser
- **Model**: Arduino Nano ULN2003 Focuser
- **INDI Interface**: `FOCUSER_INTERFACE` (8)
- **Driver Version**: 1.1.1
- **Protocol Version**: INDI 1.7

### Supported Hardware

| Component | Details |
|---|---|
| Microcontroller | Arduino Nano (ATmega328P, old bootloader) |
| Driver board | ULN2003 |
| Motor | 35BYJ46 12V geared stepper |
| Temperature sensor | DS18B20 (optional) |

The driver is also compatible with the ESP8266 Wi-Fi focuser firmware
(`ESP8266FocuserFirmware_ULN2003`) over TCP on port 4030.

## Features

- **Absolute positioning** (`ABS_FOCUS_POSITION`) — move to any step position
  within the configured range
- **Relative positioning** (`REL_FOCUS_POSITION`) — move inward/outward by a
  specified number of steps
- **Timer-based movement** (`FOCUS_TIMER`) — move for a specified duration (ms)
- **Direction control** (`FOCUS_MOTION`) — Inward / Outward for relative moves
- **Speed and acceleration** (`FOCUS_SPEED`, `ACCELERATION`) — configurable
  motor dynamics (1–2000 steps/s, 10–10000 steps/s²)
- **Maximum position limit** (`FOCUS_MAX`) — sets the upper bound for movement
- **Reverse motion** (`FOCUS_REVERSE_MOTION`) — invert motor direction
- **Continuous hold** (`HOLD_MODE`) — enable/disable motor holding torque when
  idle
- **Temperature monitoring** (`FOCUS_TEMPERATURE`) — read DS18B20 sensor
- **Abort motion** (`FOCUS_ABORT_MOTION`) — halt movement immediately
- **Auto-detection** — driver identifies the focuser on the selected serial port

## Installation

### System Requirements

- Linux (Arch, Ubuntu, Debian, etc.)
- Python 3.8+
- INDI server (`indiserver`) — install via your package manager:
  - **Arch**: `sudo pacman -S indi`
  - **Ubuntu/Debian**: `sudo apt install indi-full`

### Python Dependencies

```bash
pip install pyindi-client pyserial
```

Both packages are pure Python and require no compilation.

### Install Driver Files

```bash
cd indi_efocuser

# 1. Install driver XML definition
sudo cp drivers.xml /usr/share/indi/

# 2. Make wrapper and driver executable
chmod +x indi_efocuser_focuser indi_efocuser_focuser.py

# 3. Symlink wrapper to INDI scripts directory
sudo ln -sf "$(pwd)/indi_efocuser_focuser" /usr/share/indi/scripts/
```

Or, if building with CMake (recommended for package maintainers):

```bash
mkdir build && cd build
cmake -DCMAKE_INSTALL_PREFIX=/usr ..
make install
```

### Verify Installation

```bash
indiserver -v indi_efocuser_focuser
```

You should see `Driver indi_efocuser_focuser: started` in the output.

## Configuration

### Connection Types

| Type | Transport | Notes |
|---|---|---|
| USB Serial | `/dev/ttyUSB*` | Default for Arduino Nano |
| TCP | Host:Port | For ESP8266 Wi-Fi firmware (port 4030) |

### INDI Control Panel Setup

1. Open your INDI client (KStars / EkOS, PHD2, etc.)
2. Select **"EFucoser Focuser"** from the devices list
3. In the **Connection** tab, enter the serial port:
   - Typical: `/dev/ttyUSB0`
   - If using ESP8266 Wi-Fi: enter IP address and port `4030`
4. Click **Connect**

The driver will auto-detect the focuser on the selected port. If detection
fails, check that:
- The correct serial port is selected
- The Arduino is powered and the firmware is flashed
- Your user has permission to access the serial port

### Serial Port Permissions

If you see "Permission denied" on `/dev/ttyUSB0`, add your user to the
`dialout` group:

```bash
sudo usermod -a -G dialout $USER
# Log out and log back in for changes to take effect
```

### Main Control Tab

| Property | Description |
|---|---|
| **Speed** | Motor max speed in steps/sec (1–2000) |
| **Acceleration** | Ramp rate in steps/sec² (10–10000) |
| **Max. Position** | Upper limit for absolute moves (steps) |
| **Absolute Position** | Current position; set a target to move |
| **Relative Position** | Move relative to current position |
| **Direction** | Inward / Outward selector for relative moves |
| **Timer** | Timed movement duration (ms) |

### Options Tab

| Property | Description |
|---|---|
| **Reverse Motion** | Flip the motor direction |
| **Hold Mode** | Keep motor energized when idle (prevents drift) |

## Usage & Tips

### Using with EkOS Auto-Focus

1. Connect the focuser in EkOS
2. Configure **Max. Position** to match your physical travel range
3. Set **Speed** and **Acceleration** to values that produce smooth, reliable
   movement with your specific motor and load
4. Use EkOS's built-in autofocus routine — the driver handles the rest

### Firmware Protocol

The driver communicates with the firmware using `#`-terminated text commands
over serial (9600-8N1). Key commands:

| Command | Action |
|---|---|
| `G#` | Poll position and moving state |
| `M <steps>#` | Move to absolute position |
| `P <steps>#` | Set current logical position |
| `S#` | Stop movement |
| `R <0\|1>#` | Set direction inversion |
| `C <0\|1>#` | Hold on/off |
| `D <max>#` | Set max position range |
| `X <speed>#` | Set max speed |
| `A <accel>#` | Set acceleration |
| `V#` | Firmware version |
| `I#` | Full JSON status |

### Temperature Compensation

The driver reads temperature from the DS18B20 sensor (if present) every ~2
seconds. Temperature is reported as `FOCUS_TEMPERATURE`. Temperature
compensation coefficients can be set via the firmware (`T` command), and the
driver supports `TempComp` / `TempCompAvailable` in the INDI interface.

### Troubleshooting

| Problem | Solution |
|---|---|
| Driver not found by indiserver | Use full path or symlink to `/usr/share/indi/scripts/` |
| "No serial port selected" | Enter the correct port in the Connection tab before Connect |
| Permission denied on `/dev/ttyUSB0` | Add user to `dialout` group and re-login |
| Focuser not detected | Check USB cable, Arduino power, and firmware flash |
| Movement stutters | Reduce Speed or increase Acceleration |

### Testing

Run the included test suite to verify driver protocol behavior:

```bash
cd indi_efocuser
python -m pytest test_indi_efocuser.py -v
```

Or use the CLI controller for interactive testing:

```bash
python focuser_cli.py
```

## License

GPL-2.0-or-later. See [LICENSE](LICENSE) for full text.

## Repository

- **Source**: https://github.com/Indigo2233/EFocusor
- **Issues**: https://github.com/Indigo2233/EFocusor/issues
