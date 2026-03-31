---
weight: 3000
title: "连接到互联网"
---

# 连接到互联网

超算平台的网络是白名单制。一个可能的解决的办法是，在另一台设备上部署一个代理服务，再通过 SSH 端口转发到超算平台，让超算平台的请求经过该代理服务，从而访问网络。

> [!WARNING]
> 超算平台设置白名单肯定是有理由的。如果要大量使用，应当向平台申请开放特定网站。

## 使用服务

> [!CAUTION]
> 在继续之前，请确保已参考[《创建隔离空间》](./../create-isolation-space/)完成隔离空间的创建。不要影响其他人的配置。下述 `~` 指的是隔离空间目录。

在 `~/.bashrc` 中添加以下内容即可（当然，需要启动一个新的 `bash` 才起效果）：

```sh
# ===== proxy =====
export http_proxy="http://user:password@logini01:35263/"
export https_proxy="http://user:password@logini01:35263/"
export HTTP_PROXY="http://user:password@logini01:35263/"
export HTTPS_PROXY="http://user:password@logini01:35263/"

export no_proxy="localhost,127.0.0.1"
export NO_PROXY="localhost,127.0.0.1"
# ===== proxy =====
```

> [!TIP]
> 当然这里 `user:password` 不是真的用户名密码（如果这个端口被其他服务占据了， `logini01:35263` 也可能会变）。可以看一下 `/share/home/u13070/.bashrc` 里面是否有相关配置，复制过来即可（当然也无法保证可用）。

## 部署服务

有时候服务可能出现故障，或者出于一些原因，希望自己配置一个不同的服务。

### 第一步 找一个可以保持常开的设备

为了避免服务断开导致正在运行的程序突然断网，我们需要一台可以保持常开的设备。

> [!TIP]
> 如果只是临时需要网络，其实不需要保持常开，直接用开发时的设备也可以。

### 第二步 启动代理服务

你可以部署任何的代理服务软件。这里以 [mihomo](https://github.com/MetaCubeX/mihomo/releases) 为例，一个最简单的配置如下：

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
> 超算平台没有安装 `socat` ，此处用的是自己安装的 `/share/home/u13070/socat` 。如果丢失，可以在 [static-binaries 仓库](https://github.com/andrew-d/static-binaries/raw/master/binaries/linux/x86_64/socat)直接下载可执行文件。

> [!TIP]
> 为了保持服务长期稳定运行，可能还需要添加其他配置，如自动重连机制等。目前暂时还没有找到完全长期稳定的办法。如有，欢迎来修改此文档。

### 第四步 完成

现在应当可以在任意节点使用 `http://user:password@logini01:52629/` 访问代理。
