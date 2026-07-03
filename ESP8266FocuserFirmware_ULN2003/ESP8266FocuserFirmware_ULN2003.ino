#include <AccelStepper.h>
#include <EEPROM.h>
#include <ESP8266WebServer.h>
#include <ESP8266WiFi.h>
#include <OneWire.h>
#include <WebSocketsServer.h>
#include <DallasTemperature.h>
#include <ctype.h>
#include <math.h>

// Pin definitions - GPIO numbers for NodeMCU/Wemos D1 mini
// D0=16, D1=5, D2=4, D3=0, D4=2, D5=14, D6=12, D7=13
#define IN1_PIN 5     // D1 -> ULN2003 IN1
#define IN2_PIN 4     // D2 -> ULN2003 IN2
#define IN3_PIN 14    // D5 -> ULN2003 IN3
#define IN4_PIN 12    // D6 -> ULN2003 IN4
#define CW_PIN 13     // D7, manual inward button
#define CCW_PIN 0     // D3 (GPIO0), manual outward button. Must stay HIGH during boot.
#define TEMP_PIN 2    // D4, DS18B20 DATA with 4.7k pull-up to 3.3V
#define USE_HALL_SENSOR 0
#define HALL_PIN 16   // D0, optional Hall input. Requires external 10k pull-up to 3.3V.

#define DEVICE_RESPONSE "EFucoser ESP8266 ULN2003 Focuser ver 1101"
#define FIRMWARE_VERSION 1101
#define EEPROM_SIZE 512
#define SETTINGS_MAGIC 0xEF0C2003UL
#define ASCOM_TCP_PORT 4030
#define WEBSOCKET_PORT 81
#define MAX_TCP_CLIENTS 4

const char *AP_PASSWORD = "012345678";
IPAddress apIp(192, 168, 4, 1);
IPAddress apGateway(192, 168, 4, 1);
IPAddress apSubnet(255, 255, 255, 0);

struct FocuserSettings {
  uint32_t magic;
  long position;
  int stepsPerRev;
  long maxSteps;
  int maxSpeed;
  int acceleration;
  int manualMoveStepSize;
  int findHomeStepSize;
  bool hold;
  bool reversed;
  char staSsid[32];
  char staPassword[64];
  char staIp[16];       // static IP, empty = DHCP
  char staGateway[16];
  char staSubnet[16];
  long homeOffsetSteps;
  bool tempComp;
  float tempCoeff;
  float lastTemp;
};

FocuserSettings settings;
AccelStepper stepper(AccelStepper::HALF4WIRE, IN1_PIN, IN3_PIN, IN2_PIN, IN4_PIN);
ESP8266WebServer server(80);
WebSocketsServer webSocket(WEBSOCKET_PORT);
WiFiServer tcpServer(ASCOM_TCP_PORT);
WiFiClient tcpClients[MAX_TCP_CLIENTS];
OneWire tempOneWire(TEMP_PIN);
DallasTemperature tempSensors(&tempOneWire);

String tcpBuffers[MAX_TCP_CLIENTS];
String serialBuffer;
String apSsid;

bool positionSaved = true;
bool findingHome = false;
bool homeFound = false;
bool manualMoveCW = false;
bool manualMoveCCW = false;
int lastCWState = HIGH;
int lastCCWState = HIGH;
unsigned long lastCWDebounce = 0;
unsigned long lastCCWDebounce = 0;
unsigned long lastStatusBroadcast = 0;
unsigned long lastTempSampleMillis = 0;
unsigned long tempRequestMillis = 0;
bool tempSensorPresent = false;
bool tempConversionPending = false;
const unsigned long debounceDelayMs = 20;
const unsigned long tempReadIntervalMs = 5000;
const unsigned long tempRetryIntervalMs = 10000;
const unsigned long tempConversionMs = 750;

