---
weight: 3000
title: "连接到互联网"
---

# 连接到互联网

超算平台的网络是白名单制。一个可能的解决的办法是，在另一台设备上部署一个代理服务，再通过 SSH 端口转发到超算平台，让超算平台的请求经过该代理服务，从而访问网络。

> [!WARNING]
> 超算平台设置白名单肯定是有理由的。如果要大量使用，应当向平台申请开放特定网站。

## 第一项 在登录节点临时连接

> [!CAUTION]
> 在继续之前，请确保已参考[《创建隔离空间》](./../create-isolation-space/)完成隔离空间的创建。不要影响其他人的配置。下述 `$HOME` 或 `~` 指的是隔离空间目录。

首先我们要使用一个最简单的方式，让登录节点先连接到互联网，然后才便于进行更复杂的配置。

### 第一步 选择特定节点

由于登录节点不止一个，每次访问时不一定分配到同一个节点。为了保证先前开启的服务能够被后续过程访问，我们先修改 SSH 配置文件，固定一个登录节点，例如：

```text {hl_lines=["8-13"]}
Host hpc-u13070
    HostName logini.tongji.edu.cn
    User u13070
    Port 10022
    ForwardAgent yes
    AddKeysToAgent yes

Host hpc-u13070-fixed-node
    HostName 192.168.212.241
    User u13070
    Port 10022
    ForwardAgent yes
    AddKeysToAgent yes
```

> [!TIP]
> 登录节点的 IP 地址可能会变化，可以使用 `nslookup logini.tongji.edu.cn` 查询。

### 第二步 创建代理

随机选择一个端口，例如 `43105` 。现在使用 `-R` 参数，在该端口上创建一个到本机的代理：

```sh
ssh -R 43105 -o ExitOnForwardFailure=yes hpc-u13070-fixed-node
```

### 第三步 尝试连接

完成后，应当可以使用 `curl -x socks5h://localhost:43105/ https://www.baidu.com` 访问网页。

> [!WARNING]
> 此方法仅供临时使用。它不带身份验证功能、只能在当前登录节点访问、第二步连接断开后就会断网。

## 第二项 使用现有通道

### 第一步 安装 sing-box

> [!TIP]
> 需要访问网络，因此需在第一项的基础上进行，要把 `SING_BOX_INSTALL_DOWNLOAD_PROXY` 配置为该代理。

在超算平台执行：

```sh
SING_BOX_INSTALL_DOWNLOAD_URL="https://gh-proxy.org/https://github.com/yueyinqiu/SingboxReleasesMirrorWithVersionStrippedFromFilenames/releases/latest/download/sing-box-linux-amd64.tar.gz"
SING_BOX_INSTALL_DOWNLOAD_PROXY="socks5h://localhost:43105"
SING_BOX_INSTALL_DOWNLOADED_FILE="/tmp/sing-box-$(uuidgen).tar.gz"
SING_BOX_INSTALL_LOCATION="$HOME/.sing-box/bin"

curl -x "$SING_BOX_INSTALL_DOWNLOAD_PROXY" -L "$SING_BOX_INSTALL_DOWNLOAD_URL" -o "$SING_BOX_INSTALL_DOWNLOADED_FILE"

mkdir -p "$SING_BOX_INSTALL_LOCATION"
tar -zxvf "$SING_BOX_INSTALL_DOWNLOADED_FILE" -C "$SING_BOX_INSTALL_LOCATION" --strip-components=1

rm "$SING_BOX_INSTALL_DOWNLOADED_FILE"
```

> [!TIP]
> 官方发布地址是 `https://github.com/SagerNet/sing-box/releases` ，上述指令使用了自己部署的镜像（去除了文件名的版本号以便直接下载最新版），并使用 `https://gh-proxy.org/` 作为 GitHub 镜像。

然后在 `~/.bashrc` 中添加：

```sh
# ===== sing-box =====
export PATH="$HOME/.sing-box/bin:$PATH"
# ===== sing-box =====
```

完成后，启动新的 `bash` 让配置生效。此时应该可以调用 `sing-box` 指令。

### 第二步 筛选现有通道

在 `/share/home/u13070/data/shared/proxies` 下会包含一些共享的通道。

找一些格式看起来正确的文件，例如 `/share/home/u13070/data/shared/proxies/sample.json` ：

```json
{
    "outbounds": [
        {
            "tag": "sample-direct-out",
            "type": "direct"
        },
        {
            "tag": "sample-block-out",
            "type": "block"
        }
    ]
}
```

