#!/usr/bin/env python3
"""EFucoser 电调焦控制"""
import socket, time, re

H, P, D = "localhost", 7624, "EFucoser Focuser"
s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
s.connect((H, P))
s.settimeout(1)

def cmd(xml):
    s.sendall(xml.encode() + b"\n")
    time.sleep(0.15)

def ask(xml):
    s.sendall(xml.encode() + b"\n")
    time.sleep(0.2)
    parts = []
    deadline = time.time() + 2
    while time.time() < deadline:
        try:
            s.settimeout(0.3)
            c = s.recv(4096)
            if not c:
                break
            parts.append(c)
        except:
            break
    return b"".join(parts).decode(errors="replace")

def pos():
    # Get live position from setNumberVector updates (poll thread sends these)
    r = ask(f'<getProperties version="1.7" device="{D}"/>')
    matches = re.findall(
        r'<setNumberVector[^>]*name="ABS_FOCUS_POSITION"[^>]*>.*?'
        r'<oneNumber[^>]*>([\d-]+)</oneNumber>',
        r, re.DOTALL
    )
    if matches:
        return int(matches[-1])
    # Fallback: defNumberVector
    m = re.search(
        r'<defNumberVector[^>]*name="ABS_FOCUS_POSITION".*?'
        r'<defNumber[^>]*>([\d-]+)',
        r, re.DOTALL
    )
    return int(m.group(1)) if m else 0


# Check if already connected
r = ask(f'<getProperties version="1.7" device="{D}" name="CONNECTION"/>')
if ">On<" in r or "CONNECT" in r and "On" in r.split("CONNECT")[-1][:10]:
    print("已连接，无需重连")
else:
    cmd(f'<newTextVector device="{D}" name="DEVICE_PORT"><oneText name="PORT">/dev/ttyUSB0</oneText></newTextVector>')
    time.sleep(0.5)
    cmd(f'<newSwitchVector device="{D}" name="CONNECTION"><oneSwitch name="CONNECT">On</oneSwitch></newSwitchVector>')
    time.sleep(4)

p = pos()
print(f"位置: {p}")
print("p=位置 gN=移到N +N=向内N步 -N=向外N步 sN=速度 h=停 q=退")

while True:
    try:
        c = input("> ").strip()
    except (EOFError, KeyboardInterrupt):
        break
    if not c:
        continue
    if c in ("q", "quit"):
        break
    if c == "p":
        print(pos())
    elif c == "h":
        cmd(f'<newSwitchVector device="{D}" name="FOCUS_ABORT_MOTION"><oneSwitch name="ABORT">On</oneSwitch></newSwitchVector>')
        print("停止")
    elif c[0] == "g" and " " in c:
        v = int(c.split()[1])
        cmd(f'<newNumberVector device="{D}" name="ABS_FOCUS_POSITION"><oneNumber name="FOCUS_ABSOLUTE_POSITION">{v}</oneNumber></newNumberVector>')
        time.sleep(0.3)
        print(f"-> {v}  位置:{pos()}")
    elif c[0] == "+" and " " in c:
        v = int(c[1:])
        cmd(f'<newNumberVector device="{D}" name="REL_FOCUS_POSITION"><oneNumber name="FOCUS_RELATIVE_POSITION">{v}</oneNumber></newNumberVector>')
        time.sleep(0.3)
        print(f"+{v}  位置:{pos()}")
    elif c[0] == "-" and " " in c:
        v = int(c[1:])
        cmd(f'<newNumberVector device="{D}" name="REL_FOCUS_POSITION"><oneNumber name="FOCUS_RELATIVE_POSITION">{v}</oneNumber></newNumberVector>')
        time.sleep(0.3)
        print(f"-{v}  位置:{pos()}")
    elif c.startswith("s "):
        v = int(c.split()[1])
        cmd(f'<newNumberVector device="{D}" name="FOCUS_SPEED"><oneNumber name="FOCUS_SPEED_VALUE">{v}</oneNumber></newNumberVector>')
        print(f"速度:{v}")
    else:
        print("? p g5000 +100 -100 s600 h q")
s.close()
