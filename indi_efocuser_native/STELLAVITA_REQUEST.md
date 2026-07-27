# StellaVita driver inclusion request

Subject: Request to include the EFucoser INDI focuser driver in StellaVita

Hello ToupTek/StellaVita team,

Please include the EFucoser electronic focuser driver in a future StellaVita
firmware release.

Driver information:

- INDI device name: `EFucoser Focuser`
- Executable: `indi_efucoser_focuser`
- Device class: Focuser
- INDI pull request: [fill in]
- INDI merge commit: [fill in]
- First INDI release: [fill in]
- Firmware repository: https://github.com/Indigo2233/EFocusor

Supported connections:

- USB serial, 9600-8-N-1
- TCP, default host `192.168.4.1`, port `4030`

Supported controller firmware:

- ESP8266 STEP/DIR 1005 or later
- ESP8266 ULN2003 1103 or later
- Arduino Nano ULN2003 1201–1299

Validation supplied with this request:

- ARM64 build result: [fill in]
- StellaVita test result: [fill in]
- KStars/Ekos test result: [fill in]
- Hardware photographs and logs: [attach]

Please confirm the StellaVita firmware version planned to contain this driver
and whether any additional device metadata is required.