> [!TIP]
> 实际使用时不要选择这个配置，它只是 `direct` 或 `block` ，不能帮你越过白名单。

把它们复制到 `~/.sing-box/outbounds` 目录下，后续使用时可直接指定这个目录：

```sh
mkdir ~/.sing-box/outbounds
cp /share/home/u13070/data/shared/proxies/sample.json ~/.sing-box/outbounds/sample.json
```

> [!TIP]
> 先进行筛选的目的是避免其中包含错误的配置文件，导致整个服务启动失败。

### 第三步 创建配置文件

创建 `~/.sing-box/configurations/bypass-white-list/config.json` ：

```json
{
    "inbounds": [
        {
            "listen": "127.0.0.1",
            "listen_port": 0
        }
    ],
    "outbounds": [
        {
            "tag": "final-out",
            "type": "urltest",
            "outbounds": [
                "sample-direct-out",
                "sample-block-out"
            ],
            "url": "https://www.baidu.com/",
            "interval": "30s",
            "tolerance": 30
        }
    ],
    "route": {
        "final": "final-out"
    }
}
```

它会自动选择延迟最低的 `outbounds` 作为最终通道。

> [!WARNING]
> 请务必设置一个足够强的用户名密码，否则其他人探测到这个端口也就可以使用你的服务。

> [!TIP]
> 实际使用时建议把百度替换为小红书 `https://www.xiaohongshu.com/` ，因为百度本身就在白名单内，不能分辨出通道的可用性。

### 第四步 手动运行代理

执行以下命令即可启动代理：

```sh
sing-box run -D ~/.sing-box/configurations/bypass-white-list -c config.json -C ~/.sing-box/outbounds 
```

由于监听端口设置为 `0` ，它会自动随机选择，输出诸如 `inbound/mixed[0]: tcp server started at 127.0.0.1:35967` 的提示。

保持它运行，在同一登录节点的另外一个终端中尝试：

```sh
curl -x socks5h://user:password@127.0.0.1:35967/ https://www.baidu.com/
```

应该可以正常访问。

### 第四步 自动配置代理

创建 `~/.sing-box/configurations/bypass-white-list/bashrc.sh` ：

```sh
function __()
{
    local log="/tmp/$(uuidgen)"
    trap 'rm -f "$log"' EXIT

    sing-box run -D ~/.sing-box/configurations/bypass-white-list -c config.json -C ~/.sing-box/outbounds > "$log" 2>&1 &
    local process=$!

    local endpoint=""
    while kill -0 $process 2>/dev/null && [ -z "$endpoint" ]; do
        sleep 0.05
        endpoint=$(tail -n +1 "$log" | awk '/server started at/ {
            split($0, a, "server started at "); 
            gsub(/[ \r\n]/, "", a[2]); 
            print a[2]; 
            exit
        }')
    done

    if [ -n "$endpoint" ]; then
        local authorization="user:password"
        export all_proxy="http://$authorization@$endpoint"
        export ALL_PROXY="http://$authorization@$endpoint"
        export http_proxy="http://$authorization@$endpoint"
        export HTTP_PROXY="http://$authorization@$endpoint"
        export https_proxy="http://$authorization@$endpoint"
        export HTTPS_PROXY="http://$authorization@$endpoint"
    else
        echo "Failed to start the bypass-white-list service, for details, please check $log" >&2
    fi
}
__
unset -f __
```

> [!TIP]
> 其中 `$HTTPS_PROXY` 等环境变量可按需配置。

在 `~/.bashrc` 中添加（由于它依赖 `sing-box` ，必须放在 `sing-box` 的配置之后）：

```sh
# ===== bypass white list =====
source ~/.sing-box/configurations/bypass-white-list/bashrc.sh
# ===== bypass white list =====
```

启动新的 `bash` 令配置生效。

### 第五步 使用自动配置的代理

尝试：

```sh
curl http://www.baidu.com/ -v
```

它会提示 `Uses proxy env variable ...` 并使用该代理访问。如果筛选的通道合适，就可以通过它们访问互联网。

## 第三项 提供新的通道

现有通道不是凭空产生的，这里鼓励大家一同参与建设。通道越多，服务也就越稳定。

### 第一步 前置准备

