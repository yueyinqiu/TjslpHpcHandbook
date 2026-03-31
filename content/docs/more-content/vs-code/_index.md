---
weight: 2000
title: "VS Code"
---

# VS Code

VS Code 目前仍然是远程开发的最佳选择。

## 使用 Remote - SSH 扩展连接到超算平台

> [!CAUTION]
> 请确保已参考[《创建隔离空间》](./../create-isolation-space/)完成隔离空间的创建。

首先确保已安装 VS Code 及 Remote - SSH 扩展。

在 VS Code 侧边栏找到 Remote Explorer 。

如果已经根据[《创建隔离空间》](./../../quick-start/create-isolation-space/)在 SSH 配置文件添加了超算平台，这里会显示有相应的条目。点击右侧箭头或者窗口图标即可连接。

成功连接后，在远端安装所需的扩展，这里推荐：
- Python ：用以 Python 开发；
- Even Better TOML ：用以编辑 TOML 文件，例如 `pyproject.toml` ；
- Rainbow CSV ：用以查看 CSV 文件；
- Markdown All in One ：用以编辑 Markdown 文件；
- C# ：用以 C# 开发。

## 获取最优的 Python 开发体验

> [!TIP]
> 下述过程对应的源码已上传到 [https://github.com/yueyinqiu/TjslpHpcHandbook-VsCodeSample.git](https://github.com/yueyinqiu/TjslpHpcHandbook-VsCodeSample.git) 。

### 第一步 创建项目

使用 `uv` 的 `--lib` 模板创建一个项目：

```sh
uv init sample-project --lib --python=3.14
cd sample-project
uv sync
```

创建 `sample-project.workspace` 文件：

```json
{
	"folders": [
		{
			"path": "."
		}
	]
}
```

使用 VS Code 打开这个 Workspace 。此时应该会看到 VS Code 自动选择了 `sample-project` 环境，无需手动配置。

创建或修改 `.gitignore` ：

```text
/.vscode/

# Python-generated files
__pycache__/
*.py[oc]
build/
dist/
wheels/
*.egg-info

# Virtual environments
.venv
```

### 第二步 准备一个可执行脚本

创建 `src/exe/main.py` 文件：

```python
# 这不是通过路径引用的，而是 uv 已经把 sample-project 包安装到环境中。
# 因此不需要配置任何 PYTHON_PATH 等环境变量，也不会碰到找不到模块的问题。
#（注意 exe 本身不在包内。应当把代码写在 sample_project 中， exe 只负责启动。）
import sample_project
output = sample_project.hello()
print(output)
```

把 `src/sample_project/__init__.py` 修改为以下内容，以便测试：

```python
def hello() -> str:
    import os
    import shutil
    
    return (f"cpu: {os.sched_getaffinity(0)}\n"
            f"nvcc: {shutil.which("nvcc")}\n"
            f"nvidia-smi: {shutil.which("nvidia-smi")}")
```

可以尝试运行，确认是否配置正确：

```sh
uv run src/exe/main.py
```

在登陆节点上，它可能输出：

```text
cpu: {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95}
nvcc: None
nvidia-smi: None
```

### 第三步 软连接输入输出目录

有一件事情需要注意：在超算平台上操作文件，特别是文件数量较多的时候，速度是很慢的。为了缓解此问题，平台在 `/ssdfs` 提供了空间相对较小，但速度更快一些的高速储存。在训练模型时，如果需要频繁读写文件，应当把它们先复制到这个高速储存中。

> [!WARNING]
> 高速缓存的价格高很多。在使用完毕后应当及时把文件移除。

> [!TIP]
> 在[《创建隔离空间》](./../../quick-start/create-isolation-space/)中，脚本会自动创建 `~/ssdfs` 软连接到该高速储存。因此使用时直接访问 `~/ssdfs` 即可。

为了避免在程序中硬编码路径，以及方便在 VS Code 中浏览文件，最好在项目中另外维护一套软连接。

首先创建 `links` 文件夹以及 `links/.gitignore` 文件：

```text
*

!/.gitignore
```

然后在高速储存中创建一个输出文件夹，名字可以任取：

```sh
mkdir ~/ssdfs/outputs-of-sample-project
```

把它链接到 `links/outputs` ：

```sh
ln -s ~/ssdfs/outputs-of-sample-project links/outputs
```

注意，如果其中文件数量很多，应当配置 VS Code 的 File Watcher 忽略该目录，否则会耗尽系统的文件监听资源：

```json {hl_lines=["2-10"]}
{
	"settings": {
		"files.watcherExclude": {
			"**/.git/objects/**": true,
			"**/.git/subtree-cache/**": true,
			"**/.hg/store/**": true,
			"links": true,
            ".vscode/slurm": true
		}
	},
	"folders": [
		{
			"path": "."
		}
	]
}
```

### 第四步 使用 Task 功能提交 Slurm 作业

首先创建 `.vscode/slurm/slurm_submit.cs` ，它会生成脚本并调用 `sbatch` 提交作业：

```csharp
#:package YueYinqiu.Su.DotnetRunFileUtilities@0.0.3

using CliWrap;

// 约定命令行参数
var pythonScript = new FileInfo(args[0]);    // 要执行的脚本路径
var partition = args[1];    // 要使用的分区
var count = int.Parse(args[2]);    // 使用的 CPU 或者 GPU 数量（具体是 CPU 还是 GPU 按照分区判断）

var projectName = "sample_projct";
var sbatchScript = 
    $"""
    #!/bin/bash
    cd "{Environment.CurrentDirectory}"    # 这行不是必要的，一般来说会继承当前环境变量
    module load cuda/12.8
    uv run "{pythonScript.FullName}"
    """;
var cpusPerGpu = 7;
string? email = null;

var partitionDictionary = new Dictionary<string, string?>()
{
    { "intel", null },
    { "amd", null },
    { "L40", "l40" },
    { "A800", "a800" },
};
var partitionGpu = partitionDictionary[partition];

var scriptName = Path.GetFileNameWithoutExtension(pythonScript.Name);

var outputPath = new DirectoryInfo(Path.Join(
    Environment.CurrentDirectory,
    "links", "outputs", scriptName,
    DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff")));
outputPath.Create();

var sbatchScriptPath = Path.Join(outputPath.FullName, $"{scriptName}.sh");
File.WriteAllText(sbatchScriptPath, sbatchScript);

var arguments = new Dictionary<string, string?>
{
    // { "exclude", ... },

    { "partition", partition },
    { "nodes", "1" },
    { "ntasks-per-node", "1" },
    { "gres", partitionGpu is null ? null : $"gpu:{partitionGpu}:{count}" },
    { "gpus-per-task", partitionGpu is null ? null : $"{partitionGpu}:{count}" },
    { "cpus-per-task", partitionGpu is null ? $"{count}" : $"{count * cpusPerGpu}" },
    // { "mem-per-cpu", ... },
    // { "mem-per-gpu", ... },

    { "job-name", $"{projectName}_{scriptName}" },
    { "comment", pythonScript.FullName },
    { "output", Path.Join(outputPath.FullName, "%j.out") },
    { "error", Path.Join(outputPath.FullName, "%j.err") },
    { "mail-type", "ALL" },
    { "mail-user", email },
};

var command = Cli.Wrap("sbatch").WithArguments(arguments
    .Where(x => x.Value != null)
    .SelectMany(x => new[] { $"--{x.Key}", $"{x.Value}" })
    .Append(sbatchScriptPath));
await (command | (Console.WriteLine, Console.Error.WriteLine)).ExecuteAsync();
```

> [!TIP]
> 执行该脚本需要 .NET 10 或更高版本，可参考[《 .NET 》](./../dnet/)进行安装。

然后创建 `.vscode/tasks.json` ，为上述脚本配置不同的参数：

```json
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "Intel (x2): Current File",
            "type": "shell",
            "command": "dotnet",
            "args": [
                "run", "${workspaceFolder}/.vscode/slurm/slurm_submit.cs", "${file}",
                "intel", "2"
            ],
            "problemMatcher": []
        },
        {
            "label": "L40 (x1): Current File",
            "type": "shell",
            "command": "dotnet",
            "args": [
                "run", "${workspaceFolder}/.vscode/slurm/slurm_submit.cs", "${file}",
                "L40", "1"
            ],
            "problemMatcher": []
        },
    ]
}
```

完成后，把 VS Code 当前文件切换到 `src/exe/main.py` 。随后，展开菜单栏的 Terminal ，点击 Run Task ，就可以看到 `Intel (x2): Current File` 和 `L40 (x1): Current File` 两个选项。点击其中一个就会在相应分区提交作业。

输出文件位于 `links/outputs/main` 。正常情况下， `Intel (x2): Current File` 上会输出：

```text
cpu: {32, 33}
nvcc: /share/apps/cuda-12.8/bin/nvcc
nvidia-smi: None
```

而在 `L40 (x1): Current File` 上：

```text
cpu: {32, 33, 34, 3, 4, 5, 48}
nvcc: /share/apps/cuda-12.8/bin/nvcc
nvidia-smi: /usr/bin/nvidia-smi
```

### 第五步 调试计算节点上的程序

由于 VS Code 一般在登录节点使用，而程序是在计算节点运行，因此调试变得较为复杂。我们需要在计算节点启动调试器，然后在登录节点附加到它。

首先安装 `debugpy` ，以便在计算节点启动调试器：

```sh
uv add debugpy --dev
```

接着，添加 `.vscode/slurm/slurm_submit.cs` ，这个脚本参考[超算平台文档](https://dev.tongji.edu.cn/hpc-doc/#/pages/software/vscode?id=%e8%ae%a1%e7%ae%97%e8%8a%82%e7%82%b9-1)写就，使用 `salloc` 分配资源，自动生成 `launch.json` ，并使用 SSH 连接到计算节点启动调试器：

```csharp
#:package YueYinqiu.Su.DotnetRunFileUtilities@0.0.3

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using CliWrap;

// 约定命令行参数
var pythonScript = new FileInfo(args[0]);    // 要执行的脚本路径
var partition = args[1];    // 要使用的分区
var count = int.Parse(args[2]);    // 使用的 CPU 或者 GPU 数量（具体是 CPU 还是 GPU 按照分区判断）

var projectName = "sample_projct";
var cpusPerGpu = 7;

var port = Random.Shared.Next(10000, 65536);
var sshScript = 
    $"""
    #!/bin/bash
    cd "{Environment.CurrentDirectory}"    # 这行是必要的，因为后面是使用 SSH 连接到节点。如果不使用 uv ，可能需要加载一些其他环境。
    module load cuda/12.8

    echo "服务即将启动，请按 F5 开始远程调试"
    echo "================================="
    uv run python -X frozen_modules=off -m debugpy --listen 0.0.0.0:{port} --wait-for-client "{pythonScript.FullName}"
    echo "================================="
    """;

var partitionDictionary = new Dictionary<string, string?>()
{
    { "intel", null },
    { "amd", null },
    { "L40", "l40" },
    { "A800", "a800" },
};
var partitionGpu = partitionDictionary[partition];

var arguments = new Dictionary<string, string?>
{
    // { "exclude", ... },

    { "partition", partition },
    { "nodes", "1" },
    { "ntasks-per-node", "1" },
    { "gres", partitionGpu is null ? null : $"gpu:{partitionGpu}:{count}" },
    { "gpus-per-task", partitionGpu is null ? null : $"{partitionGpu}:{count}" },
    { "cpus-per-task", partitionGpu is null ? $"{count}" : $"{count * cpusPerGpu}" },
    // { "mem-per-cpu", ... },
    // { "mem-per-gpu", ... },

    { "job-name", $"{projectName}_{Path.GetFileNameWithoutExtension(pythonScript.Name)}" },
    { "comment", pythonScript.FullName },
};

Console.WriteLine("正在申请资源……");
var sallocOutputBuilder = new StringBuilder();
var sallocCommand = Cli.Wrap("salloc").WithArguments(arguments
    .Where(x => x.Value != null)
    .SelectMany(x => new[] { $"--{x.Key}", $"{x.Value}" })
    .Append("--no-shell"));
sallocCommand = sallocCommand.WithStandardOutputPipe(PipeTarget.Merge(
    PipeTarget.ToDelegate(Console.WriteLine), 
    PipeTarget.ToStringBuilder(sallocOutputBuilder)));
sallocCommand = sallocCommand.WithStandardErrorPipe(PipeTarget.Merge(
    PipeTarget.ToDelegate(Console.Error.WriteLine), 
    PipeTarget.ToStringBuilder(sallocOutputBuilder)));
await sallocCommand.ExecuteAsync();
var sallocOutput = sallocOutputBuilder.ToString();

#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
var match = Regex.Match(sallocOutput, @"Granted job allocation (\d+)");
#pragma warning restore SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.

if (!match.Success)
{
    Console.WriteLine("未能成功解析作业 ID 。程序将退出。");
    Console.WriteLine("请注意！ salloc 指示成功但未发现作业 ID 。您可能需要手动取消作业！");
    return;
}

string jobId = match.Groups[1].Value;
try
{
    using var cancellation = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cancellation.Cancel();
    };

    Console.WriteLine("正在解析节点名称……");
#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
    match = Regex.Match(sallocOutput, @"Nodes\s+(.+)\s+are ready for job");
#pragma warning restore SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
    if (!match.Success)
    {
        Console.WriteLine("未能成功解析节点名称。程序将退出。");
        return;
    }
    string nodeName = match.Groups[1].Value;

    if (cancellation.IsCancellationRequested)
    {
        Console.WriteLine("已取消。");
        return;
    }
    
    var scontrolOutput = new StringBuilder();
    var scontrol = await (Cli.Wrap("scontrol")
        .WithArguments(["show", "hostnames", nodeName]) | 
        scontrolOutput).ExecuteAsync(cancellation.Token);
    var possibleHosts = scontrolOutput.ToString().Split(
        ['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
    if (possibleHosts.Length != 1)
    {
        Console.WriteLine("未能成功解析节点名称。程序将退出。");
        return;
    }
    var host = possibleHosts[0];

    Console.WriteLine("正在写入 launch.json ……");
    await File.WriteAllTextAsync(".vscode/launch.json", 
        $$"""
        {
            "version": "0.2.0",
            "configurations": [
                {
                    "name": "Python Debugger: {{host}}:{{port}}",
                    "type": "debugpy",
                    "request": "attach",
                    "connect": {
                        "host": "{{host}}",
                        "port": {{port}}
                    }
                }
            ]
        }
        """, cancellation.Token);

    var sshScriptFileDirectory = new DirectoryInfo(".vscode/slurm/temp");
    sshScriptFileDirectory.Create();
    var sshScriptFile = Path.Join(sshScriptFileDirectory.FullName, $"ssh_script_{port}.sh");
    Console.WriteLine($"正在写入 {sshScriptFile} ……");
    await File.WriteAllTextAsync(sshScriptFile, sshScript, cancellation.Token);
    Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
    File.SetUnixFileMode(sshScriptFile, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

    Console.WriteLine("正在启动……");
    var sshCommand = Cli.Wrap("ssh")
        .WithArguments([
            "-o", "StrictHostKeyChecking=no", 
            host, 
            sshScriptFile
        ]) | (Console.WriteLine, Console.Error.WriteLine);
    await sshCommand.ExecuteAsync(cancellation.Token);
}
finally
{
    try
    {
        await Cli.Wrap("scancel").WithArguments([jobId]).ExecuteAsync();
        Console.WriteLine("已结束作业。");
    }
    catch
    {
        Console.WriteLine("请注意！作业取消失败。您可能需要手动取消作业！");
    }
}
```

随后在 `.vscode/tasks.json` 添加一个新的 Task ：

```json {hl_lines=["24-33"]}
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "Intel (x2): Current File",
            "type": "shell",
            "command": "dotnet",
            "args": [
                "run", "${workspaceFolder}/.vscode/slurm/slurm_submit.cs", "${file}",
                "intel", "2"
            ],
            "problemMatcher": []
        },
        {
            "label": "L40 (x1): Current File",
            "type": "shell",
            "command": "dotnet",
            "args": [
                "run", "${workspaceFolder}/.vscode/slurm/slurm_submit.cs", "${file}",
                "L40", "1"
            ],
            "problemMatcher": []
        },
        {
            "label": "DEBUG Intel (x1): Current File",
            "type": "shell",
            "command": "dotnet",
            "args": [
                "run", "${workspaceFolder}/.vscode/slurm/slurm_submit_debug.cs", "${file}",
                "intel", "1"
            ],
            "problemMatcher": []
        },
    ]
}
```

完成后，把 VS Code 当前文件切换到 `src/exe/main.py` 。展开菜单栏的 Terminal ，点击 Run Task ，点击 `DEBUG Intel (x1): Current File` 选项。

此时应当看到类似这样的内容：

```text
 *  Executing task in folder sample-project: dotnet run /share/home/u13070/data/yueyinqiu/TjslpHpcHandbook-VsCodeSample/sample-project/.vscode/slurm/slurm_submit_debug.cs /share/home/u13070/data/yueyinqiu/TjslpHpcHandbook-VsCodeSample/sample-project/src/exe/main.py intel 1 

正在申请资源……
salloc: Pending job allocation 1326330
salloc: job 1326330 queued and waiting for resources
salloc: job 1326330 has been allocated resources
salloc: Granted job allocation 1326330
salloc: Waiting for resource configuration
salloc: Nodes cpui138 are ready for job
正在解析节点名称……
正在写入 launch.json ……
正在写入 /share/home/u13070/data/yueyinqiu/TjslpHpcHandbook-VsCodeSample/sample-project/.vscode/slurm/temp/ssh_script_50565.sh ……
正在启动……
服务即将启动，请按 F5 开始远程调试
=================================
```

此时在 VS Code 中展开菜单栏 Run ，点击 Start Debugging ，即可开始附加到该调试器进行调试。

> [!CAUTION]
> 正常退出时，作业会自动关闭。如果过程中出现异常， `.vscode/slurm/slurm_submit.cs` 会试图执行 `scancel` 以结束作业。但是，在部分情况下可能会失败，最好再手动检查一次。
