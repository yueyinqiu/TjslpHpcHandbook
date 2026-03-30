---
weight: 6000
title: "基于 Slurm 的资源管理"
---

# 基于 Slurm 的资源管理

超算平台基于 Slurm 进行资源管理。我们需要通过 Slurm 申请资源，以进入计算节点运行程序。

## 第一步 了解基本概念

- 节点 Node ：集群中的一台或多台设备。使用 `sinfo --Node` 可以列出所有计算节点及其状态；
- 分区 Partition ：一组节点。在我们的超算平台上，有 intel 、 amd 、 L40 和 A800 等分区。通常一个分区内的设备类型是一致的，通过将任务提交到指定分区，就可以指定计算使用的设备类型。使用 `sinfo --summarize` 可查看分区状态；
- 任务 Task ：启动的进程。在提交作业时我们通常会指定节点数量、每个节点的任务数量，以及每个任务需要多少资源等。注意此任务本身不是一种资源限制，而是使 `srun` 启动多个相同进程，通常用于 MPI 并行计算等场景。如果在程序内部调用 `fork` ，如使用 Python 的 `multiprocessing` ，仍然可以创建更多进程（但是 `fork` 无法跨节点创建进程）；
- 作业 Job ： Slurm 管理任务的单位。

## 第二步 尝试提交一个作业

找一个合适的位置，创建 `test.sh` ：

```sh
#!/bin/bash

# 作业名称
#SBATCH --job-name=test

# 输出文件，注意目录必须已存在
#SBATCH --output=%x.out

# 使用的分区
#SBATCH --partition=intel

# 节点数，这里不指定任务数，默认一个节点一个任务
#SBATCH --nodes=2

# 这会在一个节点上执行
hostname
# 而使用 srun 会根据任务数执行多次
srun hostname
```

> [!WARNING]
> 这是需要付费的。注意不要为了测试而写太复杂的脚本。

执行：

```sh
sbatch test.sh
```

应当会看到类似 `Submitted batch job 1323895` 的提示。

在作业执行完毕后，应当可以看到 `test.out` 文件，其内容类似于：

```text
cpui123
cpui130
cpui123
```

> [!TIP]
> 如果输出文件一直没有出现，可以使用 `sacct` 查看作业状态。

## 第三步 常用参数

作业配置：

- `--job-name=<jobname>` 指定作业名称；
- `--comment=<string>` 指定备注内容；
- `--time=<time>` 指定最大运行时长，格式支持 `days-hours:minutes:seconds` ，超算平台允许配置的最大时长为 7 天；
- `--output=<filename_pattern>` 指定输出文件名称，其中可使用占位符，常见的如 `%x` 表示作业名称、 `%j` 表示作业 ID ；
- `--error=<filename_pattern>` 指定错误文件名称；
- `--mail-type=<type>` 指定在何时发送通知邮件，常见的如 `BEGIN` 、 `END` 、 `FAIL` 、 `ALL` 等；
- `--mail-user=<user>` 指定把通知邮件发送到哪个邮箱；

分区和资源配置：

- `--partition=<partition_names>` 指定分区，可指定多个分区（逗号分隔），可使用 `sinfo --summarize` 查看分区状态；
- `--exclude=<node_name_list>` 指定不使用某些节点，在节点故障时可能有帮助；
- `--gres=<list>` 这是一个通用指令，在我们超算平台的 GPU 分区上，必须用这个配置总的 GPU 数量；
- `--nodes=<minnodes>[-maxnodes]|<size_string>` 指定总的节点数量；
- `--ntasks-per-node=<ntasks>` 指定单个节点的任务数量；
- `--cpus-per-task=<ncpus>` 指定单个任务的 CPU 数量；
- `--gpus-per-task=[type:]<number>` 指定单个任务的 GPU 数量；
- `--mem-per-cpu=<size>[units]` 指定单个 CPU 分配内存量，默认单位是兆字节；
- `--mem-per-gpu=<size>[units]` 指定单个 GPU 分配内存量， `mem-per-gpu` 和 `mem-per-cpu` 只能指定一个。

配置的详细信息，以及其他更多配置可见 [Slurm 官方文档](https://slurm.schedmd.com/sbatch.html)。

## 第四步 常用命令

- `squeue` 查看尚未完成的作业；
- `scontrol show job <作业 ID>` 查看一个作业的详细信息；
- `scancel <作业 ID>` 取消一个作业；
- `sacct` 查看历史作业；
- `sinfo` 查看集群状态;
- `scontrol show partition <分区>` 查看一个分区的详细信息；
- `scontrol show node <节点>` 查看一个节点的详细信息。

## 第五步 使用 L40 训练模型

首先找一个合适的位置创建项目：

```sh
uv init slurm-test --python=3.14
cd slurm-test
```

编辑 `pyproject.toml` ：

```toml
[project]
name = "slurm-test"
version = "0.1.0"
description = "Add your description here"
readme = "README.md"
requires-python = ">=3.14"
dependencies = [
    "torch>=2.11.0",
]

[[tool.uv.index]]
url = "https://mirrors.tuna.tsinghua.edu.cn/pypi/web/simple/"
default = true

[[tool.uv.index]]
name = "pytorch-cu128"
url = "https://mirrors.nju.edu.cn/pytorch/whl/cu128"
explicit = true

[tool.uv.sources]
torch = [
    { index = "pytorch-cu128" }
]
```

运行：

```sh
uv sync
```

编辑 `main.py` ：

```python
import torch
import torch.nn as nn

model = nn.Linear(10, 1).cuda()

x = torch.randn(64, 10).cuda()
y = torch.randn(64, 1).cuda()

criterion = nn.MSELoss()
optimizer = torch.optim.SGD(model.parameters(), lr=0.01)

print(f"Using device: {torch.cuda.get_device_name(0)}")

for epoch in range(2):
    loss = criterion(model(x), y)
    loss.backward()
    optimizer.step()
    print(f"Epoch {epoch}, Loss: {loss.item():.4f}")
```

> [!WARNING]
> 这是需要付费的。注意不要为了测试而写太复杂的脚本。

添加一个 `slurm.sh` ：

```sh
#!/bin/bash

#SBATCH --job-name=slurm-test
#SBATCH --partition=L40

#SBATCH --gres=gpu:l40:1

#SBATCH --nodes=1
#SBATCH --ntasks-per-node=1
#SBATCH --gpus-per-task=l40:1
#SBATCH --cpus-per-task=7

#SBATCH --output=%j_%x.out
#SBATCH --error=%j_%x.err

module load cuda/12.8
# 相当于：
# export PATH="/share/apps/cuda-12.8/bin:$PATH"
# export LD_LIBRARY_PATH="/share/apps/cuda-12.8/lib64:$LD_LIBRARY_PATH"

uv run main.py
```

执行：

```sh
sbatch slurm.sh
```

运行结束后，输出文件应该类似：

```text
Using device: NVIDIA L40
Epoch 0, Loss: 1.4207
Epoch 1, Loss: 1.3896
```

错误文件则类似：

```text
/share/home/u13070/data/yueyinqiu/slurm-test/.venv/lib/python3.14/site-packages/torch/_subclasses/functional_tensor.py:307: UserWarning: Failed to initialize NumPy: No module named 'numpy' (Triggered internally at /pytorch/torch/csrc/utils/tensor_numpy.cpp:84.)
  cpu = _conversion_method_template(device=torch.device("cpu"))
```

## 第六步 更多内容

更多内容可参考：
- [同济超算平台快速入门](https://dev.tongji.edu.cn/hpc-doc/#/pages/quickStart/concept)
- [Slurm 官方文档](https://slurm.schedmd.com/)