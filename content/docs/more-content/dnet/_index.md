---
weight: 1000
title: ".NET"
---

# .NET

C# 是一个相当现代化的语言。在 .NET 10 中，发布了 `dotnet run script.cs` 的功能，使得使用 C# 编写临时脚本成为可能。

[《创建隔离空间》](./../../quick-start/create-isolation-space/)中的脚本就是使用 C# 编写。在本网站的其他位置，也出现 C# 编写的脚本。为了避免无法运行它们，这里详细列出 .NET 的安装流程。

## 第一步 安装 .NET

> [!CAUTION]
> 请确保已参考[《创建隔离空间》](./../create-isolation-space/)完成隔离空间的创建。

选择一个合适的目录，执行：

```sh
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x ./dotnet-install.sh
./dotnet-install.sh --version latest
```

## 第二步 配置环境变量

在 `~/.bashrc` 添加：

```sh
# ===== .NET =====
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools
# ===== .NET =====
```

记得启动新的 `bash` 令配置生效。

## 第三步 完成

正常使用即可。
