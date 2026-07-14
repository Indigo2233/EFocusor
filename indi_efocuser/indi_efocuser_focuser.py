#!/usr/bin/env python3
"""
EFucoser INDI Focuser Driver
=============================
Pure Python INDI driver for EFucoser Arduino Nano ULN2003 focuser.
Communicates via INDI XML protocol over stdin/stdout.

Usage:  indiserver -v indi_efocuser_focuser
"""

import sys, os, time, json, threading, logging
import xml.etree.ElementTree as ET
import serial, serial.tools.list_ports

# ── Constants ────────────────────────────────────────────────────────────

DEVICE = "EFucoser Focuser"
VERSION = "1.0.0"
BAUD = 9600
TIMEOUT = 2.0
DEF_MAX = 816000
DEF_SPEED = 800
DEF_ACCEL = 1000
POLL_S = 0.5

# ── Helpers ──────────────────────────────────────────────────────────────

def esc(s):
    return str(s).replace("&","&amp;").replace("<","&lt;").replace(">","&gt;").replace('"',"&quot;")

def now_ts():
    return time.strftime("%Y-%m-%dT%H:%M:%S")

def _swn(name, label, value="Off"):
    return f'  <oneSwitch name="{esc(name)}" label="{esc(label)}">{value}</oneSwitch>'

def _num(name, label, fmt, mn, mx, step, value):
    return (f'  <oneNumber name="{esc(name)}" label="{esc(label)}" '
            f'format="{esc(fmt)}" min="{mn}" max="{mx}" step="{step}">{value}</oneNumber>')

def _txt(name, label, value):
    return f'  <oneText name="{esc(name)}" label="{esc(label)}">{esc(value)}</oneText>'

# ── Serial ───────────────────────────────────────────────────────────────

class SerialIO:
    def __init__(self, log):
        self._port = None
        self._s = None
        self._lk = threading.Lock()
        self.log = log

    @property
    def ok(self):
        return self._s and self._s.is_open

    def connect(self, port):
        try:
            self._s = serial.Serial(port, BAUD, timeout=TIMEOUT)
            self._port = port
            time.sleep(2.2)
            self._s.reset_input_buffer()
            ident = self.cmd("#")
            self.log.info("Identity: %s", ident)
            return bool(ident and "EFucoser" in ident)
        except Exception as e:
            self.log.error("Serial connect: %s", e)
            self._s = None
            return False

    def disconnect(self):
        if self._s and self._s.is_open:
            try: self._s.close()
            except: pass
        self._s = None

    def cmd(self, c):
        with self._lk:
            if not self.ok: return None
            try:
                if not c.endswith("#"): c += "#"
                self._s.reset_input_buffer()
                self._s.write(c.encode())
                self._s.flush()
                r = self._s.read_until(b"#")
                return r.decode(errors="replace").rstrip("#\r\n") if r else None
            except: return None

    def status(self):
        r = self.cmd("G#")
        if not r: return (0, False)
        try:
            a = r.split(";")
            return (int(a[0].replace("P","").strip()), len(a)>1 and "true" in a[1])
        except: return (0, False)

    def full(self):
        r = self.cmd("I#")
        if r:
            try:
                s = r.find("{"); e = r.rfind("}")
                if s>=0 and e>s: return json.loads(r[s:e+1])
            except: pass
        return {}

    def move(self, p):
        r = self.cmd(f"M {p}#")
        return r and not r.startswith("ERR")
    def stop(self): self.cmd("S#")
    def set_rev(self, v): self.cmd(f"R {1 if v else 0}#")
    def set_hold(self, v): self.cmd(f"C {1 if v else 0}#")
    def set_max(self, v):
        r = self.cmd(f"D {v}#"); return r and not r.startswith("ERR")
    def set_speed(self, v):
        r = self.cmd(f"X {v}#"); return r and not r.startswith("ERR")
    def set_accel(self, v):
        r = self.cmd(f"A {v}#"); return r and not r.startswith("ERR")
    @staticmethod
    def ports():
        return [p.device for p in serial.tools.list_ports.comports()]

