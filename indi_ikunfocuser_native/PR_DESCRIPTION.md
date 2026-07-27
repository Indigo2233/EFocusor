# Add IKunFocuser driver

## Summary

This change adds a native INDI driver for the IKunFocuser electronic focuser.
IKunFocuser is an open hardware controller supporting Arduino Nano and ESP8266
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

- Protocol parser unit tests
- INDI core Linux build
- Real hardware tests with: [fill in controller and motor]
- KStars/Ekos: [fill in version and result]
- INDI Web Manager: [fill in version and result]
- Architecture: [fill in x86_64 and ARM64 results]

## Related project

Firmware, protocol, wiring, and test utilities:
https://github.com/Indigo2233/EFocusor
