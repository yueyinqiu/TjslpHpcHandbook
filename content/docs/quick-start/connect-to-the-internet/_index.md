---
weight: 2000
title: "连接到互联网"
---

# 连接到互联网

> [!NOTE]
> 这一篇……感觉不该写啊……但这个真的非常重要：现在超算平台似乎是白名单，已经到影响正常使用的地步了，怎么会这么严格？

我们解决的办法是，在另一台设备上部署一个代理服务，再通过 SSH 端口转发到超算平台，让超算平台的请求经过该代理服务，从而访问网络。

## 使用服务

> [!CAUTION]
> 请确保已参考[《创建隔离空间》](./../create-isolation-space/)完成隔离空间的创建。自己的配置不应该影响其他人。下述 `~` 指的是隔离空间目录。

在 `~/.bashrc` 中添加以下内容即可（当然，需要启动一个新的 `bash` 才起效果）：

```sh
export http_proxy="http://user:password@logini01:35263/"
export https_proxy="http://user:password@logini01:35263/"
export HTTP_PROXY="http://user:password@logini01:35263/"
export HTTPS_PROXY="http://user:password@logini01:35263/"

export no_proxy="localhost,127.0.0.1"
export NO_PROXY="localhost,127.0.0.1"
```

> [!TIP]
> 当然这里 `user:password` 不是真的用户名密码（如果这个端口被其他服务占据了， `logini01:35263` 也可能会变）。可以看一下 `/share/home/u13070/.bashrc` 里面是否有相关配置，复制过来即可（当然也无法保证可用）。

## 部署服务

当然有时候服务可能挂了，或者希望自己配置一个服务，加入一些自己的货。

### 第一步 找一个可以保持常开的设备

为了避免服务断开导致正在运行的程序突然断网，我们需要一台可以保持常开的设备。

> [!TIP]
> 如果只是临时需要一下网络，其实不需要保持常开，直接用自己的设备也可以。

### 第二步 启动代理服务

这里以 [mihomo](https://github.com/MetaCubeX/mihomo/releases) 为例。先在这台常开设备上下载并安装它。一个最简单的配置如下：

```yaml
# config.yaml
mixed-port: 7890
mode: DIRECT
authentication: ["user:password"]
```

运行它：

```sh
mihomo -f config.yaml
```

按照上述配置，它会监听 `127.0.0.1:7890` 提供代理服务。

### 第三步 转发到超算平台

现在我们要选择一个节点和两个端口号：
1. 选一个固定的登录节点进行转发，例如 `logini01` （ IP 为 `192.168.212.236` ）；
2. 选一个该节点上的端口，用以把本地服务转发到节点，例如 `20132` ；
3. 再选一个该节点上的端口，用以把服务公开到局域网，例如 `52629` 。

> [!TIP]
> 节点的 IP 可能会变化。可以使用 `nslookup logini.tongji.edu.cn` 查询。

在该常开设备上保持执行：

```sh
ssh -t -R 20132:127.0.0.1:7890 u13070@192.168.212.236 -p 10022 "/share/home/u13070/socat TCP-LISTEN:52629,reuseaddr,fork TCP:127.0.0.1:20132"
```

> [!TIP]
> 超算平台没有安装 `socat` ，这里用的是自己安装的 `/share/home/u13070/socat` 。如果丢失，可以在 [static-binaries 仓库](https://github.com/andrew-d/static-binaries/raw/master/binaries/linux/x86_64/socat)直接下载可执行文件。

> [!TIP]
> 为了保持服务长期稳定运行，可能还需要添加 `ServerAliveInterval` 等配置，以及自动重连机制等。

### 第四步 完成

现在应当可以在任意节点使用 `http://user:password@logini01:52629/` 访问代理了。
