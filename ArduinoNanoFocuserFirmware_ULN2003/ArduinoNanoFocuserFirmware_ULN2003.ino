#include <AccelStepper.h>
#include <DallasTemperature.h>
#include <EEPROM.h>
#include <OneWire.h>
#include <ctype.h>
#include <math.h>

// Arduino Nano pin definitions
#define TEMP_PIN 2    // DS18B20 DATA, with 4.7k pull-up to 5V
#define IN1_PIN 8     // ULN2003 IN1
#define IN2_PIN 9     // ULN2003 IN2
#define IN3_PIN 10    // ULN2003 IN3
#define IN4_PIN 11    // ULN2003 IN4

#define DEVICE_RESPONSE "EFucoser Arduino Nano ULN2003 Focuser ver 1201"
#define FIRMWARE_VERSION 1201
#define SETTINGS_MAGIC 0xEF0C1201UL

struct FocuserSettings {
  uint32_t magic;
  long position;
  int stepsPerRev;
  long maxSteps;
  int maxSpeed;
  int acceleration;
  bool hold;
  bool reversed;
  long homeOffsetSteps;
  bool tempComp;
  float tempCoeff;
  float lastTemp;
};

FocuserSettings settings;
AccelStepper stepper(AccelStepper::HALF4WIRE, IN1_PIN, IN3_PIN, IN2_PIN, IN4_PIN);
OneWire tempOneWire(TEMP_PIN);
DallasTemperature tempSensors(&tempOneWire);

String serialBuffer;
bool positionSaved = true;
bool tempSensorPresent = false;
bool tempConversionPending = false;
unsigned long lastTempSampleMillis = 0;
unsigned long tempRequestMillis = 0;
const unsigned long tempReadIntervalMs = 5000;
const unsigned long tempRetryIntervalMs = 10000;
const unsigned long tempConversionMs = 750;

String boolText(bool value) {
  return value ? "true" : "false";
}

long driverToPhysicalSteps(long driverSteps) {
  return settings.reversed ? -driverSteps : driverSteps;
}

long physicalToDriverSteps(long physicalSteps) {
  return settings.reversed ? -physicalSteps : physicalSteps;
}

long logicalToPhysicalSteps(long logicalSteps) {
  return logicalSteps + settings.homeOffsetSteps;
}

long physicalToLogicalSteps(long physicalSteps) {
  return physicalSteps - settings.homeOffsetSteps;
}

void clampHomeOffset() {
  if (settings.homeOffsetSteps < 0) {
    settings.homeOffsetSteps = 0;
  }
  if (settings.homeOffsetSteps > settings.maxSteps) {
    settings.homeOffsetSteps = settings.maxSteps;
  }
}

void saveSettings() {
  settings.magic = SETTINGS_MAGIC;
  settings.position = driverToPhysicalSteps(stepper.currentPosition());
  EEPROM.put(0, settings);
}

void loadSettings() {
  EEPROM.get(0, settings);
  if (settings.magic != SETTINGS_MAGIC || settings.stepsPerRev <= 0) {
    memset(&settings, 0, sizeof(settings));
    settings.magic = SETTINGS_MAGIC;
    settings.position = 0;
    settings.stepsPerRev = 8160;  // Typical 35BYJ46: 96 half-steps * 85 gearbox ratio
    settings.maxSteps = 816000L;  // 100 output shaft revolutions
    settings.maxSpeed = 800;
    settings.acceleration = 1000;
    settings.hold = false;
    settings.reversed = false;
    settings.homeOffsetSteps = 0;
    settings.tempComp = false;
    settings.tempCoeff = 0.0F;
    settings.lastTemp = 20.0F;
    EEPROM.put(0, settings);
  }
  if (settings.maxSteps <= 0) {
    settings.maxSteps = (long)settings.stepsPerRev * 100L;
  }
  clampHomeOffset();
}

void applyMotionSettings() {
  stepper.setMaxSpeed(settings.maxSpeed);
  stepper.setAcceleration(settings.acceleration);
}

