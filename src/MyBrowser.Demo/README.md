# MyBrowser - 多内核浏览器组件

> 基于 CEF (Chromium Embedded Framework) 的多内核浏览器组件，支持 Win7 (CEF 109) 和 Win10+ (CEF 最新版)

## 项目状态

| 组件 | 状态 | 备注 |
|------|------|------|
| CEF 初始化 | ✅ 成功 | cef_initialize 返回 1 |
| CEF 运行时 | ⚠️ 待测 | 等待 Run 测试 |
| 浏览器控件 | 🔄 开发中 | 正在集成 CreateBrowser |
| ModernBrowserControl | 🔄 开发中 | 占位符 → 真实浏览器 |

## 架构

### 项目结构

```
MyBrowser/
├── MyBrowser.sln
├── README.md                    # 本文件
├── src/
│   ├── MyBrowser.Abstractions/      # 接口定义 (IBrowserControl, IBrowserFactory)
│   ├── MyBrowser.Dispatcher/        # 调度器 (OS 检测, 内核选择)
│   ├── MyBrowser.Driver.Legacy/    # CEF 109 驱动 (Win7)
│   ├── MyBrowser.Driver.Modern/      # CEF 最新驱动 (Win10+)
│   ├── MyBrowser.Interop/          # P/Invoke 封装层
│   └── MyBrowser.Demo/             # Avalonia 演示应用
└── Runtimes/
    ├── Legacy/                     # CEF 109 二进制文件
    └── Modern/                    # CEF 最新二进制文件
```

### 驱动说明

| 驱动 | CEF 版本 | 目标 OS |
|------|---------|--------|
| Legacy | 109.1.11 | Windows 7+ |
| Modern | 109.1.11 | Windows 10+ |

### 核心接口

```csharp
// 浏览器工厂
public interface IBrowserFactory
{
    void Initialize(BrowserConfig config);
    object CreateControl();
    bool CreateBrowser(IntPtr parentHwnd, string initialUrl);
    void Shutdown();
}

// 浏览器控件
public interface IBrowserControl
{
    void LoadUrl(string url);
    void GoBack();
    void GoForward();
    void Reload();
    void Stop();
    void ExecuteJavaScript(string script);
    
    string Url { get; }
    string Title { get; }
    bool IsLoading { get; }
    
    event EventHandler<LoadStartEventArgs> LoadStart;
    event EventHandler<LoadEndEventArgs> LoadEnd;
    event EventHandler<TitleChangedEventArgs> TitleChanged;
    event EventHandler<AddressChangedEventArgs> AddressChanged;
}
```

## 开发日志

### 2026-04-30

- CEF 109 P/Invoke 初始化成功 ✅
- 简化 CefClient 实现（使用 IntPtr.Zero 让 CEF 使用内置默认客户端）
- CefBrowserFactory 重写，解决指针 fixed 问题

### 2026-04-29

- 发现 CEF 初始化后崩溃问题（5秒后）
- 原因：CefClient 回调函数指针为空

### 2026-04-27

- 项目初始化
- 使用 chromiumembeddedframework.runtime.win-x64 109.1.11 NuGet 包

## 技术细节

### CEF P/Invoke 结构体

- `CefMainArgs` - 主进程参数
- `CefSettings` - CEF 全局设置 (440 bytes for CEF 109)
- `CefApp` - 应用程序回调
- `CefClient` - 浏览器客户端回调
- `CefWindowInfo` - 窗口信息
- `CefBrowserSettings` - 浏览器设置

### 关键 API

| 函数 | 用途 |
|------|------|
| cef_initialize | 初始化 CEF |
| cef_shutdown | 关闭 CEF |
| cef_browser_host_create_browser | 创建浏览器 (异步) |
| cef_browser_host_create_browser_sync | 创建浏览器 (同步) |

## 下一步

- [ ] 运行 Demo 测试
- [ ] 调用 CreateBrowser(parentHWND, url) 创建真实浏览器
- [ ] 获取 Avalonia 控件的原生 HWND
- [ ] 将 CEF 窗口嵌入 Avalonia 窗口

## 依赖

- Avalonia 11.3.7
- CEF 109 (chromiumembeddedframework.runtime.win-x64 109.1.11)
- .NET 8.0

## 约束

> **零依赖**: 不使用 CefGlue NuGet 包，使用纯 P/Invoke 实现 CEF 绑定

## 运行 Demo

```bash
dotnet run --project src/MyBrowser.Demo/MyBrowser.Demo.csproj
```

或运行编译后的 EXE：
```
src\MyBrowser.Demo\bin\Debug\net8.0\MyBrowser.Demo.exe
```

## 调试

Debug 输出窗口会显示：
```
[CefDispatcher] 步骤1: SetDllDirectory()
[CefFactory.Modern] 正在初始化现代CEF...
[CefRuntime] CEF initialized OK
[ModernBrowserControl] LoadUrl: https://www.google.com
```

## 常见问题

### Q: CEF 初始化失败
A: 检查 Runtimes/Modern/ 目录是否包含 libcef.dll 和所有依赖

### Q: 程序崩溃
A: 查看 Debug 输出窗口的日志，检查 CefClient 回调是否正确设置