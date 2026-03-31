---
weight: 1000
title: "创建隔离空间"
---

# 创建隔离空间

首先要解决的问题是配置的混乱。一般来说，一个用户只会被一个人使用。然而，学校只让老师开用户，这导致很多配置都会混在一起。例如 VS Code 的一些个性化设置，又如 git 的用户名和邮箱（虽然可以为仓库单独配置，但可能有其他人错误设置了全局的，导致自己在不知情的情况下误用）。

这里，我们通过改变环境变量，实现一个简单的隔离空间。

> [!CAUTION]
> 隔离空间只能帮助创建一个相对隔离的空间。主要目的是：
> 1. 防止自己在不知情的情况下，用了其他人的配置；
> 2. 可以自己大胆进行一些配置，而不用担心影响其他人；
> 3. 在准备停止使用超算平台时，可以移除自己引入的内容，而不用担心其他影响。
> 
> 由于实际还是同一个用户，无法防止文件被访问和篡改。
>
> 此外，由于此隔离空间的原理仅仅是修改环境变量，部分程序可能无法完全隔离，或者需要额外配置。详见[《已知不兼容的程序》](#已知不兼容的程序)。

> [!TIP]
> 曾经尝试过 `unshare` 实现更完整的隔离，但这会让我们进入 `root` 身份，导致无法正常使用 `slurm` 。如果能解决这个问题，欢迎来修改此文档。

## 第一步 生成 SSH 密钥

> [!TIP]
> 如果已经拥有 SSH 密钥，可以跳过此步。

为了实现隔离空间，必须使用密钥登录，否则不可能区分登录者的身份。

在本地设备上，使用下述命令即可生成密钥：

```sh
ssh-keygen
```

> [!TIP]
> 如果不明白自己在做什么，一直按回车就可以了（如果它提示文件已存在，说明以前曾生成过密钥，那可以直接跳过此步）。

在生成过程中，它会提示密钥文件的存放位置。其中一个文件以 `.pub` 为后缀名，它的内容应该类似这样：

```text
ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIBwLLOJbq3byqJ8/KREL+93wzIUjpXQ75SUTTXRdE4BH yueyi@DESKTOP-VE9QGI5
```

后面需要用到这段内容。请记住这个文件在哪里。

> [!CAUTION]
> 另外一个没有后缀的文件是私钥。请保管好它，不要泄露。如不慎泄露，应当立即停用，并将超算平台中 `authorized_keys` 中相应条目删除。

## 第二步 连接超算平台

在校园网内，使用下述指令即可连接超算平台：

```sh
ssh u13070@logini.tongji.edu.cn -p 10022
```

为了避免每次都使用过长的命令，可在 SSH 配置文件（在 Windows 上默认位于 `%USERPROFILE%/.ssh/config` ，在 Linux 上默认位于 `~/.ssh/config` ）中添加：

```text
Host hpc-u13070
    HostName logini.tongji.edu.cn
    User u13070
    Port 10022
```

完成后使用 `ssh hpc-u13070` 即可连接。

## 第三步 创建隔离环境

在超算平台上执行：

```sh
~/register_space.cs
```

> [!CAUTION]
> 该脚本内部访问 `HOME` 环境变量构建路径，因此请确保目前没有位于隔离环境中（ `echo ~` 的输出是 `/share/home/u13070` ）。可以使用 `ssh hpc-u13070 -o PubkeyAuthentication=no` 禁用密钥登录，以避免进入隔离环境。

它会依次问以下几个问题：
1. 您想使用的隔离空间名称：相当于给自己取一个用户名（只能是字母、数字、下划线、减号，首个字符只能是字母，不少于四个字符）；
2. 请输入一项联系方式，以便遇到问题及时联系：尽量给一个能及时联系上的联系方式，没有输入格式限制，这会被储存在 `authorized_key` 中；
3. 请输入您的 SSH 公钥：在[《第一步 生成 SSH 密钥》](#第一步-生成-ssh-密钥)中生成的 `.pub` 文件内容。

正常情况下，完整交互过程类似：

```text
[u13070@logini02 ~]$ ~/register_space.cs
此脚本可辅助完成隔离空间的创建。如果您不清楚这是什么，可参考 https://tjslp-hpc.yueyinqiu.top/ 的使用说明。
您想使用的隔离空间名称：yueyinqiu
好的，隔离空间将会被创建在 /share/home/u13070/data/yueyinqiu
对应的全闪目录为 /ssdfs/datahome/u13070/yueyinqiu

请输入一项联系方式，以便遇到问题及时联系：yueyinqiu@outlook.com
好的，联系方式将记录为 yueyinqiu@outlook.com

请输入您的 SSH 公钥ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIBwLLOJbq3byqJ8/KREL+93wzIUjpXQ75SUTTXRdE4BH yueyi@DESKTOP-VE9QGI5
好的，该公钥的指纹为 SHA256:dS40JD4zcty/XQWVo1Z/TRAU52QW2ok738JxbcBdZWI

正在创建隔离空间目录结构……
正在写入公钥……
已完成。
您现在可尝试使用该公钥进行登录，以进入隔离空间。
```

> [!CAUTION]
> 这会在隔离空间中创建一个 `.ssh-command` 文件。除非明确知道自己在做什么，否则请不要修改它。至少不要删除它。

## 第四步 尝试是否成功

现在重新连接超算平台，尝试一下有没有成功进入隔离环境。

成功进入时会有提示。或者可以 `echo ~` 查看当前家目录是否被正确修改。

## 已知不兼容的程序

隔离的原理仅仅是修改环境变量（具体原理详见[《实现细节》](#实现细节)部分）。如果程序不尊重 `HOME` 变量，例如，部分程序可能读取 `getent passwd` ，此时这个隔离方案是不起效果的。这里我们列出一些已知不兼容的程序，以及建议的替代方案。

### SSH 不兼容

除非专门指定文件， SSH 总是读取 `/share/home/u13070/.ssh` 而不使用隔离空间的 `~/.ssh` 。

应当说明的是，这并非隔离空间带来的新问题，而只是隔离空间没能帮助解决 SSH 的混乱。如果要在超算平台进一步使用 SSH 访问其他服务，可以使用 SSH Agent 转发，详见[《配置 SSH Agent 》](./../configure-ssh-agent/)。

### tmux 不兼容

tmux 默认连接 `/tmp/tmux-<UID>/default` 处的服务，这会导致所有人进入同一个服务。同时，服务启动时会继承现有的环境变量，导致隔离被破坏。

不过这个很容易解决，只需要将 `TMUX_TMPDIR` 配置到隔离空间中即可。例如，在 `.bashrc` 中添加：

```sh
# ===== tmux =====
# https://tjslp-hpc.yueyinqiu.top/docs/quick-start/create-isolation-space/#tmux-%e4%b8%8d%e5%85%bc%e5%ae%b9
export TMUX_TMPDIR="$HOME/.tmux/tmp"
mkdir -p "$TMUX_TMPDIR"
# ===== tmux =====
```

## 切换其他设备

如果要在其他设备使用同一个隔离环境，一般建议先按[《第一步 生成 SSH 密钥》](#第一步-生成-ssh-密钥)重新生成一个密钥。

随后，登录到超算平台执行：

```sh
~/register_space.cs
```

> [!CAUTION]
> 该脚本内部访问 `HOME` 环境变量构建路径，因此请确保目前没有位于隔离环境中（ `echo ~` 的输出是 `/share/home/u13070` ）。可以使用 `ssh hpc-u13070 -o PubkeyAuthentication=no` 禁用密钥登录，以避免进入隔离环境。

此时输入你隔离空间的名称。它会发现隔离空间已存在，并询问是否要添加 SSH 密钥，按上述流程继续即可。

## 删除隔离空间

1. 打开 `authorized_keys` ，移除相应密钥；
2. 移除 `/share/home/u13070/data/<隔离空间名称>` ；
3. 移除 `/ssdfs/datahome/u13070/<隔离空间名称>` 。

## 实现细节

了解一些实现细节可以帮助定制功能，或者排查问题。

### 原理

见 [https://www.cnblogs.com/yueyinqiu/p/19631295](https://www.cnblogs.com/yueyinqiu/p/19631295) 。如果脚本丢失，可[点此](./register_space.cs)下载。

### 可能遇到的问题

`register_space.cs` 需要 .NET 10 或以上版本执行，并需要安装 `YueYinqiu.Su.DotnetRunFileUtilities` 包。在本篇撰写时，这些网站是不能在超算平台访问的。为此，我们有部署一个代理服务，并配置在 `/share/home/u13070/.bashrc` 中。

如果代理服务正常运行，理论上不会出现问题。但若代理出现异常，且包缓存被删除，可能导致脚本无法执行，可参考[《连接到互联网》](./../connect-to-the-internet/)进行代理配置；如果 .NET 本身被删除，可参考[《 .NET 》](./../../more-content/dnet/)进行安装。
