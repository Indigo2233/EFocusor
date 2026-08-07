# Add IKunFocuser driver

## Summary

This change adds the IKun Focuser native INDI driver for the EFucoser electronic
focuser. EFucoser is an open hardware controller supporting Arduino Nano and ESP8266
boards with STEP/DIR or ULN2003 motor interfaces.

## Supported hardware

- ESP8266 STEP/DIR, firmware 1005 or later
- ESP8266 ULN2003, firmware 1103 or later
- Arduino Nano ULN2003, firmware 1201–1299

## Connections

- Serial at 9600-8-N-1
- Raw TCP at port 4030 for ESP8266 controllers

## Features

- Absolute and relative movement
- Abort and position synchronization
- Configurable maximum travel
- Direction reversal
- Variable speed and acceleration
- Motor hold control
- Temperature reporting
- Firmware and controller identification

## Dependencies and license

The driver uses INDI core connection and focuser interfaces and introduces no
external dependency. Source files are licensed LGPL-2.1-or-later.

## Validation

- Protocol parser unit tests: ✅ all pass
- INDI core Linux build: ✅ x86_64 on Arch Linux (kernel 7.1.5, indi v2.2.4)
- Real hardware tests with: ESP8266 Wemos D1 mini + 28BYJ-48 + ULN2003, firmware 1103
- KStars/Ekos: ✅ connect/disconnect, absolute/relative move, abort, sync, reverse, hold, temperature (DS18B20)
- Speed: fixed at 300 step/s for reliable torque with 28BYJ-48 + telescope focuser load
- Architecture: x86_64 ✅, ARM64 pending

## Related project

Firmware, protocol, wiring, and test utilities:
https://github.com/Indigo2233/EFocusor
