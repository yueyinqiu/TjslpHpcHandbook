---
weight: 3000
title: "基于 Homebrew 的包管理"
---

# 基于 Homebrew 的包管理

大多数包管理器都需要 root 权限，在超算平台上无法使用。虽然官方明确不受支持，但 Homebrew 确实能够在完全无 root 的情况下使用。因此，如有需要，建议使用 Homebrew 作为包管理器。

## 第一步 下载 Homebrew

> [!CAUTION]
> 请确保已参考[《创建隔离空间》](./../create-isolation-space/)完成隔离空间的创建。

```sh
git clone --depth 1 https://mirrors.tuna.tsinghua.edu.cn/git/homebrew/brew.git ~/.homebrew
```

## 第二步 配置环境变量

在 `~/.bashrc` 中添加：

```sh
# ===== Homebrew =====
eval "$(~/.homebrew/bin/brew shellenv)"

export HOMEBREW_BREW_GIT_REMOTE="https://mirrors.tuna.tsinghua.edu.cn/git/homebrew/brew.git"
export HOMEBREW_CORE_GIT_REMOTE="https://mirrors.tuna.tsinghua.edu.cn/git/homebrew/homebrew-core.git"
export HOMEBREW_API_DOMAIN="https://mirrors.tuna.tsinghua.edu.cn/homebrew-bottles/api"
export HOMEBREW_BOTTLE_DOMAIN="https://mirrors.tuna.tsinghua.edu.cn/homebrew-bottles"
# ===== Homebrew =====
```

> [!TIP]
> 如果不想使用镜像源，那就不需要设置下面四个环境变量。

完成后启动新的 `bash` 让配置生效。

## 第三步 更新 `brew`

```sh
brew update
```

## 第四步 配置 `homebrew/core` 镜像

```sh
brew tap --force --custom-remote homebrew/core https://mirrors.tuna.tsinghua.edu.cn/git/homebrew/homebrew-core.git
```

> [!TIP]
> 如果不想使用镜像源，可以跳过此步。

## 第五步 尝试安装包

```sh
brew install hugo
hugo help
```

## 第六步 更进一步

无 root 使用是明确不受 Homebrew 官方支持的。很多包可能不得不从源码编译，而由于环境的复杂性，未必总能成功。同时， Homebrew 也不保证接口的稳定，因此镜像源未必稳定。如果遇到问题，可以进一步查阅：
- [Homebrew 官方文档](https://docs.brew.sh/)
- [清华源镜像文档](https://mirrors.tuna.tsinghua.edu.cn/help/homebrew/)
