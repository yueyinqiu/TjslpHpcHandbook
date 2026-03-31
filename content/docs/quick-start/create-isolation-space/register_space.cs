#! /usr/bin/env -S dotnet run
#:package YueYinqiu.Su.DotnetRunFileUtilities@0.0.3

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using CliWrap;

Console.WriteLine("此脚本可辅助完成隔离空间的创建。如果您不清楚这是什么，可参考 https://tjslp-hpc.yueyinqiu.top/docs/quick-start/create-isolation-space/ 的使用说明。");

Console.Write("您想使用的隔离空间名称：");
var name = Console.ReadLine() ?? "";

#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
if (!Regex.IsMatch(name, "^[a-zA-Z][a-zA-Z0-9_-]{3,}$"))
#pragma warning restore SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
{
    Console.WriteLine("隔离空间名称应当由字母、数字、下划线或减号组成。首位只能是字母，最少四个字符。请重新尝试。");
    return;
}

var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
var space = Path.Join(home, "data", name);
var ssdfs = Path.Join("/", "ssdfs", "datahome", new DirectoryInfo(home).Name, name);
var sshCommand = Path.Join(space, ".ssh-command");
Console.WriteLine($"好的，隔离空间将会被创建在 {space}");
Console.WriteLine($"对应的全闪目录为 {ssdfs}");
Console.WriteLine();
bool skipCreation = Path.Exists(space);
if (skipCreation)
{
    Console.WriteLine($"该隔离空间目录已存在。如果继续，将不会创建新的虚拟空间，而仅将 SSH 指令绑定到 {sshCommand}");
    Console.Write("请确认上述提示，若要继续，请按回车键：");
    Console.ReadLine();
    Console.WriteLine();
}

Console.Write("请输入一项联系方式，以便遇到问题及时联系：");
var contact = Console.ReadLine() ?? "";
Console.WriteLine($"好的，联系方式将记录为 {contact}");
Console.WriteLine();

async static Task<string?> GetFigure(string keyLine)
{
    try
    {
        var result = new StringBuilder();
        await Cli.Wrap("ssh-keygen")
            .WithArguments(["-l", "-f", "/dev/stdin"])
            .WithStandardInputPipe(PipeSource.FromString(keyLine))
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(result))
            .ExecuteAsync();
        return result.ToString().Split()[1];
    }
    catch
    {
        return null;
    }
}

Console.Write("请输入您的 SSH 公钥：");
var key = Console.ReadLine() ?? "";
var figure = await GetFigure(key);
if (figure is null)
{
    Console.WriteLine("无法解析该公钥。请重新尝试。");
    return;
}
Console.WriteLine($"好的，该公钥的指纹为 {figure}");
Console.WriteLine();

var authorizedKeys = Path.Join(home, ".ssh", "authorized_keys");
if (skipCreation)
{
    Console.WriteLine("已跳过隔离空间创建。");
}
else
{
    Console.WriteLine("正在创建隔离空间目录结构……");
    Directory.CreateDirectory(space);

    await File.WriteAllTextAsync(sshCommand, 
        $"""
        #!/bin/bash

        NEW_HOME="{space}"
        NEW_ENV="HOME=$NEW_HOME TERM=$TERM SSH_AUTH_SOCK=$SSH_AUTH_SOCK"

        cd $NEW_HOME

        if [ -z "$SSH_ORIGINAL_COMMAND" ]; then
            exec env -i $NEW_ENV /bin/bash --login
        else
            exec env -i $NEW_ENV /bin/bash --login -c "$SSH_ORIGINAL_COMMAND"
        fi
        """);

    Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
    File.SetUnixFileMode(sshCommand, UnixFileMode.UserRead | UnixFileMode.UserExecute);

    await File.WriteAllTextAsync(Path.Join(space, ".bash_logout"), 
        """
        # ~/.bash_logout
        """);
    await File.WriteAllTextAsync(Path.Join(space, ".bash_profile"), 
        """
        # ~/.bash_profile

        if [ -f ~/.bashrc ]; then
            . ~/.bashrc
        fi

        # User specific environment and startup programs
        """);
    await File.WriteAllTextAsync(Path.Join(space, ".bashrc"), 
        $"""
        # ~/.bashrc

        if [ -f /etc/bashrc ]; then
            . /etc/bashrc
        fi

        if ! [[ "$PATH" =~ "$HOME/.local/bin:$HOME/bin:" ]]
        then
            PATH="$HOME/.local/bin:$HOME/bin:$PATH"
        fi
        export PATH

        # User specific aliases and functions
        export HOME_ORIGINAL="{home}"

        # ===== tmux =====
        # https://tjslp-hpc.yueyinqiu.top/docs/quick-start/create-isolation-space/#tmux-%e4%b8%8d%e5%85%bc%e5%ae%b9
        export TMUX_TMPDIR="$HOME/.tmux/tmp"
        mkdir -p "$TMUX_TMPDIR"
        # ===== tmux =====

        echo "欢迎！如果看到了这条消息，说明已成功配置隔离空间！（可以在 ~/.bashrc 中移除这条提示）"
        """);

    var spaceSsh = Path.Join(space, ".ssh");
    Directory.CreateDirectory(spaceSsh);

    await File.WriteAllTextAsync(Path.Join(spaceSsh, "authorized_keys"), 
        $"""
        # 请注意，本文件位于隔离空间中，不会在登录时起到作用。
        # 若要配置 authorized_keys ，应该使用 {authorizedKeys}
        """);

    await File.WriteAllTextAsync(Path.Join(spaceSsh, "config"), 
        $"""
        # 请注意，本配置默认不会被使用。
        # 详见 https://tjslp-hpc.yueyinqiu.top/docs/quick-start/create-isolation-space/#ssh-%e4%b8%8d%e5%85%bc%e5%ae%b9
        """);

    Directory.CreateDirectory(ssdfs);
    File.CreateSymbolicLink(Path.Join(space, "ssdfs"), ssdfs);
}

Console.WriteLine("正在写入公钥……");
File.WriteAllLines(authorizedKeys, File.ReadAllLines(authorizedKeys).Prepend($"command=\"{sshCommand}\" {key} {contact}"));

Console.WriteLine("已完成。");
Console.WriteLine("您现在可尝试使用该公钥进行登录，以进入隔离空间。");