1. 准备一台本地设备，它可以同时连接互联网和超算平台，并能够保持运行状态；
2. 在该设备上选择一个端口，例如 `23716` ，该端口将用以部署代理服务；
3. 固定选择一个登录节点，例如 `logini03` （ IP 为 `192.168.212.241` ）；
4. 在该节点上选择一个端口，例如 `40132` ，该端口将用以转发把本地设备的代理服务；
5. 再选一个该节点上的端口，例如 `52629` ，该端口将用以公开代理服务到局域网。

### 第二步 启动代理服务

在本地设备下载 [sing-box](https://github.com/SagerNet/sing-box/releases) 并作如下配置：

```json
// config.json
{
    "inbounds": [
        {
            "type": "mixed",
            "listen": "127.0.0.1",
            "listen_port": 23716,
            "users": [
                {
                    "username": "userr",
                    "password": "passwordd"
                }
            ]
        }
    ]
}
```

使用以下命令运行它：

```sh
path/to/sing-box run -c path/to/config.json
```

按照上述配置，它会监听 `127.0.0.1:23716` 提供代理服务。

> [!TIP]
> 该程序需要保持运行，建议注册为后台服务。

### 第三步 配置局域网公开脚本

这里跳过中间转发到登录节点的步骤，先配置一个把登录节点服务转发到局域网的脚本。

在超算平台上创建 `~/.sing-box/helpers/expose-to-lan.cs` ：

```csharp
#:package CliFx@3.0.0
#:package SingBoxLib@1.2.1

using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using SingBoxLib.Configuration;
using SingBoxLib.Configuration.Inbound;
using System.Diagnostics;
using System.Net;

namespace ExposeToLan;

[Command]
public partial class ExposeToLanCommand : ICommand
{
    [CommandOption("in", 'i')]
    public required int In { get; set; }

    [CommandOption("out", 'o')]
    public required int Out { get; set; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var config = new SingBoxConfig()
        {
            Inbounds =
            [
                new DirectInbound
                {
                    Listen = "0.0.0.0",
                    ListenPort = this.Out,
                    OverrideAddress = "127.0.0.1",
                    OverridePort = this.In,
                }
            ]
        };

        var fileName = Path.GetTempFileName();
        await File.WriteAllTextAsync(fileName, config.ToJson());
        console.WriteLine($"Configuration saved at {fileName}");

        console.WriteLine($"127.0.0.1:{this.In} -> 0.0.0.0:{this.Out}");
        console.WriteLine($"To access it in the cluster, use {Dns.GetHostName()}:{this.Out}");

        await Process.Start("sing-box", ["run", "-c", fileName]).WaitForExitAsync();
    }
}
```

> [!TIP]
> 执行该脚本需要 .NET 10 或更高版本，可参考[《 .NET 》](./../../more-content/dnet/)进行安装。

### 第四步 转发代理服务 

回到本地设备，执行以下指令即可完成全部转发：

```sh
ssh -t -p 10022 u13070@192.168.212.241 -R 40132:127.0.0.1:23716 "dotnet run ~/.sing-box/helpers/expose-to-lan.cs -- -i 40132 -o 52629"
```

其中 `192.168.212.241` 是所选登录节点； `23716` 是第二步配置的本地端口；命令中两次出现的 `40132` 需保持一致； `52629` 是最终在局域网公开代理的端口。

> [!TIP]
> 该程序需要保持运行，建议注册为后台服务。

完成后，应当可以在超算平台和校园网内使用如 `socks5h://userr:passwordd@192.168.212.241:52629/` 的方式访问该代理。

### 第五步 共享代理服务

创建 `~/.sing-box/shared-outbounds/test.json` ，并按如下格式写入该代理的信息：

```json
{
    "outbounds": [
        {
            "tag": "为它取一个名字，以便唯一地指定该代理",

            "type": "socks",
            "server": "192.168.212.241",
            "server_port": 52629,
            "username": "userr",
            "password": "passwordd"
        },
        {
            "tag": "可以继续加第二个代理",

            "type": "socks",
            "server": "192.168.212.241",
            "server_port": 52629,
            "username": "userr",
            "password": "passwordd"
        }
    ]
}
```

然后运行以下命令把它链接到 `/share/home/u13070/data/shared/proxies` ，表明这个代理大家可以自由取用：

```sh
ln -s ~/.sing-box/shared-outbounds/test.json /share/home/u13070/data/shared/proxies/取个名字.json
```

建议把文件设置为只读，以免软链接被意外修改：

```sh
chmod a-w ~/.sing-box/shared-outbounds/test.json
```

> [!TIP]
> 使用软连接主要是为便于确认代理是由谁提供的。如果出现问题能方便联系，并且可以适时清理。
