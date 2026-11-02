# FlashPad++

一个 Windows 桌面便签本，改写自经典 **FlashPad** 的使用习惯，但把「一页一个 .txt 文件」改成了**单文件存储**。

原版 FlashPad 每新建一页就在 `Data\` 下面丢一个小 txt，页数一多就是几十上百个文件：同步软件要逐个比对、备份是一堆碎文件、拷到另一台机器还得整个目录拖过去。这个版本把全部页面收进一个 `flashpadx.dat`，同步和备份都只需要面对一个文件。

## 功能

- **左侧页面列表 + 右侧正文**，中间分隔条可拖动调整宽度，列表显示编号和标题
- **单文件存储**：所有页面存在 `flashpadx.dat` 一个文件里，迁移/同步/备份只处理它
- **自动保存**：停笔约 1.2 秒落盘，Ctrl+S 可立即保存；写入采用「先写临时文件再整体替换」，断电不会写坏原文件
- **每日自动备份**：`backup\flashpadx-YYYYMMDD.dat`，保留最近 30 份
- **全局热键呼出**：默认 `Ctrl+Alt+F`；被别的软件占用时自动降级到 `Ctrl+Alt+Q` → `Ctrl+Alt+P` → `Ctrl+Shift+F` → `Ctrl+Alt+W`，状态栏显示实际生效的键位，托盘菜单可手动重试
- **关闭按钮 = 最小化**：右上角 X 只收进托盘，真正退出走托盘右键 → 退出（或 Ctrl+Q）
- **启动定位**：每次启动停在列表最后一项，光标落在文末，打开就能接着写
- **页面管理**：F2 或右键重命名、右键新建/删除、上下移动；Ctrl+N 新建
- **查找**：Ctrl+F，连续按可向下继续找；Ctrl+D 插入当前日期时间
- **外观**：字体、字号、文字颜色、背景颜色、自动换行、窗口置顶，窗口位置和大小自动记忆
- **导入**：可从原版 FlashPad 的 `Data` 目录批量导入已有页面

## 数据格式

`flashpadx.dat` 是 UTF-8（带 BOM）的纯文本，字段用 `\x01` 分隔，页面之间以换行分隔：

```
FLASHPADX 1.0
\x01PAGE\x01{order}\x01{title}\x01{created_ticks}\x01{modified_ticks}\x01{guid}
正文内容（可多行）
\x01PAGE\x01...
```

时间戳是 .NET 的 `DateTime.Ticks`。解析时要求一行里至少切出 7 个字段才算页面头，正文里出现零散的 `\x01` 不会被误判成新页面。

## 编译

不需要装 Visual Studio，也不需要联网。Windows 10/11 自带的 .NET Framework 4.x 就够：

```
build.bat
```

脚本会找 `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe` 编译 `FlashPadPP.cs`，产物是同目录下的 `FlashPad++.exe`；失败时把报错写进 `build.log`。

自己是 C# + WinForms，只依赖 `System`、`System.Drawing`、`System.Windows.Forms`，没有第三方包。图标在编译时用 `/win32icon` 嵌入，所以换图标要重新编译一次。

## 快捷键

| 键 | 功能 |
| --- | --- |
| `Ctrl+N` | 新建页面 |
| `Ctrl+F` | 查找（连续按继续向下找） |
| `Ctrl+D` | 插入当前日期时间 |
| `Ctrl+S` | 立即保存 |
| `F2` | 重命名当前页面 |
| `Ctrl+Alt+F` | 全局呼出（被占用会自动换备选键） |
| `Ctrl+Q` | 退出 |

## 说明

- 仅支持 Windows（WinForms + P/Invoke 的 `RegisterHotKey`）
- 仓库里不含编译产物和个人数据：`.exe`、`flashpadx.dat`、`flashpadx.ini`、`backup\` 都在 `.gitignore` 里，克隆后跑一次 `build.bat` 即可
- 程序不联网、不装服务、不写注册表（开机自启需要自己把快捷方式放进 `shell:startup`）