const char INDEX_HTML[] PROGMEM = R"rawliteral(
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover">
<title>EFucoser Focuser</title>
<style>
:root{color-scheme:dark;--bg:#111318;--panel:#1a1f28;--panel2:#202734;--line:#384252;--text:#f2f5f8;--muted:#aeb8c6;--accent:#4db6ac;--warn:#f4b24e;--danger:#ee6b63;--ok:#7bc96f}
body.red{--accent:#cc4444;--ok:#bb3333;--warn:#d4883a;--danger:#881111;--muted:#996666;--text:#eecccc;--panel2:#2a1a1a;--line:#4a3030}
body.red .progress-fill{background:var(--accent)}
*{box-sizing:border-box}
body{margin:0;background:var(--bg);color:var(--text);font-family:system-ui,-apple-system,Segoe UI,Roboto,Arial,sans-serif;letter-spacing:0;transition:color .3s}
main{max-width:720px;margin:0 auto;padding:18px 14px 28px}
header{display:flex;align-items:center;justify-content:space-between;gap:12px;margin-bottom:14px}
h1{font-size:22px;margin:0;font-weight:680}
.status{font-size:13px;color:var(--muted)}
.grid{display:grid;grid-template-columns:1fr;gap:10px}
section{border:1px solid var(--line);background:var(--panel);border-radius:8px;padding:12px;transition:background .3s,border-color .3s}
.readout{display:grid;grid-template-columns:1fr 1fr;gap:10px}
.metric{background:var(--panel2);border:1px solid var(--line);border-radius:8px;padding:10px;min-height:76px;transition:background .3s,border-color .3s}
.metric span{display:block;color:var(--muted);font-size:12px;margin-bottom:8px}
.metric strong{font-size:22px;line-height:1.2;white-space:nowrap}
.row{display:flex;gap:8px;align-items:center;flex-wrap:wrap}
.controls{display:grid;grid-template-columns:repeat(3,1fr);gap:8px}
button,input,select{font:inherit}
button{border:1px solid var(--line);background:#263244;color:var(--text);border-radius:8px;min-height:44px;padding:0 12px;cursor:pointer;transition:background .3s,border-color .3s}
button:active{transform:translateY(1px)}
button.primary{background:var(--accent);border-color:var(--accent);color:#111;font-weight:700}
button.warn{background:var(--warn);border-color:var(--warn);color:#111;font-weight:700}
button.danger{background:var(--danger);border-color:var(--danger);color:#fcc;font-weight:700}
button.toggle.active{background:var(--ok);border-color:var(--ok);color:#111;font-weight:700}
button.night{background:transparent;border-color:transparent;font-size:20px;min-height:36px;padding:0 8px;line-height:1}
label{display:grid;gap:5px;color:var(--muted);font-size:12px;min-width:0}
input{width:100%;background:#121720;color:var(--text);border:1px solid var(--line);border-radius:8px;min-height:42px;padding:8px 10px;transition:background .3s}
.form{display:grid;grid-template-columns:1fr 1fr;gap:10px}
.wide{grid-column:1/-1}
.small{font-size:12px;color:var(--muted)}
.progress-bar{width:100%;height:12px;background:var(--panel2);border:1px solid var(--line);border-radius:6px;overflow:hidden;margin-top:8px;transition:background .3s}
.progress-fill{height:100%;background:var(--accent);border-radius:6px;transition:width .3s,background .3s}
@media (min-width:680px){.grid{grid-template-columns:1fr 1fr}.span{grid-column:1/-1}}
</style>
</head>
<body>
<main>
<header>
<h1>EFucoser Focuser</h1>
<button class="night" id="nightToggle" title="Red light mode">🌙</button>
<div class="status" id="link">Connecting</div>
</header>
<div class="grid">
<section class="span">
<div class="readout">
<div class="metric"><span>Position</span><strong id="position">--</strong></div>
<div class="metric"><span>Max Steps</span><strong id="maxSteps">--</strong></div>
<div class="metric"><span>Temperature</span><strong id="temperature">--</strong></div>
</div>
<div class="progress-bar"><div class="progress-fill" id="progressFill" style="width:0%"></div></div>
</section>
<section>
<div class="form">
<label class="wide">Target Position (steps)<input id="targetPosition" type="number" min="0" step="1" value="0"></label>
<button class="primary wide" id="moveAbsolute">Move</button>
<button data-rel="-1000">-1000</button><button data-rel="-100">-100</button><button data-rel="-10">-10</button>
<button data-rel="10">+10</button><button data-rel="100">+100</button><button data-rel="1000">+1000</button>
<button class="danger" id="halt">STOP</button><button id="home">Home</button><button id="setZero">Set 0</button>
</div>
</section>
<section>
<div class="form">
<button class="toggle" id="hold">Hold</button>
<button class="toggle" id="reverse">Reverse</button>
<label>Steps/rev<input id="stepsPerRev" type="number" min="1" step="1"></label>
<label>Max Speed<input id="maxSpeed" type="number" min="1" step="1"></label>
<label>Acceleration<input id="acceleration" type="number" min="1" step="1"></label>
<label>Manual step<input id="manualStep" type="number" min="1" step="1"></label>
<label>STA SSID<input id="staSsid" type="text" maxlength="31"></label>
<label>STA Password<input id="staPassword" type="password" maxlength="63"></label>
<label>Static IP<input id="staIp" type="text" maxlength="15" placeholder="DHCP if empty"></label>
<label>Gateway<input id="staGateway" type="text" maxlength="15" placeholder="192.168.4.1"></label>
<label>Subnet<input id="staSubnet" type="text" maxlength="15" placeholder="255.255.255.0"></label>
<button class="primary wide" id="saveSettings">Save settings</button>
<div class="small wide" id="network"></div>
</div>
</section>
</div>
</main>
<script>
const $=id=>document.getElementById(id);
let state={};
async function api(path,body){
 const opt=body===undefined?{}:{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)};
 const res=await fetch(path,opt);
 if(!res.ok)throw new Error(path);
 return res.json();
}
function setState(s){
 state=s;
 $('position').textContent=String(s.positionSteps??0);
 $('maxSteps').textContent=String(s.maxSteps??0);
 const temp=Number(s.lastTemp);
 $('temperature').textContent=Number.isFinite(temp)?temp.toFixed(2)+' C':'--';
 const pct=s.maxSteps>0?((s.positionSteps??0)/s.maxSteps*100):0;
 $('progressFill').style.width=Math.min(100,Math.max(0,pct))+'%';
 $('link').textContent=s.isMoving?'Moving':'Ready';
 $('hold').classList.toggle('active',!!s.hold);
 $('reverse').classList.toggle('active',!!s.reversed);
 $('stepsPerRev').value=s.stepsPerRev??200;
 $('maxSpeed').value=s.maxSpeed??800;
 $('acceleration').value=s.acceleration??1000;
 $('manualStep').value=s.manualStep??50;
 if(document.activeElement!==$('staSsid'))$('staSsid').value=s.staSsid||'';
 if(document.activeElement!==$('staIp'))$('staIp').value=s.staIp||'';
 if(document.activeElement!==$('staGateway'))$('staGateway').value=s.staGateway||'';
 if(document.activeElement!==$('staSubnet'))$('staSubnet').value=s.staSubnet||'';
 const staIp=s.staIp&&s.staIp!=='0.0.0.0'?s.staIp:'not connected';
 $('network').textContent='AP: '+(s.apIp||'192.168.4.1')+' | STA: '+staIp+' | TCP: '+(s.tcpPort||4030);
}
async function refresh(){try{setState(await api('/api/status'));}catch(e){$('link').textContent='Disconnected';}}
function connectWs(){
 const ws=new WebSocket('ws://'+location.hostname+':81/');
 ws.onopen=()=>{$('link').textContent='Connected'};
 ws.onmessage=e=>{try{setState(JSON.parse(e.data));}catch(_){}};
 ws.onclose=()=>{setTimeout(connectWs,1200);};
}
$('moveAbsolute').onclick=()=>api('/api/move',{steps:Number($('targetPosition').value)}).then(setState);
document.querySelectorAll('[data-rel]').forEach(b=>b.onclick=()=>api('/api/move',{relativeSteps:Number(b.dataset.rel)}).then(setState));
$('halt').onclick=()=>api('/api/halt',{}).then(setState);
$('home').onclick=()=>api('/api/home',{}).then(setState);
$('setZero').onclick=()=>api('/api/set-position',{steps:0}).then(setState);
$('hold').onclick=()=>api('/api/settings',{hold:!state.hold}).then(setState);
$('reverse').onclick=()=>api('/api/settings',{reversed:!state.reversed}).then(setState);
$('saveSettings').onclick=()=>{
 const body={
  stepsPerRev:Number($('stepsPerRev').value),
  maxSpeed:Number($('maxSpeed').value),
  acceleration:Number($('acceleration').value),
  manualStep:Number($('manualStep').value)
 };
 if($('staSsid').value.trim())body.staSsid=$('staSsid').value.trim();
 if($('staPassword').value)body.staPassword=$('staPassword').value;
 if($('staIp').value.trim())body.staIp=$('staIp').value.trim();
 if($('staGateway').value.trim())body.staGateway=$('staGateway').value.trim();
 if($('staSubnet').value.trim())body.staSubnet=$('staSubnet').value.trim();
 api('/api/settings',body).then(setState);
};
// Red light (night) mode - persisted in localStorage
(function(){
 const night=localStorage.getItem('efucoser_night')==='1';
 if(night)document.body.classList.add('red');
 $('nightToggle').textContent=night?'☀️':'🌙';
 $('nightToggle').onclick=()=>{
  const on=document.body.classList.toggle('red');
  $('nightToggle').textContent=on?'☀️':'🌙';
  localStorage.setItem('efucoser_night',on?'1':'0');
 };
})();
connectWs();
refresh();
setInterval(refresh,5000);
</script>
</body>
</html>
)rawliteral";

// ==================== Settings Management ====================

void saveSettings() {
  settings.magic = SETTINGS_MAGIC;
  settings.position = stepper.currentPosition();
  EEPROM.put(0, settings);
  EEPROM.commit();
}

void loadSettings() {
  EEPROM.begin(EEPROM_SIZE);
  EEPROM.get(0, settings);
  if (settings.magic != SETTINGS_MAGIC || settings.stepsPerRev <= 0) {
    memset(&settings, 0, sizeof(settings));
    settings.magic = SETTINGS_MAGIC;
    settings.position = 0;
    settings.stepsPerRev = 8160;  // Typical 35BYJ46: 96 half-steps * 85 gearbox ratio
    settings.maxSteps = 816000L;  // 100 output shaft revolutions
    settings.maxSpeed = 800;
    settings.acceleration = 1000;
    settings.manualMoveStepSize = 100;
    settings.findHomeStepSize = 200;
    settings.hold = false;
    settings.reversed = false;
    settings.homeOffsetSteps = 0;
    settings.tempComp = false;
    settings.tempCoeff = 0.0F;
    settings.lastTemp = 20.0F;
    EEPROM.put(0, settings);
    EEPROM.commit();
  }
  if (settings.maxSteps <= 0) {
    settings.maxSteps = (long)settings.stepsPerRev * 100L;
  }
  if (settings.homeOffsetSteps < 0 || settings.homeOffsetSteps > settings.maxSteps) {
    settings.homeOffsetSteps = 0;
  }
}

void applyMotionSettings() {
  stepper.setMaxSpeed(settings.maxSpeed);
  stepper.setAcceleration(settings.acceleration);
}

long driverToPhysicalSteps(long driverSteps) {
  return settings.reversed ? -driverSteps : driverSteps;
}

long physicalToDriverSteps(long physicalSteps) {
  return settings.reversed ? -physicalSteps : physicalSteps;
}

void setReversed(bool reversed) {
  long currentPhysical = driverToPhysicalSteps(stepper.currentPosition());
  long targetPhysical = driverToPhysicalSteps(stepper.targetPosition());
  settings.reversed = reversed;
  stepper.setCurrentPosition(physicalToDriverSteps(currentPhysical));
  stepper.moveTo(physicalToDriverSteps(targetPhysical));
}

void updateMotorOutputs() {
  if (stepper.distanceToGo() != 0 || findingHome || settings.hold) {
    stepper.enableOutputs();
  } else {
    stepper.disableOutputs();
  }
}

// ==================== Position Helpers ====================

bool hallTriggered() {
#if USE_HALL_SENSOR
  return digitalRead(HALL_PIN) == LOW;
#else
  return false;
#endif
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
  long physicalTarget = logicalToPhysicalSteps(logicalTarget);
  return moveToPhysicalSteps(physicalTarget);
}

// ==================== Status / JSON ====================

String boolText(bool value) {
  return value ? "true" : "false";
}

String statusResponse() {
  long pos = physicalToLogicalSteps(driverToPhysicalSteps(stepper.currentPosition()));
  // Clamp to valid range [0, maxSteps]
  if (pos < 0) pos = 0;
  if (pos > settings.maxSteps) pos = settings.maxSteps;
  String response = "P ";
  response += pos;
  response += ";M ";
  response += boolText(stepper.distanceToGo() != 0 || findingHome);
  response += "#";
  return response;
}

String ipToString(const IPAddress &ip) {
  return String(ip[0]) + "." + String(ip[1]) + "." + String(ip[2]) + "." + String(ip[3]);
}

String statusJson() {
  String json = "{";
  json += "\"firmware\":";
  json += FIRMWARE_VERSION;
  json += ",\"positionSteps\":";
  json += physicalToLogicalSteps(driverToPhysicalSteps(stepper.currentPosition()));
  json += ",\"targetSteps\":";
  json += physicalToLogicalSteps(driverToPhysicalSteps(stepper.targetPosition()));
  json += ",\"isMoving\":";
  json += boolText(stepper.distanceToGo() != 0 || findingHome);
  json += ",\"home\":";
  json += boolText(hallTriggered());
  json += ",\"hold\":";
  json += boolText(settings.hold);
  json += ",\"reversed\":";
  json += boolText(settings.reversed);
  json += ",\"tempComp\":";
  json += boolText(settings.tempComp);
  json += ",\"stepsPerRev\":";
  json += settings.stepsPerRev;
  json += ",\"maxSteps\":";
  json += settings.maxSteps;
  json += ",\"maxSpeed\":";
  json += settings.maxSpeed;
  json += ",\"acceleration\":";
  json += settings.acceleration;
  json += ",\"manualStep\":";
  json += settings.manualMoveStepSize;
  json += ",\"homeStep\":";
  json += settings.findHomeStepSize;
  json += ",\"homeOffsetSteps\":";
  json += settings.homeOffsetSteps;
  json += ",\"tempCoeff\":";
  json += String(settings.tempCoeff, 2);
  json += ",\"lastTemp\":";
  json += String(settings.lastTemp, 2);
  json += ",\"tempSensorPresent\":";
  json += boolText(tempSensorPresent);
  json += ",\"apSsid\":\"";
  json += apSsid;
  json += "\",\"apIp\":\"";
  json += ipToString(WiFi.softAPIP());
  json += "\",\"staIp\":\"";
  json += ipToString(WiFi.localIP());
  json += "\",\"staSsid\":\"";
  json += settings.staSsid;
  json += "\",\"staIp\":\"";
  json += settings.staIp;
  json += "\",\"staGateway\":\"";
  json += settings.staGateway;
  json += "\",\"staSubnet\":\"";
  json += settings.staSubnet;
  json += "\",\"tcpPort\":";
  json += ASCOM_TCP_PORT;
  json += "}";
  return json;
}

void broadcastStatus() {
  String json = statusJson();
  webSocket.broadcastTXT(json);
}

// ==================== Command Processing ====================

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
    return "ERR:empty#";
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
      broadcastStatus();
      return statusResponse();
    case 'M':
      if (!moveToLogicalSteps(value)) {
        return "ERR:out_of_range#";
      }
      broadcastStatus();
      return statusResponse();
    case 'H':
#if USE_HALL_SENSOR
      findingHome = true;
      homeFound = false;
      broadcastStatus();
      return "H false#";
#else
      return "ERR:home_unavailable#";
#endif
    case 'S':
      stepper.stop();
      findingHome = false;
      broadcastStatus();
      return "S#";
    case 'R':
      setReversed(value != 0);
      applyMotionSettings();
      saveSettings();
      broadcastStatus();
      return String("reversed = ") + boolText(settings.reversed) + "#";
    case 'C':
      settings.hold = value != 0;
      saveSettings();
      updateMotorOutputs();
      broadcastStatus();
      return String("hold = ") + boolText(settings.hold) + "#";
    case 'V':
      return String("V ") + FIRMWARE_VERSION + "#";
    case 'I':
      return statusJson() + "#";
    case 'D':
      {
        // D <maxSteps># - set max steps and recompute range
        if (value <= 0) {
          return "ERR:max_steps#";
        }
        settings.maxSteps = value;
        clampHomeOffset();
        saveSettings();
        broadcastStatus();
        return String("D ") + settings.maxSteps + "#";
      }
    case 'T':
      {
        // T <tempCoeff * 1000># - set temperature coefficient
        settings.tempCoeff = value / 1000.0F;
        if (value != 0) settings.tempComp = true;
        else settings.tempComp = false;
        saveSettings();
        broadcastStatus();
        return String("T ") + String(settings.tempCoeff, 3) + "#";
      }
    case 'E':
      {
        // E <temp * 100># - update current temperature for compensation
        settings.lastTemp = value / 100.0F;
        broadcastStatus();
        return String("E ") + String(settings.lastTemp, 2) + "#";
      }
    default:
      return String("ERR:") + code + "#";
  }
}

// ==================== HTTP API ====================

void sendJson(int code, const String &json) {
  server.sendHeader("Cache-Control", "no-store");
  server.sendHeader("Access-Control-Allow-Origin", "*");
  server.send(code, "application/json", json);
}

bool extractNumber(const String &body, const String &key, double &value) {
  int keyIndex = body.indexOf("\"" + key + "\"");
  if (keyIndex < 0) {
    return false;
  }
  int colon = body.indexOf(':', keyIndex);
  if (colon < 0) {
    return false;
  }
  int start = colon + 1;
  while (start < (int)body.length() && isspace(body.charAt(start))) {
    start++;
  }
  int end = start;
  while (end < (int)body.length()) {
    char c = body.charAt(end);
    if ((c >= '0' && c <= '9') || c == '-' || c == '+' || c == '.') {
      end++;
    } else {
      break;
    }
  }
  if (end == start) {
    return false;
  }
  value = body.substring(start, end).toFloat();
  return true;
}

bool extractBool(const String &body, const String &key, bool &value) {
  int keyIndex = body.indexOf("\"" + key + "\"");
  if (keyIndex < 0) {
    return false;
  }
  int colon = body.indexOf(':', keyIndex);
  if (colon < 0) {
    return false;
  }
  String tail = body.substring(colon + 1);
  tail.trim();
  if (tail.startsWith("true") || tail.startsWith("1")) {
    value = true;
    return true;
  }
  if (tail.startsWith("false") || tail.startsWith("0")) {
    value = false;
    return true;
  }
  return false;
}

bool extractString(const String &body, const String &key, char *dest, size_t destSize) {
  int keyIndex = body.indexOf("\"" + key + "\"");
  if (keyIndex < 0) {
    return false;
  }
  int colon = body.indexOf(':', keyIndex);
  int firstQuote = body.indexOf('"', colon + 1);
  int secondQuote = body.indexOf('"', firstQuote + 1);
  if (colon < 0 || firstQuote < 0 || secondQuote < 0 || destSize == 0) {
    return false;
  }
  String value = body.substring(firstQuote + 1, secondQuote);
  value.toCharArray(dest, destSize);
  dest[destSize - 1] = '\0';
  return true;
}

void handleRoot() {
  server.send_P(200, "text/html", INDEX_HTML);
}

void handleStatus() {
  sendJson(200, statusJson());
}

void handleMoveApi() {
  String body = server.arg("plain");
  double value;
  bool ok = false;
  if (extractNumber(body, "steps", value)) {
    ok = moveToLogicalSteps(lround(value));
  } else if (extractNumber(body, "relativeSteps", value)) {
    long target = physicalToLogicalSteps(driverToPhysicalSteps(stepper.currentPosition())) + lround(value);
    if (target < 0) target = 0;
    if (target > settings.maxSteps) target = settings.maxSteps;
    ok = moveToLogicalSteps(target);
  }

  if (!ok) {
    sendJson(400, "{\"error\":\"invalid_move\"}");
    return;
  }
  broadcastStatus();
  sendJson(200, statusJson());
}

void handleHaltApi() {
  stepper.stop();
  findingHome = false;
  broadcastStatus();
  sendJson(200, statusJson());
}

void handleHomeApi() {
  moveToLogicalSteps(0);  // Move to saved zero position
  broadcastStatus();
  sendJson(200, statusJson());
}

void handleSetPositionApi() {
  String body = server.arg("plain");
  double value;
  if (extractNumber(body, "steps", value)) {
    setCurrentLogicalPosition(lround(value));
  } else {
    sendJson(400, "{\"error\":\"invalid_position\"}");
    return;
  }
  positionSaved = true;
  saveSettings();
  broadcastStatus();
  sendJson(200, statusJson());
}

void handleSettingsGetApi() {
  sendJson(200, statusJson());
}

void handleSettingsPostApi() {
  String body = server.arg("plain");
  double numberValue;
  bool boolValue;

  if (extractNumber(body, "stepsPerRev", numberValue) && numberValue > 0) {
    settings.stepsPerRev = (int)numberValue;
  }
  if (extractNumber(body, "maxSpeed", numberValue) && numberValue > 0) {
    settings.maxSpeed = (int)numberValue;
  }
  if (extractNumber(body, "acceleration", numberValue) && numberValue > 0) {
    settings.acceleration = (int)numberValue;
  }
  if (extractNumber(body, "manualStep", numberValue) && numberValue > 0) {
    settings.manualMoveStepSize = (int)numberValue;
  }
  if (extractNumber(body, "homeStep", numberValue) && numberValue > 0) {
    settings.findHomeStepSize = (int)numberValue;
  }
  if (extractNumber(body, "tempCoeff", numberValue)) {
    settings.tempCoeff = (float)numberValue;
  }
  if (extractBool(body, "hold", boolValue)) {
    settings.hold = boolValue;
  }
  if (extractBool(body, "reversed", boolValue)) {
    setReversed(boolValue);
  }
  if (extractBool(body, "tempComp", boolValue)) {
    settings.tempComp = boolValue;
  }
  bool staChanged = false;
  staChanged |= extractString(body, "staSsid", settings.staSsid, sizeof(settings.staSsid));
  staChanged |= extractString(body, "staPassword", settings.staPassword, sizeof(settings.staPassword));
  staChanged |= extractString(body, "staIp", settings.staIp, sizeof(settings.staIp));
  staChanged |= extractString(body, "staGateway", settings.staGateway, sizeof(settings.staGateway));
  staChanged |= extractString(body, "staSubnet", settings.staSubnet, sizeof(settings.staSubnet));

  applyMotionSettings();
  saveSettings();
  updateMotorOutputs();

  if (staChanged && strlen(settings.staSsid) > 0) {
    IPAddress ip, gw, sn;
    if (strlen(settings.staIp) > 0
        && ip.fromString(settings.staIp)
        && gw.fromString(settings.staGateway)
        && sn.fromString(settings.staSubnet)) {
      WiFi.config(ip, gw, sn);
    }
    WiFi.begin(settings.staSsid, settings.staPassword);
  }

  broadcastStatus();
  sendJson(200, statusJson());
}

void handleOptions() {
  server.sendHeader("Access-Control-Allow-Origin", "*");
  server.sendHeader("Access-Control-Allow-Methods", "GET,POST,OPTIONS");
  server.sendHeader("Access-Control-Allow-Headers", "Content-Type");
  server.send(204, "text/plain", "");
}

void setupHttp() {
  server.on("/", HTTP_GET, handleRoot);
  server.on("/api/status", HTTP_GET, handleStatus);
  server.on("/api/move", HTTP_POST, handleMoveApi);
  server.on("/api/halt", HTTP_POST, handleHaltApi);
  server.on("/api/home", HTTP_POST, handleHomeApi);
  server.on("/api/set-position", HTTP_POST, handleSetPositionApi);
  server.on("/api/settings", HTTP_GET, handleSettingsGetApi);
  server.on("/api/settings", HTTP_POST, handleSettingsPostApi);
  server.onNotFound([]() {
    if (server.method() == HTTP_OPTIONS) {
      handleOptions();
      return;
    }
    server.send(404, "text/plain", "Not found");
  });
  server.begin();
}

void setupWifi() {
  apSsid = "Focuser-" + String(ESP.getChipId(), HEX);
  WiFi.mode(WIFI_AP_STA);
  WiFi.softAPConfig(apIp, apGateway, apSubnet);
  WiFi.softAP(apSsid.c_str(), AP_PASSWORD);
  if (strlen(settings.staSsid) > 0) {
    // Use static IP if configured, otherwise DHCP
    IPAddress staIpAddr, staGw, staSn;
    if (strlen(settings.staIp) > 0
        && staIpAddr.fromString(settings.staIp)
        && staGw.fromString(settings.staGateway)
        && staSn.fromString(settings.staSubnet)) {
      WiFi.config(staIpAddr, staGw, staSn);
    }
    WiFi.begin(settings.staSsid, settings.staPassword);
  }
  tcpServer.begin();
  tcpServer.setNoDelay(true);
}

void handleTcpClients() {
  if (tcpServer.hasClient()) {
    WiFiClient nextClient = tcpServer.available();
    bool assigned = false;
    for (byte i = 0; i < MAX_TCP_CLIENTS; i++) {
      if (!tcpClients[i] || !tcpClients[i].connected()) {
        if (tcpClients[i]) {
          tcpClients[i].stop();
        }
        tcpClients[i] = nextClient;
        tcpClients[i].setNoDelay(true);
        tcpBuffers[i] = "";
        assigned = true;
        break;
      }
    }
    if (!assigned) {
      nextClient.stop();
    }
  }

  for (byte i = 0; i < MAX_TCP_CLIENTS; i++) {
    if (!tcpClients[i] || !tcpClients[i].connected()) {
      continue;
    }
    while (tcpClients[i].available()) {
      char c = (char)tcpClients[i].read();
      if (c == '#') {
        String response = processCommand(tcpBuffers[i]);
        tcpClients[i].print(response);
        tcpBuffers[i] = "";
      } else if (c != '\r' && c != '\n') {
        tcpBuffers[i] += c;
      }
    }
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

void updateManualButton(int pin, int &lastState, bool &manualFlag, unsigned long &lastDebounce, int direction) {
  int reading = digitalRead(pin);
  if (reading != lastState) {
    lastDebounce = millis();
    lastState = reading;
  }
  if ((millis() - lastDebounce) > debounceDelayMs) {
    if (reading == LOW && !manualFlag) {
      // Edge-triggered: one move per press (prevents floating-pin runaway)
      manualFlag = true;
      if (!findingHome) {
        stepper.move(direction * settings.manualMoveStepSize);
        positionSaved = false;
      }
    } else if (reading == HIGH) {
      manualFlag = false;
    }
  }
}

void handleManualButtons() {
  updateManualButton(CW_PIN, lastCWState, manualMoveCW, lastCWDebounce, 1);
  updateManualButton(CCW_PIN, lastCCWState, manualMoveCCW, lastCCWDebounce, -1);
}

void serviceHome() {
  if (!findingHome) {
    return;
  }
  if (hallTriggered()) {
    findingHome = false;
    homeFound = true;
    stepper.stop();
    stepper.setCurrentPosition(0);
    settings.homeOffsetSteps = 0;
    moveToPhysicalSteps(0);
    broadcastStatus();
    return;
  }
  if (stepper.distanceToGo() == 0) {
    long currentPhysical = driverToPhysicalSteps(stepper.currentPosition());
    moveToPhysicalSteps(currentPhysical - settings.findHomeStepSize);
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
        broadcastStatus();
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
      broadcastStatus();
    } else {
      tempSensorPresent = false;
      broadcastStatus();
    }
    return;
  }

  if (now - lastTempSampleMillis >= tempReadIntervalMs) {
    requestTemperatureConversion();
  }
}

// ==================== Setup & Loop ====================

void setup() {
#if USE_HALL_SENSOR
  pinMode(HALL_PIN, INPUT);
#endif
  pinMode(CW_PIN, INPUT_PULLUP);
  pinMode(CCW_PIN, INPUT_PULLUP);

  // Read actual pin state at boot to avoid false edge detection
  lastCWState = digitalRead(CW_PIN);
  lastCCWState = digitalRead(CCW_PIN);

  Serial.begin(9600);
  Serial.setTimeout(2000);

  loadSettings();
  stepper.setCurrentPosition(settings.position);
  applyMotionSettings();
  updateMotorOutputs();
  setupTemperatureSensor();

  setupWifi();
  setupHttp();
  webSocket.begin();
  webSocket.onEvent([](uint8_t num, WStype_t type, uint8_t *payload, size_t length) {
    if (type == WStype_CONNECTED) {
      String json = statusJson();
      webSocket.sendTXT(num, json);
    }
  });

  Serial.println();
  Serial.println(DEVICE_RESPONSE);
  Serial.print("AP SSID: ");
  Serial.println(apSsid);
  Serial.println("AP URL: http://192.168.4.1");
}

void loop() {
  server.handleClient();
  webSocket.loop();
  handleTcpClients();
  handleSerial();
  handleManualButtons();
  serviceHome();
  serviceTemperatureSensor();
  updateMotorOutputs();
  stepper.run();

  if (stepper.distanceToGo() == 0 && !positionSaved && !findingHome) {
    positionSaved = true;
    saveSettings();
    broadcastStatus();
  }

  if (millis() - lastStatusBroadcast > 500) {
    lastStatusBroadcast = millis();
    broadcastStatus();
  }
}
