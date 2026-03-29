---
weight: 4000
title: "基于 uv 的 Python 环境"
---

# 基于 uv 的 Python 环境

uv 现在已成为现代化 Python 开发的事实标准，统一了 Python 版本管理、包管理、项目管理等功能。可能我们更熟悉的是 Anaconda ，但如果没有什么 C/C++ 的依赖需要处理，真的应该使用 uv 。

除了它足够现代化之外，在这里优先推荐 uv 还有一些其他原因：

1. 不用担心环境变量问题，只要所在目录正确，执行 `uv run python` 绝对不可能使用其他环境；
2. 可以所有人共享一套缓存，不需要在新环境重新下载包，节约时间和空间。

## 第一步 安装 uv

> [!CAUTION]
> 请确保已参考[《创建隔离空间》](./../create-isolation-space/)完成隔离空间的创建。

执行下述指令即可安装 uv ：

```sh
UV_INSTALLER_GITHUB_BASE_URL=https://gh-proxy.org/https://github.com/ curl -LsSf https://github.com/astral-sh/uv/releases/latest/download/uv-installer.sh | sh
```

> [!TIP]
> 官方脚本是 `curl -LsSf https://astral.sh/uv/install.sh | sh` ，上述指令使用 `https://gh-proxy.org/` 作为 GitHub 镜像。

## 第二步 配置环境变量

建议在 `.bashrc` 中配置以下环境变量：

```sh
# ===== uv =====
# 把缓存位置设置到原本的家目录下，从而共享缓存。如果不想和其他人共享缓存，可跳过。
export UV_CACHE_DIR="/share/home/u13070/.cache/uv"
export UV_PYTHON_CACHE_DIR="/share/home/u13070/.cache/uv/python"

# uv 自更新镜像； Python 下载镜像； PyPI 包下载镜像。如果不想使用，可跳过。
export UV_INSTALLER_GITHUB_BASE_URL="https://gh-proxy.org/https://github.com/"
export UV_PYTHON_INSTALL_MIRROR="https://gh-proxy.org/https://github.com/astral-sh/python-build-standalone/releases/download"
export UV_DEFAULT_INDEX="https://mirrors.tuna.tsinghua.edu.cn/pypi/web/simple/"    # 这会在 pyproject.toml 中自动添加相应项，如果不想要，可以在 `~/.config/uv/uv.toml` 中配置 index ，而不是使用环境变量。
# ===== uv =====
```

> [!CAUTION]
> 不要试图修改虚拟环境中某个第三方包的代码。
> 
> 1. uv 的缓存功能默认使用硬链接，这可能导致所有人的文件都被修改；
> 2. 在下次更新或同步时，修改可能被抹除。
>
> 如果确实需要修改第三方包，应当从源代码安装它，或者使用 Monkey Patch 。
> 
> 如果担心有其他人这么做，可以随时使用 `uv sync` 或者 `uv sync --reinstall` 恢复。

启动一个新的 `bash` 令上述配置生效。

## 第三步 创建测试项目

接下来让我们试着创建一个项目：

```sh
mkdir ~/uv-test
cd ~/uv-test

uv init my-test-project --python=3.14
cd my-test-project

uv sync
```

## 第四步 运行测试项目

使用 uv 时，一般不使用 `activate` （虽然它也支持），而是直接在命令前面加一个 `uv run` ：

```sh
uv run main.py
```

## 第五步 给测试项目添加依赖

使用 `uv add` 为项目添加依赖：

```sh
uv add csharp-like-file
```

然后试着把 `main.py` 改成：

```python
import csfile
print(csfile.read_all_text(__file__))
```

运行：

```sh
uv run main.py
```

## 第六步 删除测试项目的环境

如果习惯了手动管理 Python 环境，现在切换到 uv 可能会有一丝不安。不过，事实上环境就位于 `.venv` 中：

```sh
ls .venv
```

