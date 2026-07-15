#!/usr/bin/env python3
"""EFucoser Focuser CLI Controller — 命令行控制电调焦"""

import socket
import sys
import time
import re

HOST = "localhost"
PORT = 7624
DEVICE = "EFucoser Focuser"


def send(xml: str):
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    s.connect((HOST, PORT))
    s.sendall((xml + "\n").encode())
    time.sleep(0.3)
    s.settimeout(2)
    data = b""
    try:
        while True:
            chunk = s.recv(4096)
            if not chunk:
                break
            data += chunk
    except socket.timeout:
        pass
    s.close()
    return data.decode()


def get_position():
    resp = send(
        f'<getProperties version="1.7" device="{DEVICE}" name="ABS_FOCUS_POSITION"/>'
    )
    m = re.search(
        r"<(?P<kind>def|one)Number[^>]*>(?P<value>\d+)</(?P=kind)Number>",
        resp,
    )
    return int(m.group("value")) if m else None


def move_absolute(target: int):
    send(
        f'<newNumberVector device="{DEVICE}" name="ABS_FOCUS_POSITION">'
        f'<oneNumber name="FOCUS_ABSOLUTE_POSITION">{target}</oneNumber>'
        f"</newNumberVector>"
    )


def move_relative(delta: int):
    send(
        f'<newNumberVector device="{DEVICE}" name="REL_FOCUS_POSITION">'
        f'<oneNumber name="FOCUS_RELATIVE_POSITION">{abs(delta)}</oneNumber>'
        f"</newNumberVector>"
    )


def halt():
    send(
        f'<newSwitchVector device="{DEVICE}" name="FOCUS_ABORT_MOTION">'
        f'<oneSwitch name="ABORT">On</oneSwitch>'
        f"</newSwitchVector>"
    )


def set_speed(speed: int):
    send(
        f'<newNumberVector device="{DEVICE}" name="FOCUS_SPEED">'
        f'<oneNumber name="FOCUS_SPEED_VALUE">{speed}</oneNumber>'
        f"</newNumberVector>"
    )


def get_status():
    resp = send(f'<getProperties version="1.7" device="{DEVICE}"/>')
    props = {}
    for name in [
        "ABS_FOCUS_POSITION",
        "FOCUS_SPEED",
        "FOCUS_MAX",
        "FOCUS_TEMPERATURE",
    ]:
        m = re.search(
            rf'<defNumberVector[^>]*name="{name}".*?<defNumber[^>]*>([\d.]+)',
            resp,
            re.DOTALL,
        )
        if m:
            props[name] = m.group(1)
    return props


def is_connected():
    resp = send(
        f'<getProperties version="1.7" device="{DEVICE}" name="CONNECTION"/>'
    )
    return ">On<" in resp or "CONNECT" in resp and "On" in resp


def connect(port="/dev/ttyUSB0"):
    send(
        f'<newTextVector device="{DEVICE}" name="DEVICE_PORT">'
        f'<oneText name="PORT">{port}</oneText>'
        f"</newTextVector>"
    )
    time.sleep(0.3)
    send(
        f'<newSwitchVector device="{DEVICE}" name="CONNECTION">'
        f'<oneSwitch name="CONNECT">On</oneSwitch>'
        f"</newSwitchVector>"
    )
    time.sleep(3)
    return is_connected()


def wait_move():
    """Wait until the focuser stops moving."""
    for _ in range(300):  # 30 seconds max
        pos = get_position()
        time.sleep(0.1)
        pos2 = get_position()
        if pos == pos2 and pos is not None:
            return pos
    return get_position()


def main():
    print("EFucoser 电调焦 CLI 控制")
    print("========================")

    # Check connection
    if not is_connected():
        print("正在连接 Arduino Nano...")
        if connect():
            print("✅ 已连接")
        else:
            print("❌ 连接失败，请检查 /dev/ttyUSB0")
            sys.exit(1)
    else:
        print("✅ 已连接")

    while True:
        status = get_status()
        pos = status.get("ABS_FOCUS_POSITION", "?")
        speed = status.get("FOCUS_SPEED", "?")
        max_step = status.get("FOCUS_MAX", "?")

        print(f"\n位置: {pos} / {max_step}  速度: {speed} steps/s")
        print("命令: g N   移动到位置N")
        print("      + N   向内移动N步")
        print("      - N   向外移动N步")
        print("      s N   设置速度")
        print("      h     停止")
        print("      q     退出")
        cmd = input("> ").strip().split()

        if not cmd:
            continue
        action = cmd[0].lower()

        if action == "q":
            break
        elif action == "h":
            halt()
            print("⏹ 已停止")
        elif action == "s" and len(cmd) > 1:
            set_speed(int(cmd[1]))
            print(f"⚡ 速度: {cmd[1]}")
        elif action == "g" and len(cmd) > 1:
            target = int(cmd[1])
            move_absolute(target)
            print(f"➡ 移动到 {target} ...")
            final = wait_move()
            print(f"📍 到达 {final}")
        elif action == "+" and len(cmd) > 1:
            delta = int(cmd[1])
            move_relative(delta)
            print(f"➡ 向内 {delta} 步 ...")
            final = wait_move()
            print(f"📍 到达 {final}")
        elif action == "-" and len(cmd) > 1:
            delta = int(cmd[1])
            move_relative(-delta)
            print(f"➡ 向外 {delta} 步 ...")
            final = wait_move()
            print(f"📍 到达 {final}")
        else:
            print("未知命令")


if __name__ == "__main__":
    main()