# ── Driver ───────────────────────────────────────────────────────────────

class EFocuserINDI:
    def __init__(self):
        self.log = logging.getLogger("EFocuser")
        self.ser = SerialIO(self.log)
        self.runflag = True

        self.conn = False
        self.pos = 0
        self.moving = False
        self.maxst = DEF_MAX
        self.speed = DEF_SPEED
        self.acc = DEF_ACCEL
        self.rev = False
        self.hold = False
        self.temp = 20.0
        self.inward = False
        self.port = "/dev/ttyUSB0"

        self._pt = threading.Thread(target=self._poll, daemon=True)

    # ── Output ────────────────────────────────────────────────────────

    def _out(self, tag, attrs, body="", end="/>"):
        a = " ".join(f'{k}="{esc(v)}"' for k,v in attrs.items())
        if body:
            self._write(f"<{tag} {a}>\n{body}\n</{tag}>")
        else:
            self._write(f"<{tag} {a}{end}")

    def _write(self, s):
        sys.stdout.write(s + "\n")
        sys.stdout.flush()

    def _msg(self, m):
        self.log.info(m)
        ts = now_ts()
        self._write(f'<message device="{esc(DEVICE)}" name="" timestamp="{ts}" message="{esc(m)}"/>')

    def _delete(self, name):
        ts = now_ts()
        self._write(f'<delProperty device="{esc(DEVICE)}" name="{esc(name)}" timestamp="{ts}"/>')

    # ── Define Properties ─────────────────────────────────────────────

    def define_all(self):
        ts = now_ts()

        # DRIVER_INFO — required for indiserver to recognize device interface
        # FOCUSER_INTERFACE = 1 << 1 = 2
        self._out("defTextVector", {
            "device": DEVICE, "name": "DRIVER_INFO", "label": "Driver Info",
            "group": "Info", "perm": "ro", "state": "Idle",
            "timeout": "60", "timestamp": ts,
        }, body=(
            _txt("DRIVER_NAME", "Name", DEVICE) + "\n" +
            _txt("DRIVER_EXEC", "Exec", "indi_efocuser_focuser") + "\n" +
            _txt("DRIVER_VERSION", "Version", VERSION) + "\n" +
            _txt("DRIVER_INTERFACE", "Interface", "2")
        ))

        # Port text
        ports = SerialIO.ports()
        p = ports[0] if ports else "/dev/ttyUSB0"
        self.port = p
        self._out("defTextVector", {
            "device": DEVICE, "name": "DEVICE_PORT", "label": "Serial Port",
            "group": "Connection", "perm": "rw", "state": "Idle",
            "timeout": "60", "timestamp": ts,
        }, body=_txt("TEXT", "Port", p))

        # Connection switch
        self._out("defSwitchVector", {
            "device": DEVICE, "name": "CONNECTION", "label": "Connection",
            "group": "Connection", "perm": "rw", "rule": "OneOfMany",
            "state": "Idle", "timeout": "60", "timestamp": ts,
        }, body=_swn("CONNECT","Connect","Off")+"\n"+_swn("DISCONNECT","Disconnect","On"))

    def define_focus(self):
        ts = now_ts()
        mx = self.maxst

        def _defN(name, label, group, fmt, mn, mxv, step, value, perm="rw"):
            self._out("defNumberVector", {
                "device":DEVICE,"name":name,"label":label,"group":group,
                "perm":perm,"state":"Idle","timeout":"60","timestamp":ts,
            }, body=_num("VALUE",label,fmt,mn,mxv,step,value))

        def _defS(name, label, group, switches, rule="OneOfMany"):
            self._out("defSwitchVector", {
                "device":DEVICE,"name":name,"label":label,"group":group,
                "perm":"rw","rule":rule,"state":"Idle","timeout":"60","timestamp":ts,
            }, body="\n".join(_swn(n,l,v) for n,l,v in switches))

        # First pass: define all properties
        _defN("FOCUS_SPEED","Speed (steps/sec)","Main","%4.0f",1,2000,10,self.speed)
        _defN("ACCELERATION","Accel (steps/sec²)","Main","%5.0f",10,10000,10,self.acc)
        _defN("FOCUS_MAX","Max Position","Main","%7.0f",100,9999999,1000,mx)
        _defN("FOCUS_POSITION","Position","Main","%7.0f",0,mx,1,self.pos,perm="ro")
        _defN("FOCUS_ABSOLUTE_POSITION","Goto","Main","%7.0f",0,mx,1,0)
        _defN("FOCUS_RELATIVE_POSITION","Relative Move","Main","%7.0f",-mx,mx,1,0)
        _defS("FOCUS_MOTION","Direction","Main",[
            ("FOCUS_INWARD","Inward","Off"),("FOCUS_OUTWARD","Outward","On")])
        _defN("FOCUS_TIMER","Timer (ms)","Main","%5.0f",10,60000,10,100)
        _defS("REVERSE_MOTION","Reverse","Options",[
            ("REVERSE_ENABLED","Enabled","On" if self.rev else "Off"),
            ("REVERSE_DISABLED","Disabled","Off" if self.rev else "On")])
        _defS("HOLD_MODE","Hold Mode","Options",[
            ("HOLD_ON","On","On" if self.hold else "Off"),
            ("HOLD_OFF","Off","Off" if self.hold else "On")])
        _defN("FOCUS_TEMPERATURE","Temperature (°C)","Main","%4.1f",-55,125,0.1,self.temp,perm="ro")
        _defS("ABORT_MOTION","Abort","Main",[("ABORT","Abort","Off")])

        # Flush to ensure indiserver processes definitions
        sys.stdout.flush()
        time.sleep(0.5)

        # Second pass: set values (pushes to existing clients)
        self._setN("FOCUS_SPEED", self.speed)
        self._setN("ACCELERATION", self.acc)
        self._setN("FOCUS_MAX", mx)
        self._setN("FOCUS_POSITION", self.pos)
        self._setN("FOCUS_ABSOLUTE_POSITION", 0)
        self._setN("FOCUS_RELATIVE_POSITION", 0)
        self._setS("FOCUS_MOTION", [("FOCUS_INWARD","Off"),("FOCUS_OUTWARD","On")])
        self._setN("FOCUS_TIMER", 100)
        self._setS("REVERSE_MOTION", [
            ("REVERSE_ENABLED","On" if self.rev else "Off"),
            ("REVERSE_DISABLED","Off" if self.rev else "On")])
        self._setS("HOLD_MODE", [
            ("HOLD_ON","On" if self.hold else "Off"),
            ("HOLD_OFF","Off" if self.hold else "On")])
        self._setN("FOCUS_TEMPERATURE", self.temp)
        self._setS("ABORT_MOTION", [("ABORT","Off")])

    def remove_focus(self):
        for n in ["FOCUS_SPEED","ACCELERATION","FOCUS_MAX","FOCUS_POSITION",
                  "FOCUS_ABSOLUTE_POSITION","FOCUS_RELATIVE_POSITION",
                  "FOCUS_MOTION","FOCUS_TIMER","REVERSE_MOTION",
                  "HOLD_MODE","FOCUS_TEMPERATURE","ABORT_MOTION"]:
            self._delete(n)

    # ── Set / Update ──────────────────────────────────────────────────

    def _setS(self, name, pairs, state="Ok"):
        ts = now_ts()
        body = "\n".join(f'  <oneSwitch name="{esc(n)}">{esc(v)}</oneSwitch>' for n,v in pairs)
        self._out("setSwitchVector", {
            "device":DEVICE,"name":name,"state":state,"timestamp":ts,
        }, body=body)

    def _setN(self, name, value, state="Ok"):
        ts = now_ts()
        self._out("setNumberVector", {
            "device":DEVICE,"name":name,"state":state,"timestamp":ts,
        }, body=f'  <oneNumber name="VALUE">{value}</oneNumber>')

    # ── Connection ────────────────────────────────────────────────────

    def do_connect(self):
        if self.ser.connect(self.port):
            self.conn = True
            s = self.ser.full()
            if s:
                self.maxst = s.get("maxSteps", DEF_MAX)
                self.speed = s.get("maxSpeed", DEF_SPEED)
                self.acc = s.get("acceleration", DEF_ACCEL)
                self.rev = s.get("reversed", False)
                self.hold = s.get("hold", False)
                self.temp = s.get("lastTemp", 20.0)
            self.define_focus()
            self._setS("CONNECTION", [("CONNECT","On"),("DISCONNECT","Off")])
            self._msg(f"Connected to {self.port}")
            if not self._pt.is_alive():
                self._pt = threading.Thread(target=self._poll, daemon=True)
                self._pt.start()
        else:
            self._setS("CONNECTION", [("CONNECT","Off"),("DISCONNECT","On")], "Alert")
            self._msg(f"Failed: {self.port}")

    def do_disconnect(self):
        self.conn = False
        self.ser.disconnect()
        self.remove_focus()
        self._setS("CONNECTION", [("CONNECT","Off"),("DISCONNECT","On")])
        self._msg("Disconnected")

    # ── Polling ───────────────────────────────────────────────────────

    def _poll(self):
        c = 0
        while self.runflag:
            time.sleep(POLL_S)
            if not self.conn: continue
            try:
                p, m = self.ser.status()
                self.pos = p; self.moving = m
                self._setN("FOCUS_POSITION", p)
                c += 1
                if c >= 4:
                    c = 0
                    s = self.ser.full()
                    if s:
                        t = s.get("lastTemp", self.temp)
                        if abs(t - self.temp) > 0.05:
                            self.temp = t; self._setN("FOCUS_TEMPERATURE", t)
            except Exception as e:
                self.log.error("Poll: %s", e)

    # ── XML Parser ────────────────────────────────────────────────────

    def _on_sw(self, root, name):
        for sw in root.findall("oneSwitch"):
            if sw.get("name") == name and sw.text and sw.text.strip() == "On":
                return True
        return False

    def parse(self, data):
        data = data.strip()
        if not data: return
        try:
            root = ET.fromstring(data)
        except ET.ParseError:
            return

        tag = root.tag
        dev = root.get("device","")
        if dev and dev != DEVICE: return
        nm = root.get("name","")

        if tag == "getProperties":
            # Don't respond — let indiserver use its cached properties
            return

        if tag == "newSwitchVector":
            if nm == "CONNECTION":
                if self._on_sw(root, "CONNECT"): self.do_connect()
                elif self._on_sw(root, "DISCONNECT"): self.do_disconnect()
            elif nm == "ABORT_MOTION":
                if self._on_sw(root, "ABORT"):
                    if self.conn: self.ser.stop()
                    self._setS("ABORT_MOTION", [("ABORT","Off")])
            elif nm == "REVERSE_MOTION":
                if self._on_sw(root, "REVERSE_ENABLED"):
                    self.rev = True
                    if self.conn: self.ser.set_rev(True)
                    self._setS("REVERSE_MOTION", [("REVERSE_ENABLED","On"),("REVERSE_DISABLED","Off")])
                elif self._on_sw(root, "REVERSE_DISABLED"):
                    self.rev = False
                    if self.conn: self.ser.set_rev(False)
                    self._setS("REVERSE_MOTION", [("REVERSE_ENABLED","Off"),("REVERSE_DISABLED","On")])
            elif nm == "HOLD_MODE":
                if self._on_sw(root, "HOLD_ON"):
                    self.hold = True
                    if self.conn: self.ser.set_hold(True)
                    self._setS("HOLD_MODE", [("HOLD_ON","On"),("HOLD_OFF","Off")])
                elif self._on_sw(root, "HOLD_OFF"):
                    self.hold = False
                    if self.conn: self.ser.set_hold(False)
                    self._setS("HOLD_MODE", [("HOLD_ON","Off"),("HOLD_OFF","On")])
            elif nm == "FOCUS_MOTION":
                self.inward = self._on_sw(root, "FOCUS_INWARD")

        elif tag == "newNumberVector":
            e = root.find("oneNumber")
            if e is None or e.text is None: return
            v = e.text.strip()

            if nm == "FOCUS_SPEED":
                self.speed = int(v)
                if self.conn: self.ser.set_speed(self.speed)
                self._setN("FOCUS_SPEED", self.speed)
            elif nm == "ACCELERATION":
                self.acc = int(v)
                if self.conn: self.ser.set_accel(self.acc)
                self._setN("ACCELERATION", self.acc)
            elif nm == "FOCUS_MAX":
                val = int(v)
                if val <= 0: return
                self.maxst = val
                if self.conn: self.ser.set_max(val)
                self._setN("FOCUS_MAX", self.maxst)
            elif nm == "FOCUS_ABSOLUTE_POSITION":
                t = max(0, min(int(v), self.maxst))
                if self.conn:
                    self.ser.move(t)
                    self._msg(f"Goto {t}")
                self._setN("FOCUS_ABSOLUTE_POSITION", t)
            elif nm == "FOCUS_RELATIVE_POSITION":
                d = int(v)
                if self.conn:
                    nt = max(0, min(self.pos + d, self.maxst))
                    self.ser.move(nt)
                    self._msg(f"Relative {d}")
                self._setN("FOCUS_RELATIVE_POSITION", 0)
            elif nm == "FOCUS_TIMER":
                dur = int(v)
                if self.conn and dur > 0:
                    delta = int(dur * self.speed / 1000.0)
                    if self.inward: delta = -delta
                    nt = max(0, min(self.pos + delta, self.maxst))
                    self.ser.move(nt)
                    self._msg(f"Timer {dur}ms, ~{abs(delta)} steps")
                self._setN("FOCUS_TIMER", dur)

        elif tag == "newTextVector":
            if nm == "DEVICE_PORT":
                e = root.find("oneText")
                if e is not None and e.text:
                    self.port = e.text.strip()
                    self.log.info("Port: %s", self.port)

    # ── Main Loop ─────────────────────────────────────────────────────

    def run(self):
        self.log.info("EFucoser INDI Focuser v%s starting", VERSION)
        self.define_all()
        self._msg("Driver ready.")

        buf = ""
        while self.runflag:
            try:
                line = sys.stdin.readline()
                if not line: break
                buf += line
                stripped = buf.strip()
                # Only parse when we have a complete XML element:
                # self-closing: <tag .../> or <tag ... />
                # with body: <tag ...>...</tag>
                if stripped.endswith("/>") or stripped.endswith("</" + self._top_tag(buf) + ">"):
                    self.parse(buf)
                    buf = ""
            except KeyboardInterrupt:
                break
            except Exception as e:
                self.log.error("Input: %s", e)
                buf = ""

        self.runflag = False
        if self.conn: self.ser.disconnect()
        self.log.info("Stopped.")

    @staticmethod
    def _top_tag(xml: str) -> str:
        """Extract the top-level tag name from an XML fragment."""
        import re
        m = re.match(r'^\s*<(\w+)', xml)
        return m.group(1) if m else ""

def main():
    logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(name)s] %(message)s", stream=sys.stderr)
    EFocuserINDI().run()

if __name__ == "__main__":
    main()