void setReversed(bool reversed) {
  long currentPhysical = driverToPhysicalSteps(stepper.currentPosition());
  long targetPhysical = driverToPhysicalSteps(stepper.targetPosition());
  settings.reversed = reversed;
  stepper.setCurrentPosition(physicalToDriverSteps(currentPhysical));
  stepper.moveTo(physicalToDriverSteps(targetPhysical));
}

void updateMotorOutputs() {
  if (stepper.distanceToGo() != 0 || settings.hold) {
    stepper.enableOutputs();
  } else {
    stepper.disableOutputs();
  }
}

void setCurrentLogicalPosition(long logicalSteps) {
  settings.homeOffsetSteps = driverToPhysicalSteps(stepper.currentPosition()) - logicalSteps;
  clampHomeOffset();
}

bool moveToPhysicalSteps(long target) {
  if (target < 0 || target > settings.maxSteps) {
    return false;
  }
  stepper.enableOutputs();
  stepper.moveTo(physicalToDriverSteps(target));
  positionSaved = false;
  return true;
}

bool moveToLogicalSteps(long logicalTarget) {
  return moveToPhysicalSteps(logicalToPhysicalSteps(logicalTarget));
}

String statusResponse() {
  long pos = physicalToLogicalSteps(driverToPhysicalSteps(stepper.currentPosition()));
  if (pos < 0) pos = 0;
  if (pos > settings.maxSteps) pos = settings.maxSteps;
  String response = "P ";
  response += pos;
  response += ";M ";
  response += boolText(stepper.distanceToGo() != 0);
  response += "#";
  return response;
}

void printStatusJson() {
  Serial.print("{");
  Serial.print("\"firmware\":");
  Serial.print(FIRMWARE_VERSION);
  Serial.print(",\"positionSteps\":");
  Serial.print(physicalToLogicalSteps(driverToPhysicalSteps(stepper.currentPosition())));
  Serial.print(",\"targetSteps\":");
  Serial.print(physicalToLogicalSteps(driverToPhysicalSteps(stepper.targetPosition())));
  Serial.print(",\"isMoving\":");
  Serial.print(boolText(stepper.distanceToGo() != 0));
  Serial.print(",\"home\":false");
  Serial.print(",\"hold\":");
  Serial.print(boolText(settings.hold));
  Serial.print(",\"reversed\":");
  Serial.print(boolText(settings.reversed));
  Serial.print(",\"tempComp\":false");
  Serial.print(",\"stepsPerRev\":");
  Serial.print(settings.stepsPerRev);
  Serial.print(",\"maxSteps\":");
  Serial.print(settings.maxSteps);
  Serial.print(",\"maxSpeed\":");
  Serial.print(settings.maxSpeed);
  Serial.print(",\"acceleration\":");
  Serial.print(settings.acceleration);
  Serial.print(",\"manualStep\":0");
  Serial.print(",\"homeStep\":0");
  Serial.print(",\"homeOffsetSteps\":");
  Serial.print(settings.homeOffsetSteps);
  Serial.print(",\"tempCoeff\":");
  Serial.print(settings.tempCoeff, 2);
  Serial.print(",\"lastTemp\":");
  Serial.print(settings.lastTemp, 2);
  Serial.print(",\"tempSensorPresent\":");
  Serial.print(boolText(tempSensorPresent));
  Serial.print(",\"transport\":\"serial\"");
  Serial.print("}");
}

String statusJson() {
  printStatusJson();
  return "";
}

long commandParameter(String command) {
  if (command.length() <= 1) {
    return 0;
  }
  String param = command.substring(1);
  param.trim();
  return param.toInt();
}

