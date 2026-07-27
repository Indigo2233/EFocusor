#include "ikunfocuser_protocol.h"

#include <cmath>
#include <cstdlib>
#include <iostream>
#include <string>

namespace
{

void require(bool condition, const std::string &message)
{
    if (!condition)
    {
        std::cerr << "FAIL: " << message << '\n';
        std::exit(1);
    }
}

} // namespace

int main()
{
    IKunFocuserProtocol::MotionStatus status;
    require(IKunFocuserProtocol::parseMotionStatus("P 12345;M true#", status), "moving status parses");
    require(status.position == 12345 && status.moving, "moving status values");
    require(IKunFocuserProtocol::parseMotionStatus("P 0;M false#", status), "stopped status parses");
    require(status.position == 0 && !status.moving, "stopped status values");
    require(!IKunFocuserProtocol::parseMotionStatus("P -1;M false#", status), "negative position rejected");
    require(!IKunFocuserProtocol::parseMotionStatus("garbage", status), "malformed status rejected");

    int version = 0;
    require(IKunFocuserProtocol::parseVersion("V 1103#", version) && version == 1103, "version parses");
    require(!IKunFocuserProtocol::parseVersion("ERR:V#", version), "bad version rejected");
    require(IKunFocuserProtocol::isSupportedVersion(1005), "STEP/DIR release supported");
    require(IKunFocuserProtocol::isSupportedVersion(1103), "ESP8266 ULN2003 release supported");
    require(IKunFocuserProtocol::isSupportedVersion(1201), "Nano release supported");
    require(!IKunFocuserProtocol::isSupportedVersion(1102), "old ULN2003 release rejected");
    require(!IKunFocuserProtocol::isSupportedVersion(1300), "unknown future family rejected");

    const std::string json =
        R"({"firmware":1103,"positionSteps":42,"targetSteps":42,"isMoving":false,"maxSteps":816000,"maxSpeed":800,"acceleration":1000,"reversed":true,"hold":false,"lastTemp":-3.25,"tempSensorPresent":true})";
    int64_t integerValue = 0;
    double numberValue = 0;
    bool booleanValue = false;
    require(IKunFocuserProtocol::parseJsonInteger(json, "positionSteps", integerValue) && integerValue == 42,
            "JSON integer parses");
    require(IKunFocuserProtocol::parseJsonNumber(json, "lastTemp", numberValue) &&
            std::fabs(numberValue + 3.25) < 0.0001, "JSON number parses");
    require(IKunFocuserProtocol::parseJsonBoolean(json, "reversed", booleanValue) && booleanValue,
            "JSON true parses");
    require(IKunFocuserProtocol::parseJsonBoolean(json, "hold", booleanValue) && !booleanValue,
            "JSON false parses");
    require(!IKunFocuserProtocol::parseJsonInteger(json, "missing", integerValue), "missing JSON key rejected");

    require(IKunFocuserProtocol::isErrorResponse("ERR:out_of_range#"), "error response detected");
    require(!IKunFocuserProtocol::isErrorResponse("P 1;M false#"), "normal response accepted");
    require(IKunFocuserProtocol::isSupportedIdentity("IKunFocuser ESP8266 Focuser ver 1005#"),
            "current identity accepted");
    require(IKunFocuserProtocol::isSupportedIdentity("EFucoser ESP8266 Focuser ver 1005#"),
            "legacy identity accepted");
    require(!IKunFocuserProtocol::isSupportedIdentity("Unknown focuser#"), "unknown identity rejected");
    require(std::string(IKunFocuserProtocol::modelForVersion(1005)) == "ESP8266 STEP/DIR", "STEP/DIR model");
    require(std::string(IKunFocuserProtocol::modelForVersion(1103)) == "ESP8266 ULN2003", "ULN2003 model");
    require(std::string(IKunFocuserProtocol::modelForVersion(1201)) == "Arduino Nano ULN2003", "Nano model");

    std::cout << "All IKunFocuser protocol tests passed.\n";
    return 0;
}