然而，我们还要进一步强调，环境真的不重要。让我们删掉虚拟环境：

```sh
rm -r .venv
```

随后，直接让它运行：

```sh
uv run main.py
```

它会自动把环境还原。这和 Potery 等其他现代化的包管理器是一样的，它们以 `pyproject.toml` 作为“真理来源”。

相应地，它也会删除未在 `pyproject.toml` 中包含的包。例如，假如用 `pip` 安装一个包：

```sh
uv add pip
uv run pip install variable-declaration-checker --index-url https://mirrors.tuna.tsinghua.edu.cn/pypi/web/simple
```

此时如果我们执行：

```sh
uv sync
```

刚刚的 `variable-declaration-checker` 将会被移除。因为环境和 `pyproject.toml` 不匹配。

由于 `pyproject.toml` 是可以由 git 等版本管理工具追踪的，与它的完全同步，意味着整个环境都是可以进行版本管理、可以稳定复现的。在某些场景下这会非常有帮助。

## 第七步 安装 PyTorch

然而不得不承认，这样的管理对正常的 Python 包没有问题，但是对于 PyTorch 等依赖硬件环境的包事情会变得困难。

### 直接从默认源安装 PyTorch

当然，这其实不是 uv 的问题，而是 PyPI 本身设计时没有考虑如何区分不同 CUDA 版本。

如果本身是使用 `pip install torch` 从默认源安装，那么使用 uv 时没有任何区别：

```sh
uv add torch
```

一般来说，直接从默认源安装 PyTorch 完全够用。但是，如果必须需要一个特定版本的 PyTorch ，例如 `pip install torch --index-url https://mirrors.nju.edu.cn/pytorch/whl/cpu` ，那么问题就变得复杂了。

### 使用特定源安装但不在 `pyproject.toml` 指定（不建议）

如果只是为了把 PyTorch 安装上，仍然可以使用：

```sh
uv add torch --index-url https://mirrors.nju.edu.cn/pytorch/whl/cpu
```

但是这会收到警告：

```text
warning: Indexes specified via `--index-url` will not be persisted to the `pyproject.toml` file; use `--default-index` instead.
```

这意味着，具体 `torch` 应该从什么源下载并没有被记录。在部分情况下， uv 可能会认为当前版本不符合需要，转而从默认 PyPI 源重新下载 `torch` 。（当然，一般情况下 `uv` 会根据 `uv.lock` 试图从同样的源安装包。）

### 在 `pyproject.toml` 指定源

更好的方式是指定下载源。建议直接手动编辑 `pyproject.toml` ，例如：

```toml {hl_lines=["17-27"]}
[project]
name = "my-test-project"
version = "0.1.0"
description = "Add your description here"
readme = "README.md"
requires-python = ">=3.14"
dependencies = [
    "csharp-like-file>=0.0.8",
    "pip>=26.0.1",
    "torch>=2.11.0"
]

[[tool.uv.index]]
url = "https://mirrors.tuna.tsinghua.edu.cn/pypi/web/simple/"
default = true

# 配置一个叫 pytorch-cpu 的源
[[tool.uv.index]]
name = "pytorch-cpu"
url = "https://mirrors.nju.edu.cn/pytorch/whl/cpu"
explicit = true    # 除非指定，否则不使用该源

# 配置包的源
[tool.uv.sources]
torch = [
    { index = "pytorch-cpu" }    # 指定需要从 pytorch-cu126 源安装 torch 
]
```

编辑完后使用 `uv sync` 就会从指定的源安装 PyTorch 了。

### 更多方式

上述方案一般已经足够。不过，还有一些更复杂的情况，例如希望在两个不同的设备上开发，并且它们具有不同 CUDA 版本，需要分别下载对应的版本。这种情况下，可以通过可选依赖解决，具体可参考[ uv 官方文档](https://docs.astral.sh/uv/guides/integration/pytorch/#configuring-accelerators-with-optional-dependencies)。
