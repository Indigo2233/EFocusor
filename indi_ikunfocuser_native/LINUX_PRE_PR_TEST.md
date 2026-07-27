# IKun Focuser 提交 PR 前的 Linux 真机验证

这份流程用于在普通 Ubuntu/Debian Linux 电脑上验证驱动。测试时直接运行
构建目录中的程序，不会覆盖系统已经安装的 INDI。

## 1. 准备环境

```bash
sudo apt update
sudo apt install -y \
  build-essential cmake ninja-build git \
  libcfitsio-dev libnova-dev libusb-1.0-0-dev \
  libjpeg-dev libcurl4-gnutls-dev zlib1g-dev \
  libgsl-dev libev-dev libfftw3-dev netcat-openbsd
```

## 2. 下载并构建测试分支

```bash
git clone --depth 1 --branch add-ikun-focuser \
  https://github.com/Indigo2233/indi.git
cd indi

cmake -S . -B build -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build build --target \
  indiserver indi_getprop indi_setprop indi_ikun_focuser \
  -j"$(nproc)"
```

确认程序已经生成：

```bash
ls -l build/bin/indiserver build/bin/indi_ikun_focuser
```

## 3. 检查 ESP8266 网络和协议

让 Linux 电脑连接电调的 `Focuser-<chipid>` Wi-Fi，然后执行：

```bash
ping -c 3 192.168.4.1
printf '#'  | nc -w 3 192.168.4.1 4030
printf 'V#' | nc -w 3 192.168.4.1 4030
printf 'G#' | nc -w 3 192.168.4.1 4030
```

预期结果：

- `#` 返回包含 `EFucoser` 的设备身份。
- `V#` 返回固件版本。
- `G#` 返回当前位置和移动状态。

## 4. 临时启动 INDI 驱动

```bash
./build/bin/indiserver -vvv \
  ./build/bin/indi_ikun_focuser \
  2>&1 | tee ikunfocuser-indiserver.log
```

INDI 服务默认监听 `7624` 端口。保持此终端运行，然后在 KStars/Ekos 中建立
远程设备配置：

- INDI 主机：运行上述命令的 Linux 电脑 IP
- INDI 端口：`7624`
- 设备名称：`IKun Focuser`
- 驱动连接方式：TCP
- 电调地址：`192.168.4.1`
- 电调端口：`4030`

## 5. 真机检查

先使用较小步数，并确保调焦机构有安全余量。

- 连接和断开均正常。
- 固件版本、当前位置和最大行程可以读取。
- 小步数绝对移动正常。
- 向内和向外相对移动正常。
- 移动过程中停止正常。
- 位置同步正常。
- 方向反转正常。
- 最大行程、速度、加速度和电机保持设置正常。
- ESP8266 断电重启后可以重新连接。
- 连续运行 10 分钟没有断线、崩溃或异常移动。

温度传感器或自动对焦功能未安装时，可以在测试记录中注明未测试。

## 6. 保存提交 PR 所需信息

```bash
uname -a
cat /etc/os-release
git rev-parse HEAD
```

请保留：

- `ikunfocuser-indiserver.log`
- Linux 发行版和 CPU 架构
- ESP8266 型号及固件版本
- 已通过和未测试的检查项
- KStars/Ekos 中成功连接的截图

完成后将以上结果发回来，即可整理 INDI PR 的测试说明。普通 x86_64 Linux
真机测试可以作为 PR 的初始验证；图谱天文盒子的 ARM64 验证可在后续补充。
