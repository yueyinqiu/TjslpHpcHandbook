---
weight: 1000
title: "连接超算平台"
---

# 连接超算平台

> [!NOTE]
> 当然我们说的是 SSH 。应该没有人会用那个网页吧？也不对，应该是没有人不会用。

## 第一步 确认你有 SSH

随便打开一个终端，无论是 bash 、 cmd 、 PowerShell 还是什么乱七八糟。输入 `ssh` 按下回车。正常情况下，会跳出来这么一串东西：

```text
usage: ssh [-46AaCfGgKkMNnqsTtVvXxYy] [-B bind_interface] [-b bind_address]
           [-c cipher_spec] [-D [bind_address:]port] [-E log_file]
           [-e escape_char] [-F configfile] [-I pkcs11] [-i identity_file]
           [-J destination] [-L address] [-l login_name] [-m mac_spec]
           [-O ctl_cmd] [-o option] [-P tag] [-p port] [-Q query_option]
           [-R address] [-S ctl_path] [-W host:port] [-w local_tun[:remote_tun]]
           destination [command [argument ...]]
```

> [!NOTE]
> 什么叫没有 `ssh` ？不信。

## 第二步 在校园网内尝试连接

接下来确保你位于校园网内，输入：

```sh
ssh @hpc-user@@@logini.tongji.edu.cn -p 10022
```

然后输入密码。相信你肯定连上了。

> [!IMPORTANT]
> 这里 @hpc-user@ 是超算用户名，如果你不是这位老师的学生，请用那个正确的用户名。在页面的最顶端，你可以自定义显示的用户名，这样就不需要手动改命令了。

> [!NOTE]
> 什么叫输入密码没反应？密码是不会显示出来的。你就蒙眼按键盘，输完按回车。

## 第三步 谁记得住命令啊？使用 SSH 配置文件

上面这个命令还是太麻烦了，在你的电脑上找到 SSH 的配置文件 `@local-home@/.ssh/config` 。

> [!IMPORTANT]
> 这里 `@local-home@/.ssh/config` 是 SSH 配置文件的默认位置。如果你曾经通过某种方式调整过它的位置，那么要到对应位置去找。如果你本机的家目录不是 `@local-home@` ，请对应调整（同样可以在页面最顶端修改）。

把它当文本文件打开，然后把这团东西塞进去：

```text
Host @ssh-host@
    HostName logini.tongji.edu.cn
    User @hpc-user@
    Port 10022
```

保存它。

> [!TIP]
> 如果你不想叫它 `@ssh-host@` ，也可以在页面顶端修改。

回到我们的终端，输入 `ssh hpc` ，输入密码。相信你已经成功登录了。

> [!TIP]
> 这里说的是你电脑的终端。由于刚刚那个终端已经连到超算平台了，如果想继续用那个终端，可以先 `exit` 退出。

## 第四步 谁记得住密码啊？使用密钥登录

如果每次连接都要输入那一串随机生成的、毫无逻辑的密码，你迟早被烦死。

> [!NOTE]
> 什么叫密码是 `123456` ？等着被盗号吧就。

在你的电脑上使用 `ssh-keygen` 生成一对密钥，把它放在 `@local-home@/.ssh/id_for_hpc` ：

```text
Generating public/private ed25519 key pair.
Enter file in which to save the key (@local-home@/.ssh/id_ed25519): @local-home@/.ssh/id_for_hpc
Enter passphrase (empty for no passphrase): 
Enter same passphrase again: 
Your identification has been saved in id_for_hpc
Your public key has been saved in id_for_hpc.pub
The key fingerprint is:
SHA256:ckHz/3YSA77qliwTUuCQ23VCByGZd99kFnmiiOXpV3A yueyi@@DESKTOP-VE9QGI5
The key's randomart image is:
+--[ED25519 256]--+
|     ..+*o.   .o |
|    o +o+++ . E .|
|     = +.B.+.O o |
|    . o o.+oo.o  |
|      ..S.  o.o  |
|      .o. . .o o |
|       . o o. + .|
|        o +. . o |
|         =o      |
+----[SHA256]-----+
```

这里我们设置了密钥的位置为 `@local-home@/.ssh/id_for_hpc` ，但是实际进去将会看到两个文件：一个叫 `id_for_hpc` ，一个叫 `id_for_hpc.pub` 。

> [!CAUTION]
> 其中不带 `.pub` 的是私钥。请保管好它，不要泄露。如果你使用的设备不安全，或者希望在未来通过任何方式传递私钥，在先前生成时应当为它设置一个 passphrase （当然一般不需要，换设备重新生成一个就是了）。现在删掉它重新再生成一个也来得及。

现在要做的事情是，打开 `id_for_hpc.pub` ，会看到类似于这样的内容：

```text
ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIBwLLOJbq3byqJ8/KREL+93wzIUjpXQ75SUTTXRdE4BH yueyi@@DESKTOP-VE9QGI5
```

把它复制下来。

然后登录超算平台：

```sh
ssh @ssh-host@
```

进入之后输入

```sh
echo "" >> ~/.ssh/authorized_keys    # 确保前面有个换行，免得两个拼到一行去了
echo "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIBwLLOJbq3byqJ8/KREL+93wzIUjpXQ75SUTTXRdE4BH yueyi@@DESKTOP-VE9QGI5" >> ~/.ssh/authorized_keys
```

> [!IMPORTANT]
> 这里密钥换成你自己的，不要粘这里的。

接下来在你的电脑上，还是那个 SSH 配置文件，加一行 `IdentityFile` ：

```text
Host @ssh-host@
    HostName logini.tongji.edu.cn
    User @hpc-user@
    Port 10022
    IdentityFile @local-home@/.ssh/id_for_hpc
```

现在重新尝试连接：

```sh
ssh @ssh-host@
```

这次应该不用输入密码就能进去了。

> [!TIP]
> 如果直接登不上了，不要着急，你仍然可以使用密码登录（例如，使用 `ssh @hpc-user@@@logini.tongji.edu.cn -p 10022 -o PubkeyAuthentication=no` ）。可以登录上去检查 `authorized_keys` 文件，看看是不是哪里写错了。

## 第五步 在校园网外连接

> [!NOTE]
> 不会。
