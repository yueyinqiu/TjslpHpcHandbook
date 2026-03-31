---
weight: 4000
title: "Conda"
---

# Conda

> [!TIP]
> 建议使用 uv 而不是 `conda` ，详见[《基于 uv 的 Python 环境》](./../../quick-start/python-environment-on-uv/)。

出于隔离性，建议自行安装 Miniconda ，而非使用超算平台提供的 `conda` 。

## 第一步 安装 Miniconda

> [!CAUTION]
> 在继续之前，请确保已参考[《创建隔离空间》](./../create-isolation-space/)完成隔离空间的创建。不要为其他人安装。

选择一个合适的目录，执行：

```sh
curl -O https://mirrors.tuna.tsinghua.edu.cn/anaconda/miniconda/Miniconda3-latest-Linux-x86_64.sh 
bash ./Miniconda3-latest-Linux-x86_64.sh
conda init
```

## 第二步 配置镜像源

在 `~/.condarc` 写入：

```yaml
channels:
    - defaults
show_channel_urls: true
default_channels:
    - https://mirrors.tuna.tsinghua.edu.cn/anaconda/pkgs/main
    - https://mirrors.tuna.tsinghua.edu.cn/anaconda/pkgs/r
    - https://mirrors.tuna.tsinghua.edu.cn/anaconda/pkgs/msys2
custom_channels:
    conda-forge: https://mirrors.tuna.tsinghua.edu.cn/anaconda/cloud
    pytorch: https://mirrors.tuna.tsinghua.edu.cn/anaconda/cloud
```

## 第三步 完成

正常使用 `conda` 即可。