String processCommand(String command) {
  command.trim();
  if (command.endsWith("#")) {
    command.remove(command.length() - 1);
  }
  command.trim();
  if (command.length() == 0) {
    return String(DEVICE_RESPONSE) + "#";
  }

  char code = command.charAt(0);
  long value = commandParameter(command);
  switch (code) {
    case '#':
      return String(DEVICE_RESPONSE) + "#";
    case 'G':
      return statusResponse();
    case 'P':
      setCurrentLogicalPosition(value);
      positionSaved = true;
      saveSettings();
      return statusResponse();
    case 'M':
      if (!moveToLogicalSteps(value)) {
        return "ERR:out_of_range#";
      }
      return statusResponse();
    case 'H':
      return "ERR:home_unavailable#";
    case 'S':
      stepper.stop();
      return "S#";
    case 'R':
      setReversed(value != 0);
      applyMotionSettings();
      saveSettings();
      return String("reversed = ") + boolText(settings.reversed) + "#";
    case 'C':
      settings.hold = value != 0;
      saveSettings();
      updateMotorOutputs();
      return String("hold = ") + boolText(settings.hold) + "#";
    case 'V':
      return String("V ") + FIRMWARE_VERSION + "#";
    case 'I':
      printStatusJson();
      Serial.print("#");
      return "";
    case 'D':
      if (value <= 0) {
        return "ERR:max_steps#";
      }
      settings.maxSteps = value;
      clampHomeOffset();
      saveSettings();
      return String("D ") + settings.maxSteps + "#";
    case 'T':
      settings.tempCoeff = value / 1000.0F;
      settings.tempComp = value != 0;
      saveSettings();
      return String("T ") + String(settings.tempCoeff, 3) + "#";
    case 'E':
      settings.lastTemp = value / 100.0F;
      return String("E ") + String(settings.lastTemp, 2) + "#";
    case 'X':
      if (value <= 0) {
        return "ERR:speed#";
      }
      settings.maxSpeed = (int)value;
      applyMotionSettings();
      saveSettings();
      return String("X ") + settings.maxSpeed + "#";
    case 'A':
      if (value <= 0) {
        return "ERR:acceleration#";
      }
      settings.acceleration = (int)value;
      applyMotionSettings();
      saveSettings();
      return String("A ") + settings.acceleration + "#";
    default:
      return String("ERR:") + code + "#";
  }
}

void handleSerial() {
  while (Serial.available() > 0) {
    char c = (char)Serial.read();
    if (c == '#') {
      Serial.print(processCommand(serialBuffer));
      serialBuffer = "";
    } else if (c != '\r' && c != '\n') {
      serialBuffer += c;
    }
  }
}

void requestTemperatureConversion() {
  tempSensors.requestTemperatures();
  tempRequestMillis = millis();
  tempConversionPending = true;
}

void setupTemperatureSensor() {
  pinMode(TEMP_PIN, INPUT);
  tempSensors.begin();
  tempSensors.setWaitForConversion(false);
  tempSensors.setResolution(12);
  tempSensorPresent = tempSensors.getDeviceCount() > 0;
  if (tempSensorPresent) {
    requestTemperatureConversion();
  }
}

void serviceTemperatureSensor() {
  unsigned long now = millis();

  if (!tempSensorPresent) {
    if (now - lastTempSampleMillis >= tempRetryIntervalMs) {
      lastTempSampleMillis = now;
      tempSensors.begin();
      tempSensorPresent = tempSensors.getDeviceCount() > 0;
      if (tempSensorPresent) {
        requestTemperatureConversion();
      }
    }
    return;
  }

  if (tempConversionPending) {
    if (now - tempRequestMillis < tempConversionMs) {
      return;
    }

    float tempC = tempSensors.getTempCByIndex(0);
    tempConversionPending = false;
    lastTempSampleMillis = now;
    if (tempC != DEVICE_DISCONNECTED_C && tempC >= -55.0F && tempC <= 125.0F) {
      settings.lastTemp = tempC;
    } else {
      tempSensorPresent = false;
    }
    return;
  }

  if (now - lastTempSampleMillis >= tempReadIntervalMs) {
    requestTemperatureConversion();
  }
}

void setup() {
  Serial.begin(9600);
  Serial.setTimeout(2000);

  loadSettings();
  stepper.setCurrentPosition(physicalToDriverSteps(settings.position));
  applyMotionSettings();
  updateMotorOutputs();
  setupTemperatureSensor();

  Serial.println();
  Serial.println(DEVICE_RESPONSE);
}

void loop() {
  handleSerial();
  serviceTemperatureSensor();
  updateMotorOutputs();
  stepper.run();

  if (stepper.distanceToGo() == 0 && !positionSaved) {
    positionSaved = true;
    saveSettings();
  }
}
