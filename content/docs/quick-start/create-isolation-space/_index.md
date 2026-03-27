---
weight: 2000
title: "创建隔离空间"
---

# 创建隔离空间

> [!NOTE]
> 一个人用一个用户是一个 Linux 一个基本的一个设计。然而学校只让老师开用户，好多配置都混在一起，这咋办啊？要不把家目录改了吧！

> [!CAUTION]
> 本篇只能帮助你创建一个相对隔离的空间。主要目的是：
> 1. 防止自己在不知情的情况下，用了其他人的配置；
> 2. 可以自己大胆进行一些配置，而不用担心影响其他人。
> 
> 由于实际还是同一个用户，无法防止你的文件被访问和篡改。

## 第一步 确认你正在使用密钥登录

为了在登录时区分你的身份，必须使用密钥登录，而不能使用密码登录。

[《连接超算平台》](./../connect-to-hpc/)从零开始详细介绍了如何使用密钥登录超算平台。

## 第二步 创建你的隔离家目录

在超算平台上创建你的专属目录：

```sh
mkdir "@fake-home@"
```

> [!IMPORTANT]
> 你未必想在 `@fake-home@` 创建目录。在页面顶部可以修改显示的路径。
> 
> 特别注意它和超算用户名 `@hpc-user` 是否匹配。
> 
> 另外建议不要在其中包含 `~` 等环境变量，因为这些环境变量将被修改，有可能会在未来导致问题。

> [!TIP]
> 这个命令当然是在超算平台执行的，先 `ssh @ssh-host@` 连接到超算平台。

## 第三步 准备 SSH 登录时的脚本

在超算平台，使用 `nano "@fake-home@/ssh-command.sh"` 创建一个脚本文件，写入：

```sh
#!/bin/bash

NEW_HOME="@fake-home@"
NEW_ENV="HOME=$NEW_HOME TERM=$TERM"

cd $NEW_HOME

if [ -z "$SSH_ORIGINAL_COMMAND" ]; then
    exec env -i $NEW_ENV /bin/bash --login
else
    exec env -i $NEW_ENV /bin/bash --login -c "$SSH_ORIGINAL_COMMAND"
fi
```

> [!WARNING]
> 请尽量理解这段脚本做了什么，以便后续定制：它启动了一个新的 `/bin/bash` 替换当前的终端。其中环境变量 `HOME` 被修改到指定路径，并原样保留了 `TERM` ，其余环境变量都被抛弃。当然，启动时使用了 `--login` ，这会重新运行各 `profile` 脚本以添加部分必要的环境变量。

> [!NOTE]
> 什么叫不会用 `nano` ？
> 1. 刚进去就是写东西的地方，就直接把上面这段粘贴进去。
> 2. 粘贴好之后，按 `Ctrl + X` 。
> 3. 这时候它会问 `Save modified buffer?` 。按 `Y` 。
> 4. 这时候会问 `File Name to Write:` 。但它应该预先帮你填好了，按回车就行。

写完后别忘记给它加上执行权限：

```sh
chmod +x "@fake-home@/ssh-command.sh"
```

## 第四步 让这个脚本在登录时执行

现在 `nano ~/.ssh/authorized_keys` 打开之前配置密钥时所写的文件。

> [!TIP]
> 这里的 `~` 是指原本的用户目录而不是新的隔离目录。如果你迫不及待地执行了前面的脚本导致 `~` 指向的位置发生变化，建议先退出来重新连接超算平台。

然后找到你的密钥，在前面添加一个 `command="@fake-home@/ssh-command.sh"` 。例如，如果你本来的密钥是：

```text
ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIBwLLOJbq3byqJ8/KREL+93wzIUjpXQ75SUTTXRdE4BH yueyi@DESKTOP-VE9QGI5
```

把它改成：

```text
command="@fake-home@/ssh-command.sh" ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIBwLLOJbq3byqJ8/KREL+93wzIUjpXQ75SUTTXRdE4BH yueyi@DESKTOP-VE9QGI5
```

## 第五步 尝试一下是否成功

现在重新连接超算平台：

```sh
ssh @ssh-host@
```

然后尝试：

```sh
echo $HOME
```

如果你的输出变成了 `@fake-home@` 。恭喜你，成功了！

> [!TIP]
> 如果直接登不上了，不要着急，你仍然可以使用密码登录（例如，使用 `ssh @hpc-user@@@logini.tongji.edu.cn -p 10022 -o PubkeyAuthentication=no` ）。可以登录上去检查上述脚本或者 `authorized_keys` 文件，看看是不是哪里写错了。

## 第六步 把 `.bashrc` 什么的创建起来

直接从 `/etc/skel/` 下面拷就行：

```sh
cp -r /etc/skel/. "@fake-home@"
```

另外你可能会看到类似 `Saving key "@fake-home@/.ssh/id_rsa" failed: No such file or directory` 之类的提示。那就给它创建个文件夹吧：

```sh
mkdir "@fake-home@/.ssh"
```

好了，现在重新登录。应该一切正常，干干净净！
