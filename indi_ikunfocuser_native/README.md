# IKunFocuser native INDI driver

Native C++ INDI focuser driver for IKunFocuser controllers. The driver is prepared
for submission to the INDI core repository and has no dependencies beyond
libindi and the C++ standard library.

## Supported controllers

| Controller | Minimum firmware | Connection |
| --- | ---: | --- |
| ESP8266 STEP/DIR | 1005 | USB serial or TCP |
| ESP8266 ULN2003 | 1103 | USB serial or TCP |
| Arduino Nano ULN2003 | 1201 | USB serial |

Serial settings are 9600-8-N-1. ESP8266 TCP connections use port 4030 and
default to `192.168.4.1`.

## Driver features

- Absolute and relative movement
- Abort and position synchronization
- Configurable maximum travel
- Direction reversal
- Configurable maximum speed and acceleration
- Motor hold control
- Temperature reporting
- Firmware and controller identification
- Serial and TCP connection plugins supplied by INDI

## Standalone Linux build

Install libindi development files, CMake, and a C++ compiler, then run:

```bash
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build
ctest --test-dir build --output-on-failure
sudo cmake --install build
```

Start the driver with:

```bash
indiserver -vv indi_ikun_focuser
```

## INDI core submission

The staging tool copies the driver into a clean INDI checkout and adds the
required build and discovery entries:

```bash
python tools/stage_indi_core.py /path/to/indi
```

Review the resulting INDI diff and build the `indi_ikun_focuser` target.
See [SUBMISSION_CHECKLIST.md](SUBMISSION_CHECKLIST.md) for the complete release
gate.

On Windows, the current INDI headers can also be checked without linking a
driver binary:

```powershell
.\tools\check_upstream_headers.ps1 -IndiCheckout C:\path\to\indi
```

## License

The native driver is licensed under LGPL-2.1-or-later, matching the license
headers in each source file.
