# INDI submission checklist

## Source and build

- [x] Native `INDI::Focuser` implementation
- [x] Serial and TCP connection plugins
- [x] No external runtime dependency beyond INDI core
- [x] LGPL-2.1-or-later source headers
- [x] INDI executable name fixed as `indi_ikun_focuser`
- [x] Driver XML metadata prepared
- [x] INDI core CMake and `drivers.xml` staging tool prepared
- [x] Protocol parser unit tests
- [x] Existing Python driver regression tests
- [x] Current INDI `master` header syntax check with warnings as errors
- [x] Clean and idempotent INDI core staging
- [x] Linux CI workflow prepared
- [ ] Linux x86_64 CI completed successfully
- [ ] Linux ARM64 build completed successfully

## Firmware

- [x] STEP/DIR protocol implementation
- [x] ESP8266 ULN2003 protocol implementation
- [x] Arduino Nano ULN2003 protocol implementation
- [x] `X` speed command implemented consistently
- [x] `A` acceleration command implemented consistently
- [x] Command ranges documented
- [x] STEP/DIR 1005 compiled for Wemos D1 mini
- [x] ESP8266 ULN2003 1103 compiled for Wemos D1 mini
- [x] Arduino Nano ULN2003 1201 compiled for ATmega328P old bootloader
- [ ] STEP/DIR 1005 flashed and tested on hardware
- [ ] ESP8266 ULN2003 1103 flashed and tested on hardware
- [ ] Arduino Nano ULN2003 1201 flashed and tested on hardware

## Client and hardware acceptance

- [ ] Serial handshake and disconnect
- [ ] TCP handshake and disconnect
- [ ] Absolute movement
- [ ] Relative inward and outward movement
- [ ] Abort during movement
- [ ] Position synchronization
- [ ] Maximum travel update and boundary rejection
- [ ] Direction reversal
- [ ] Speed update
- [ ] Acceleration update
- [ ] Motor hold update
- [ ] Temperature reporting
- [ ] Controller power cycle and reconnect
- [ ] USB unplug and reconnect
- [ ] KStars/Ekos connection
- [ ] Ekos autofocus run
- [ ] INDI Web Manager connection
- [ ] `indiserver -vv` log captured
- [ ] Clear screenshots captured for driver documentation

## Pull request material

- [x] User documentation in `doc/index.md`
- [x] Protocol documentation
- [x] Draft INDI PR description
- [x] Draft StellaVita integration request
- [ ] Hardware photographs added to documentation
- [ ] INDI PR URL recorded
- [ ] First INDI release containing the driver recorded
- [ ] StellaVita firmware inclusion confirmed

Items requiring real hardware, Linux CI, an INDI maintainer, or ToupTek remain
open until that external evidence exists.
