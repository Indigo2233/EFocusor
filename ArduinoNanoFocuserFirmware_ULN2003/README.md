# EFucoser Arduino Nano ULN2003 Firmware

This firmware is for an Arduino Nano connected over USB serial to the ASCOM
driver. It drives a 35BYJ46 12V geared stepper motor through a ULN2003 board and
reads an optional DS18B20 temperature sensor.

## Wiring

### ULN2003

| Arduino Nano | ULN2003 board |
| --- | --- |
| D8 | IN1 |
| D9 | IN2 |
| D10 | IN3 |
| D11 | IN4 |
| GND | GND |

Power wiring:

| Power | ULN2003 board |
| --- | --- |
| 12V positive | VCC |
| 12V negative | GND |

The Arduino Nano is powered by USB. The Nano GND, ULN2003 GND, and 12V negative
must be connected together.

### 35BYJ46 Motor

If the motor has the common 5-wire order:

```text
1 blue
2 pink
3 yellow
4 orange
5 red
```

the motor can usually plug into the ULN2003 XH-5P socket directly. The red wire
is normally the common wire connected to VCC.

### DS18B20 Temperature Sensor

Use Arduino Nano `D2` for the DS18B20 data line:

| DS18B20 | Arduino Nano |
| --- | --- |
| VCC | 5V |
| GND | GND |
| DATA | D2 |

Add a 4.7k resistor between DATA and 5V.

Common waterproof probe wire colors:

```text
red   -> 5V
black -> GND
yellow/white -> D2
```

## Protocol

The firmware implements the same serial protocol used by the ASCOM driver:

| Command | Response / behavior |
| --- | --- |
| `#` | Device identity |
| `G#` | `P <steps>;M <true|false>#` |
| `M <steps>#` | Move to absolute logical position |
| `P <steps>#` | Set current logical position |
| `H#` | `ERR:home_unavailable#` |
| `S#` | Stop movement |
| `R <0|1>#` | Set direction inversion |
| `C <0|1>#` | Continuous hold on/off |
| `V#` | Firmware version |
| `I#` | JSON status |
| `D <maxSteps>#` | Set maximum position range |
| `T <coeff*1000>#` | Store temperature coefficient for compatibility |
| `E <temp*100>#` | Set temperature for compatibility |

Device-side temperature compensation movement is intentionally absent. NINA can
read ASCOM `Temperature` and manage autofocus behavior.

## Defaults

```text
firmware = 1201
baud = 9600
stepsPerRev = 8160
maxSteps = 816000
maxSpeed = 800
acceleration = 1000
```

The 8160 steps/rev default assumes a typical 35BYJ46 with `7.5 deg / 85` and
half-step drive. Calibrate this value on the real focuser if needed.

## Build

Install these Arduino libraries:

```text
AccelStepper
OneWire
DallasTemperature
```

Install the Arduino AVR core if needed:

```powershell
arduino-cli core install arduino:avr
```

Compile for Arduino Nano:

```powershell
arduino-cli compile --fqbn arduino:avr:nano ArduinoNanoFocuserFirmware_ULN2003
```

For older Nano bootloaders:

```powershell
arduino-cli compile --fqbn arduino:avr:nano:cpu=atmega328old ArduinoNanoFocuserFirmware_ULN2003
```
