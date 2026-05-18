---
weight: 2500
title: "Pixi"
---

# Pixi

Pixi 是一个现代化的通用包管理工具，已基本可以完全替代 Conda 。

## 第一步 安装 Pixi

> [!CAUTION]
> 在继续之前，请确保已参考[《创建隔离空间》](./../create-isolation-space/)完成隔离空间的创建。不要为其他人安装。

执行下述指令即可安装 Pixi ：

```sh
PIXI_DOWNLOAD_URL=https://gh-proxy.org/https://github.com/prefix-dev/pixi/releases/latest/download/pixi-x86_64-unknown-linux-musl.tar.gz curl -fsSL https://gh-proxy.org/https://github.com/prefix-dev/pixi/releases/latest/download/install.sh | sh
```

> [!TIP]
> 官方脚本是 `curl -fsSL https://pixi.sh/install.sh | sh` ，上述指令使用 `https://gh-proxy.org/` 作为 GitHub 镜像。

完成后，启动一个新的 `bash` 令配置生效。

## 第二步 配置镜像源和缓存位置

创建或编辑 `~/.pixi/config.toml` ：

```toml
# 类似于 uv ，把缓存位置设置到原本的家目录下。如果不想和其他人共享缓存，可跳过。
[cache]
root = "/share/home/u13070/.cache/rattler/cache"


[mirrors]
# PyPI 镜像
"https://pypi.org/simple" = ["https://mirrors.tuna.tsinghua.edu.cn/pypi/web/simple"]
"https://files.pythonhosted.org/packages" = ["https://mirrors.tuna.tsinghua.edu.cn/pypi/web/packages"]

# conda-forge 镜像，一般来说就使用这个通道，这里便不配置其他通道的镜像了
"https://conda.anaconda.org/conda-forge" = ["https://mirrors.tuna.tsinghua.edu.cn/anaconda/cloud/conda-forge"]

# Conda mapping 镜像
# 注意：
# 由于目前没有 https://conda-mapping.prefix.dev 的镜像，这里采用了一个临时的 work-around ：把它镜像到百度，始终返回 404 ，以完全使用 prefix-dev/parselmouth 提供的 compressed_mapping.json 。
# 但是， Pixi 正计划将所有内容均放在 https://conda-mapping.prefix.dev ，届时这个 work-around 将失效。见 https://github.com/prefix-dev/pixi/discussions/6124 。
# 目前已经在清华源的仓库发起了镜像请求，见 https://github.com/tuna/issues/issues/2522 。
"https://conda-mapping.prefix.dev" = ["https://www.baidu.com/please_give_me_a_404"]
"https://raw.githubusercontent.com/prefix-dev/parselmouth/main/files" = ["https://gh-proxy.org/https://raw.githubusercontent.com/prefix-dev/parselmouth/main/files"]
```

> [!WARNING]
> 上述 Conda mapping 镜像是一个暂时的 work-around 。见其中注释。

## 第三步 创建测试项目

找一个合适的位置，运行：

```sh
pixi init my-test-pixi-workspace
cd my-test-pixi-workspace
```

使用 `pixi add` 为项目添加依赖：

```sh
# 安装 Conda 包（由于 numpy 依赖 Python ，这会自动安装 Python 。如果想指定 Python 版本，执行如 pixi add python==3.12 即可。）
pixi add numpy
# 安装 PyPI 包
pixi add --pypi scipy
```

在 Conda 中，上述安装方法可能导致混乱，但是 Pixi 能够处理 Conda 和 PyPI 包的依赖关系（依靠前面配置过镜像的 Conda mapping ）。使用 `pixi list` 可以确认当前安装的包。

接下来可以尝试运行：

```sh
pixi run python -c "import numpy; from scipy.optimize import fsolve; f = lambda x: x ** 2 - 4; print(fsolve(f, x0=[1, -1]))"
```

## 第四步 重建测试项目

类似于 `pyproject.toml` ， Pixi 使用 `pixi.toml` 作为环境配置。

现在，删除环境：

```sh
rm -r .pixi
```

运行：

```sh
pixi install
```

即可还原环境。

## 第五步 安装 PyTorch

首先在 `pixi.toml` 中添加 `system-requirements` ：

```toml {hl_lines=["16-17"]}
[workspace]
authors = ["yueyinqiu <yueyinqiu@outlook.com>"]
channels = ["conda-forge"]
name = "my-test-pixi-workspace"
platforms = ["linux-64"]
version = "0.1.0"

[tasks]

[dependencies]
numpy = ">=2.4.4,<3"

[pypi-dependencies]
scipy = ">=1.17.1, <2"

[system-requirements]
cuda = "12.8"
```

然后运行：

```sh
CONDA_OVERRIDE_CUDA=12.8 pixi add pytorch-gpu
```

这里设置了 `CONDA_OVERRIDE_CUDA=12.8` ，是因为 Pixi 会检查 `system-requirements` 里配置的 `cuda` 是否满足。然而，登录节点上并没有 GPU ，因此我们需要使用这个环境变量，让它以为 CUDA 是存在的。

为了避免每次都重复写 `CONDA_OVERRIDE_CUDA` ，可以使用 `feature` 功能，配置 CPU 和 GPU 两个环境：

```toml {hl_lines=["10-11", "16-27"]}
[workspace]
authors = ["yueyinqiu <yueyinqiu@outlook.com>"]
channels = ["conda-forge"]
name = "my-test-pixi-workspace"
platforms = ["linux-64"]
version = "0.1.0"

[tasks]

[dependencies]
numpy = ">=2.4.4,<3"

[pypi-dependencies]
scipy = ">=1.17.1, <2"

[feature.gpu.system-requirements]
cuda = "12.8"

[feature.gpu.dependencies]
pytorch-gpu = ">=2.10.0,<3"

[feature.cpu.dependencies]
pytorch-cpu = ">=2.10.0,<3"

[environments]
default = ["cpu"]
gpu = ["gpu"]
```

由于 `default = ["cpu"]` ， `pixi` 将默认使用 CPU 版本。可以尝试：

```sh
pixi run python -c "import torch; print(torch.cuda.is_available())"
```

而在计算节点上，使用 `--environment gpu` 即可选择 GPU 版本，例如：

```sh
module load cuda/12.8
pixi run --environment gpu python -c "import torch; print(torch.cuda.is_available())"
```

> [!TIP]
> 在计算节点运行前，建议先在登录节点使用 `CONDA_OVERRIDE_CUDA=12.8 pixi install --environment gpu` 配置好 GPU 环境，以免在计算节点花时间安装依赖，浪费机时。

## 第六步 更多内容

更多内容可参考：
- [Pixi 官方文档](https://pixi.prefix.dev/latest/)
