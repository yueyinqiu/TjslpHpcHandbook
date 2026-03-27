---
weight: 5000
title: "基于 uv 的 Python 环境"
---

# 基于 uv 的 Python 环境

> [!NOTE]
> Python 真是垃圾语言！配环境都值得单开一篇（甚至一篇未必够）？？但他们都在用，有什么办法！！

## 为什么推荐 uv ？

uv 现在已成为现代化 Python 开发的事实标准，统一了 Python 版本管理、包管理、项目管理等等等等。你可能正在使用 Anaconda 并觉得它还不错，但如果没有什么 C/C++ 的依赖需要处理，你真的应该使用 uv 。（如果真的有 C/C++ 依赖，也可以一起用嘛。）除了它足够现代化之外，其实在这里优先推荐 uv 是有一些其他原因的：

1. 不用担心环境变量问题，只要所在目录正确，执行 `uv run python` 绝对不可能使用其他环境。
2. 可以和其他人共享一套缓存，不需要在新环境重新下载包，多花时间还多占空间。
3. Anaconda 通常会统一管理环境，还要给环境取名字，这是很蠢的事情。而 uv ，你别管环境在哪。

## 第一步 安装 uv

> [!WARNING]
> 在安装之前，请确认你跟随[《创建隔离空间》](./../create-isolation-space/)完成了隔离空间的配置。虽然安装 uv 一般没什么太大的副作用，但它确实会改变 `PATH` 。如果没有必要的理由，你真的不应该影响其他人的配置。

安装 uv 理应很简单，一行命令搞定：

```sh
curl -LsSf https://astral.sh/uv/install.sh | sh
```

> [!TIP]
> 如果你失败了，或许可以看一下[《连接到互联网》](./../connect-to-the-internet/)能否解决问题。


## 第二步 修改缓存位置

为了 uv 的缓存功能可以起到效果，可以在 `.bashrc` 中把缓存位置设置到原本的家目录下，而不是在隔离空间中：

```sh
export UV_CACHE_DIR="/share/home/@hpc-user@/.cache/uv"
export UV_PYTHON_CACHE_DIR="/share/home/@hpc-user@/.cache/uv/python"
```

> [!CAUTION]
> 不要试图修改虚拟环境中某个第三方包的代码。
> 
> 1. uv 的缓存功能默认使用硬链接，你的修改可能导致所有人的文件都被修改；
> 2. 在下次更新或同步时，你的修改可能被抹除。
>
> 如果确实需要修改第三方包，应当从源代码安装它，或者使用 Monkey Patch 。
> 
> 如果你担心有其他人这么做，可以随时使用 `uv sync` 或者 `uv sync --reinstall` 恢复。

> [!TIP]
> 写入 `.bashrc` 后是不会在当前终端生效的，得新开一个 `bash` 才会在其中生效。因此进行下一步之前，先执行一下 `bash` 命令吧。

## 第三步 创建一个测试项目

让我们创建一个测试项目：

```sh
mkdir ~/test
cd ~/test

uv init my-test-project --python=3.14
cd my-test-project

uv sync
```

好吧，就是这么简单。

## 第四步 运行测试项目

使用 uv 时，你不需要 `activate` ，也不用找 Python 解释器在哪里，只需要在命令前面加一个 `uv run` ：

```sh
uv run main.py
```

这正是第一个优势：不需要管现在激活了什么东西，只管 `cd` 到项目目录， `uv run` 就一定在使用正确的环境。

## 第四步 给测试项目添加依赖

现在我们尝试使用 `uv add` 添加一些依赖：

```sh
uv add numpy
uv add csharp-like-file
```

然后可以把 `main.py` 改成：

```python
import csfile

print(csfile.read_all_text(__file__))
```

> [!TIP]
> 不会用命令行修改文件，就改用 Visual Studio Code 。

再跑一下看看：

```sh
uv add main.py
```

可以跑说明确实成功添加依赖了。

## 第六步 现在，把虚拟环境删了

这时候你可能觉得，这个 uv 和 Anaconda 甚至 virtualenv 比，好像也没什么区别。

然而事实上区别很大。让我们来波大的，删掉虚拟环境：

```sh
rm -r .venv
```

然后，让它运行：

```sh
uv run main.py
```

它竟然自个把环境装回来了！这就是现代化的，基于 `pyproject.toml` 进行包管理的威力：“环境？那不重要。你要啥，写到 `pyproject.toml` 里面去就行。”

当然你可能会说，我手动调用个 `requirements.txt` 也没关系。不过，除了它可以配置更复杂的东西之外。例如我们用一个诡异的方式，强制用原本的 `pip` 安装一个包：

```sh
uv add pip
uv run pip install variable-declaration-checker
```

现在我们执行：

```sh
uv sync
```

它会把你乱装进去的 `variable-declaration-checker` 给删了，以保证你的环境和 `pyproject.toml` 配置完全一致。

> [!NOTE]
> 如果你认为这样反而不方便……提升一下审美吧！这种可控的感觉是多么美妙（不是）！这玩意的修改可是能用 git 追踪的，再也不怕有坏人（多半是自己）搞坏我的环境了好不。

## 第七步 上 PyTorch

PyTorch 好装吗？如果你觉得 `pip install torch` 没问题，那它就一样装：

```sh
uv add torch
```