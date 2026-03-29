---
weight: 5000
title: "基于 GitHub 的版本管理"
---

# 基于 GitHub 的版本管理

为了更好地管理代码、进行协作，我们不可避免要使用 GitHub 。

## 第一步 使用 SSH 访问 GitHub

> [!TIP]
> 如果已经在使用 SSH 访问 GitHub ，可以跳过此步。

一般来说，我们倾向于使用 SSH 访问 GitHub 仓库。

使用浏览器访问 [GitHub](https://github.com/settings/profile) ，点击右上角头像，进入 `Settings` 。进入后，在侧边栏点击 `SSH and GPG Keys` 。随后点击右侧的 `New SSH Key` 按钮。

这里有三个选项：
- `Title` ：就是取一个名字。如果有多个设备或者多个密钥，可以帮助区分哪个是哪个；
- `Key Type` ：选择 `Authorization Key` ；
- `Key` ：填入[《创建隔离空间》](./../create-isolation-space/)一章中生成的 SSH 密钥。

> [!TIP]
> 当然也可以另外再生成一个密钥填入 `Key` ，但是这样可能无法使用 `AddKeysToAgent` 配置。

配置完成后，应当可以在本地使用以下命令访问 GitHub ，并看到自己的用户名：

```sh
ssh -T git@github.com
```

## 第二步 配置 SSH 代理转发

在第一步中，我们实现了在本地通过 SSH 密钥访问 GitHub 。接下来我们要通过 SSH 代理转发，使远端服务器也可使用该密钥访问 GitHub 。

在 Windows 上，默认是不开启 SSH 代理服务的，我们需要先开启该服务。在具有管理员权限的 PowerShell 中运行：

```powershell
Set-Service ssh-agent -StartupType Automatic
Start-Service ssh-agent
```

在 Linux 上，一般是默认开启的。如果没有，可以使用以下指令开启：

```sh
eval "$(ssh-agent -s)"
```

> [!TIP]
> 使用 `ssh-add -l` 可以检测 SSH 代理服务是否打开并正常运行。（提示 `Could not open a connection` 说明没开，而提示 `The agent has no identities` 说明是开着的，只是还没有添加密钥。）

随后我们修改 SSH 配置文件，为超算平台添加 `ForwardAgent` 和 `AddKeysToAgent` 配置，例如：

```text
Host hpc-u13070
    HostName logini.tongji.edu.cn
    User u13070
    Port 10022
    ForwardAgent yes
    AddKeysToAgent yes
```

> [!TIP]
> 如果打算为超算平台和 GitHub 使用不同的密钥，那么 `AddKeysToAgent` 可能不起效果，需要手动调用 `ssh-add` 添加密钥。

## 第三步 在超算平台上访问 GitHub

现在 `ssh hpc-u13070` 登录超算平台，尝试：

```ssh
ssh -T git@github.com
```

应当会看到自己成功登录了。

> [!TIP]
> 如果出现类似 `ssh: connect to host github.com port 22: Connection timed out` 等连接错误，可能是因为 `22` 端口被封禁了。可以尝试 `ssh.github.com:443` ：
>
> ```sh
> ssh -T git@ssh.github.com -p 443
> ```
>
> 为了后续从官网复制仓库链接，可以在 `/share/home/u13070/.ssh/config` 作如下配置：
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

应当可以成功把仓库克隆到本地。

## 第五步 配置用户名和邮箱

> [!CAUTION]
> 请确保已参考[《创建隔离空间》](./../create-isolation-space/)完成隔离空间的创建。用户名和邮箱当然仅应该为自己配置。

在 git 中创建 commit 时，需要给定用户名和邮箱。可以使用以下指令进行全局配置：

```sh
git config --global user.name "<用户名>"
git config --global user.email "<邮箱>"
```

## 第六步 尝试提交修改

现在可以对刚刚克隆的仓库做一些修改。

然后使用：

```sh
git add .
git commit -m "modified blahblah"
git push
```

> [!TIP]
> 当然，这里不一定要用命令行，例如可以用 Visual Studio Code 。

## 第七步 更进一步

git 功能复杂，没有办法进行全面整理。可浏览以下文档以更好使用 git 和 GitHub ：
- [git 官方文档](https://git-scm.com/docs)
- [Visual Studio Code 文档](https://code.visualstudio.com/docs/sourcecontrol/overview)
- [git 官方文档（中文）](https://git-scm.cn/docs)
- [同济超算平台文档](https://dev.tongji.edu.cn/hpc-doc/#/pages/basicKnowledge/git)
