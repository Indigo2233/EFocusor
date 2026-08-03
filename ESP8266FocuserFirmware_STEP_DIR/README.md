# EFucoser ESP8266 STEP/DIR Firmware Variant

This compatibility sketch supports Wemos D1 mini and NodeMCU controllers using
an A4988, DRV8825, TMC2208, or another STEP/DIR/ENABLE stepper driver.

The repository's primary ESP8266 firmware uses a ULN2003 four-phase driver.

## Wiring

| ESP8266 pad | Connect to |
| --- | --- |
| D1 / GPIO5 | Driver STEP |
| D2 / GPIO4 | Driver DIR |
| D5 / GPIO14 | Driver ENABLE, active low |
| D6 / GPIO12 | Hall sensor output, active low |
| GND | Driver logic GND and motor supply negative |

## Build

```powershell
arduino-cli compile --fqbn esp8266:esp8266:d1_mini ESP8266FocuserFirmware_STEP_DIR
```

The firmware version is `1005`, and the identification string is:

```text
EFucoser ESP8266 Focuser ver 1005
```
