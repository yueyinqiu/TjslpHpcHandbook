---
weight: 4000
title: "连接到互联网"
---

# 连接到互联网

> [!NOTE]
> 这一篇真不知道怎么写……主要是该不该写……但这个真的真的非常重要：现在超算平台似乎是白名单，已经到影响使用的地步了，不知道为什么这么严格。

> [!WARNING]
> 本篇包含两个方案，只需选择其中一种。如果条件允许，尽量选择方案二，因为它的功能更加全面。

## 方案一 临时使用本机代理

这个方案的原理，是在你的电脑上启动代理软件，再通过端口转发，使超算平台的请求经过代理软件，从而访问网络。

### 第一步 本地开启代理

如果你本地已经有代理，那很好，就用这个就好。

如果你本地没有，那去下载一个吧，你总会用到的。这里推荐几个：
- [https://www.clashverge.dev/](https://www.clashverge.dev/)
- [https://github.com/nelvko/clash-for-linux-install](https://github.com/nelvko/clash-for-linux-install)
- [https://github.com/chen08209/FlClash](https://github.com/chen08209/FlClash)

> [!TIP]
> 这个代理是为了解决正常国内网站都访问不了的问题，因此不需要可以访问国际互联网。
> 
> 一个最简单的配置是：
>
> ```text
> mixed-port: @local-proxy@
> mode: DIRECT
> ```
> 
> 如果你希望这个代理和你平时使用的完全分开，不妨直接[下载 mihomo](https://github.com/MetaCubeX/mihomo/releases) 并使用上述配置启动。

代理肯定会开一个端口接受请求，这里假定你代理的端口在 `127.0.0.1:@local-proxy@` 。如果不是，可以在页面顶部修改一下。

### 第二步 进行端口转发

接下来你要随机挑一个端口号，比如你挑到了 `@remote-proxy@` 。

> [!IMPORTANT]
> 请确实随机生成一个，以免端口号冲突。在页面顶部可以修改显示的端口号。

接着，改用下面的命令连接超算平台，它会把本地的 `@local-proxy@` 端口转发到超算平台的 `@remote-proxy@` 端口：

```sh
ssh -R @remote-proxy@:127.0.0.1:@local-proxy@ @ssh-host@
```

### 第三步 测试是否成功

找一个原本没法访问的网站尝试一下：

```sh
curl -x http://localhost:@remote-proxy@/ https://tools.yueyinqiu.top/
```

应当是可以成功访问的，本地代理也应当可以看到一次请求记录。

> [!TIP]
> 你可能需要先 `curl https://tools.yueyinqiu.top/` 确认一下目前的网络情况。撰写时确实没法访问，但白名单随时可能调整。

### 第四步 修改 SSH 配置

在 SSH 配置文件中，可以添加 `RemoteForward` 让它自动进行端口转发，例如：

```text
Host @ssh-host@
    HostName logini.tongji.edu.cn
    User @hpc-user@
    Port 10022
    IdentityFile @local-home@/.ssh/id_for_hpc
    RemoteForward @remote-proxy@ 127.0.0.1:@local-proxy@
```

> [!TIP]
> 应该还记得 SSH 配置文件是什么吧？在[《连接超算平台》](./../connect-to-hpc/)中，我们直接通过路径打开并编辑了这个文件。
> 
> 在 Visual Studio Code 中按下 `Ctrl + Shift + P` ，有一个 `> Remote-SSH: Open SSH Configuration File...` 可以直接打开 SSH 配置文件。

现在，只要 `ssh @ssh-host@` 进去，应该就可以直接使用代理了。

### 第五步 设置环境变量

> [!CAUTION]
> 在继续之前，请确认你跟随[《创建隔离空间》](./../create-isolation-space/)完成了隔离空间的配置。当你结束端口转发后，其他人不再能通过这个端口访问网络。因此，你只应当为你自己进行配置。

很多程序都会尊重 `HTTP_PROXY` 等环境变量，例如你可以执行：

```sh
export https_proxy="http://localhost:@remote-proxy@/"
```

此时不需要 `-x` ， `curl` 也会使用这个代理：

```sh
curl https://tools.yueyinqiu.top/
```

一般来说，你可以把以下内容写入 `.bashrc` 中，这样你启动 bash 时就会自动加载了：

```sh
export http_proxy="http://localhost:@remote-proxy@/"
export https_proxy="http://localhost:@remote-proxy@/"
export HTTP_PROXY="http://localhost:@remote-proxy@/"
export HTTPS_PROXY="http://localhost:@remote-proxy@/"

export no_proxy="localhost,127.0.0.1"
export NO_PROXY="localhost,127.0.0.1"
```

> [!TIP]
> 写入 `.bashrc` 后是不会在当前终端生效的，得新开一个 `bash` 才会在其中生效。

> [!NOTE]
> 什么叫怎么写入 `.bashrc` ？用 Visual Studio Code 打开你那个 `~/.bashrc` 文件，粘贴进去就完事了。（这里的 `~` 是指你隔离空间的位置。）

## 方案二 常开设备部署代理

上述方案有一个问题：当本地设备关闭后，端口转发也随之断开，如果里面有正在运行的程序，就直接断网了。

最好的办法是找一台可以保持开启的设备，把代理部署在这个常开设备上面。

> [!NOTE]
> 没有设备？配置麻烦？你说有没有可能：早就有人部署了这样一个服务可以直接用！（如果你认识维护这篇文档的人，或许你应该先向 TA 确认一下。）

### 第一步 设备开启代理

参考[《第一步 本地开启代理》](#第一步-本地开启代理)，在该常开设备中开启代理。

> [!CAUTION]
> 此类长期保持运行的服务应当开启身份验证，以免被盗用或被攻击。见[《第三步 开启身份验证》](#第三步-开启身份验证)。

### 第二步 进行端口转发

参考[《第二步 进行端口转发》](#第二步-进行端口转发)，在该常开设备上完成端口转发。可以继续参考[《第三步 测试是否成功》](#第三步-测试是否成功)进行测试。

### 第三步 开启身份验证

此类长期保持运行的服务应当开启身份验证，以免被盗用或被攻击。

这里以 Clash 为例，若原本配置为：

```text
mixed-port: @local-proxy@
mode: DIRECT
```

可以将它改成：

```text
mixed-port: @local-proxy@
mode: DIRECT
authentication: ["user:password"]
```

> [!IMPORTANT]
> 这里的 `user:password` 当然是要改成自己的，且应该用强密码。

此时改用 `http://user:password@@localhost:@remote-proxy@/` 即可访问代理，例如：

```sh
curl -x http://user:password@@localhost:@remote-proxy@/ https://tools.yueyinqiu.top/
```

> [!CAUTION]
> 在这里直接使用用户名密码是不安全的，因为可能会被其他用户通过 `ps` 捕获。正式使用时，应当写在环境变量中，见[《第六步 设置环境变量》](#第六步-设置环境变量)。

### 第四步 解决跨节点问题

由于登录节点并非只有一个，然而上述端口转发仅转发到一个节点。这里我们考虑：先把它转发到特定节点，然后把服务开放到整个局域网。

首先我们要固定在某一个节点上做转发，以便后续找到该服务。例如我们选择 `logini01` 节点（ IP 为 `192.168.212.236` ），并另外选择一个端口 `XXXXX` 作为中转：

```sh
# 在该常开设备上执行，保持运行
ssh -R XXXXX:127.0.0.1:@local-proxy@ -p 10022 @hpc-user@@@192.168.212.236
```

> [!IMPORTANT]
> 这里的 `XXXXX` 也是要一个随机端口号。

> [!TIP]
> 节点的 IP 可能会变化。可以使用 `nslookup logini.tongji.edu.cn` 进行查询。 

此时转发出来的代理只能由 `logini01` 节点本机访问。可以先验证一下是否可用：

```sh
# 在超算平台 logini01 节点上执行（建议使用刚刚在该常开设备上登录的终端）
curl -x http://user:password@@localhost:XXXXX/ https://tools.yueyinqiu.top/
```

接着使用 `ncat` 把它转发到整个局域网：

```sh
# 在超算平台 logini01 节点上执行（建议使用刚刚在该常开设备上登录的终端），保持运行
ncat -lk @remote-proxy@ -c "ncat 127.0.0.1 XXXXX"
```

现在，应该可以在任意节点上使用：

```sh
# 在超算平台任意节点上
curl -x http://user:password@@logini01:@remote-proxy@/ https://tools.yueyinqiu.top/
```

> [!CAUTION]
> 在这里直接使用用户名密码是不安全的，因为可能会被其他用户通过 `ps` 捕获。正式使用时，应当写在环境变量中，见[《第六步 设置环境变量》](#第六步-设置环境变量)。

### 第五步 完整转发指令

如果前述步骤都成功了，那我们可以把它合并为一条指令：

```sh
# 在该常开设备上执行，保持运行
ssh -t -R XXXXX:127.0.0.1:@local-proxy@ -p 10022 @hpc-user@@@192.168.212.236 "ncat -lk @remote-proxy@ -c 'ncat 127.0.0.1 XXXXX'"
```

以后只需要执行这条指令就可以开启转发（理想状态下它当然是一直运行根本不用动，但不好说）。

### 第六步 设置环境变量

参考[《第五步 设置环境变量》](#第五步-设置环境变量)进行环境变量的设置，其中代理改为 `http://user:password@@logini01:@remote-proxy@/` 即可。

### 第七步 在计算节点使用

相比于临时使用本机代理的方案，这个方法还有另外一个优势：在计算节点也可以使用。

不需要额外操作，使用 `http://user:password@@logini01:@remote-proxy@/` 作为代理即可。
