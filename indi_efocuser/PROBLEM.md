# INDI Driver Problem Report

## Resolution

Property forwarding was resolved in driver version 1.1.0. KStars property
parsing was resolved in version 1.1.1.

- The driver now responds to `getProperties`, including device-wide and
  property-specific requests. `indiserver` routes definitions according to
  each client's request and does not replay previously emitted definitions.
- Focuser vectors and elements now use the names from the INDI 2.2.3 focuser
  interface, including `ABS_FOCUS_POSITION`, `REL_FOCUS_POSITION`,
  `FOCUS_ABORT_MOTION`, and `FOCUS_REVERSE_MOTION`.
- Absolute-position updates report `Busy` while the firmware is moving and
  `Ok` after movement completes.
- Protocol regression tests cover property discovery and standard absolute and
  relative movement commands.
- Property definition vectors now contain the INDI definition widgets
  `defNumber`, `defSwitch`, and `defText`. Update and command vectors continue
  to use `oneNumber`, `oneSwitch`, and `oneText`.

## Project
- **Name**: EFucoser INDI Focuser Driver
- **Repo**: https://github.com/Indigo2233/EFocusor
- **File**: `indi_efocuser/indi_efocuser_focuser.py` (441 lines, pure Python)
- **Environment**: indiserver v2.2.3, protocol 1.7, Arch Linux

## What Works
- Firmware (Arduino Nano ULN2003) compiles and runs correctly
- Driver standalone test (direct stdin/stdout, no indiserver): fully functional
  - Connects to Arduino Nano via `/dev/ttyUSB0` at 9600 baud
  - Sends correct `defNumberVector` / `defSwitchVector` / `setNumberVector` XML
  - Polls position every 0.5s, updates temperature
- Driver declares `DRIVER_INFO` with `DRIVER_INTERFACE=2` (FOCUSER_INTERFACE)

## The Problem
When running under `indiserver`, focuser-specific properties (FOCUS_SPEED,
ACCELERATION, FOCUS_MAX, FOCUS_POSITION, etc.) are **never forwarded to INDI
clients**. Only `setSwitchVector(CONNECTION)` reaches the client.

## Evidence (from `indiserver -vvv` logs)

### 1. indiserver DOES read all properties from the driver's stdout:
```
read defNumberVector EFucoser Focuser FOCUS_SPEED Idle rw  VALUE='500'
read setNumberVector EFucoser Focuser FOCUS_SPEED Ok
read defNumberVector EFucoser Focuser ACCELERATION Idle rw  VALUE='800'
read setNumberVector EFucoser Focuser ACCELERATION Ok
read defNumberVector EFucoser Focuser FOCUS_MAX Idle rw  VALUE='816000'
read setNumberVector EFucoser Focuser FOCUS_MAX Ok
read defNumberVector EFucoser Focuser FOCUS_POSITION Idle ro  VALUE='0'
read setNumberVector EFucoser Focuser FOCUS_POSITION Ok
... (all 12 focuser properties are read correctly)
```

### 2. But indiserver only queues the CONNECTION update for the client:
```
Client 9: queuing <setSwitchVector device='EFucoser Focuser' name='CONNECTION'>
Client 9: sending msg nq 1:
```
No focuser properties are ever queued or sent to the client.

## What We've Tried (none helped)

1. `defNumberVector` immediately followed by `setNumberVector` (push values)
2. `sys.stdout.flush()` + `time.sleep(0.5)` delay between def and set
3. Responding to `getProperties` with both `define_all()` and `define_focus()`
4. NOT responding to `getProperties` (relying on indiserver cache)
5. Single-line vs multi-line XML format
6. Defining `DRIVER_INFO` with correct `DRIVER_INTERFACE=2`

## The Core Question
Why does indiserver read and store `defNumberVector` / `setNumberVector` from
the driver's stdout, but not push them to connected clients? Standard INDI
drivers that dynamically define new properties after connection should have
them auto-forwarded to all clients.
