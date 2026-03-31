---
weight: 2000
title: "配置 SSH 代理"
---

# 配置 SSH 代理

为了能够在登录节点继续使用本地 SSH 密钥进一步访问其他设备（例如，使自己在访问超算平台的其他节点时，能够同样进入隔离空间），我们必须配置 SSH 代理转发。

由于 GitHub 具有直观的登录成功与否的验证方式，这里将以 GitHub 作为案例。

## 第一步 本地使用 SSH 访问 GitHub

> [!TIP]
> 如果本地已经在使用 SSH 访问 GitHub ，可以跳过此步。

使用浏览器访问 [GitHub](https://github.com/settings/profile) ，点击右上角头像，进入 `Settings` 。进入后，在侧边栏点击 `SSH and GPG Keys` 。随后点击右侧的 `New SSH Key` 按钮。

这里有三个选项：
- `Title` ：为这个密钥取一个名字。如果有多个设备或者多个密钥，可以帮助区分；
- `Key Type` ：选择 `Authorization Key` ；
- `Key` ：填入[《创建隔离空间》](./../create-isolation-space/)一章中生成的 SSH 密钥。

配置完成后，应当可以在本地使用以下命令访问 GitHub ，并看到自己的用户名：

```sh
ssh -T git@github.com
```

## 第二步 开启 SSH 代理服务

首先我们要在本地开启 SSH 代理服务，这样超算平台才能访问它并使用它提供的密钥。

在 Windows 上，默认是不开启 SSH 代理服务的，需要先开启该服务。在具有管理员权限的 PowerShell 中运行：

```powershell
Set-Service ssh-agent -StartupType Automatic
Start-Service ssh-agent
```

在 Linux 上，一般是默认开启的。如果没有，可以使用以下指令开启：

```sh
eval "$(ssh-agent -s)"
```

> [!TIP]
> 使用 `ssh-add -l` 可以检测 SSH 代理服务是否打开并正常运行。（提示 `Could not open a connection` 说明服务未开启，而提示 `The agent has no identities` 说明服务是开启的，只是还没有添加密钥。）

## 第三步 开启 SSH 代理转发

接下来，我们修改 SSH 配置文件，为超算平台添加 `ForwardAgent` 和 `AddKeysToAgent` 配置，例如：

```text {hl_lines=["5-6"]}
Host hpc-u13070
    HostName logini.tongji.edu.cn
    User u13070
    Port 10022
    ForwardAgent yes
    AddKeysToAgent yes
```

其中 `ForwardAgent yes` 意思是开启代理转发功能。而 `AddKeysToAgent` 表示把连接超算平台时使用的密钥自动添加到代理服务中：如果超算平台和 GitHub 使用同一个密钥，就不需要再另外配置了；如果使用不同密钥，需要在本地使用 `ssh-add` 添加 GitHub 的密钥。

## 第三步 在超算平台上访问 GitHub

现在使用 `ssh hpc-u13070` 登录超算平台，尝试：

```ssh
ssh -T git@github.com
```

应当可以看到成功登录的提示。

> [!TIP]
> 如果出现类似 `ssh: connect to host github.com port 22: Connection timed out` 等连接错误，可能是因为 `22` 端口被封禁了。可以尝试 `ssh.github.com:443` ：
>
> ```sh
> ssh -T git@ssh.github.com -p 443
> ```
>
> 为了将来从官网直接复制仓库链接，可以在 `/share/home/u13070/.ssh/config` 作如下配置：
> 
> ```text
> Host github.com
>    Hostname ssh.github.com
>    Port 443
> ```

## 第四步 尝试克隆仓库

在 GitHub 上选择或者创建一个私有仓库，点击绿色的 `Code` 按钮，切换到 `SSH` 选项卡，会得到一个类似 `git@github.com:yueyinqiu/TjslpHpcHandbook.git` 的仓库链接。

在超算平台上，输入 `git clone` 并粘贴该仓库链接，然后执行。例如：

```sh
git clone git@github.com:yueyinqiu/TjslpHpcHandbook.git
```

应当可以成功克隆。

## 第五步 尝试切换节点

> [!TIP]
> 在继续之前，请确保已参考[《创建隔离空间》](./../create-isolation-space/)完成隔离空间的创建。

前面我们成功通过 SSH 代理转发，在超算平台直接使用本地密钥访问 GitHub 。现在，将进一步确认能否正常在超算平台上切换节点。

在超算平台上执行下述命令，尝试再次连接自己：

```sh
ssh localhost -p 10022
```

它应当会使用 SSH 代理中的本地密钥，从而进入隔离空间。可使用 `echo ~` 进行确认。

> [!TIP]
> 在 `ssh localhost -p 10022` 连接自己后，在此处重新 `ssh localhost -p 10022` ，会发现它没有进入隔离空间。这是因为在第一次连接自己时没有进行 SSH 代理转发，改为 `ssh localhost -p 10022 -A` 即可。