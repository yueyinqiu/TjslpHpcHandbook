---
weight: 3000
title: "使用 Visual Studio Code"
---

# 使用 Visual Studio Code

> [!NOTE]
> VSCode 太香辣！目前来看在远程开发方面它的体验还是最好的。

## 第一步 本地安装 Visual Studio Code

想必大家已经都在用了，但还是给个下载链接吧 [https://code.visualstudio.com/](https://code.visualstudio.com/) 。

## 第二步 确认你正在使用隔离空间

请确认你跟随[《创建隔离空间》](./../create-isolation-space/)完成了隔离空间的配置。否则，你可能和其他人共享一个 `vscode-server` ，你们的插件将是共用的，你们在远端的配置也是共用的。这……通常不太好……

## 第三步 直接用啊！

`Remote - SSH` 扩展是默认安装好的，可以在侧边栏找到 `Remote Explorer` 。然后如果你按照[《连接超算平台》](./../connect-to-hpc/)配置了，里面会有一个 `@ssh-host@` 。直接点右边的箭头就完事了。

> [!TIP]
> 如果你发现首次使用 Visual Studio Code 连接时，竟然没有 `Downloading VSCode Server...` 的过程，或者看到一堆眼花缭乱的扩展，请 `echo $HOME` 进行检查，确认你是否在隔离空间中。

> [!NOTE]
> 啊对吗？那为什么需要这一篇呢？因为后面的一些内容可能会包括“ Visual Studio Code 上这样这样做更好”等等。那这里必须先出现一下，确保完全跟随这个《快速开始》走是没有问题的。
