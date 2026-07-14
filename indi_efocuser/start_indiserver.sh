#!/bin/bash
# Launch INDI server with EFucoser Focuser driver
# Usage: ./start_indiserver.sh [serial_port]

PORT="${1:-/dev/ttyUSB0}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
DRIVER="$SCRIPT_DIR/indi_efocuser_focuser.py"

echo "Starting INDI server with EFucoser Focuser driver..."
echo "Driver: $DRIVER"
echo "Port will be selected in INDI client (default: $PORT)"
echo ""

exec indiserver -v "$DRIVER"
