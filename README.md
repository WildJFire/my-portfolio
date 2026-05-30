

# Unity Utility (AssetBundle Framework)

Unity 资源管理与热更新框架 - 一个完整的 Unity  AssetBundle 解决方案。

## 概述

Unity Utility 是一个功能强大的 Unity 资源管理框架，提供了完整的 AssetBundle 打包、加载和热更新功能。该框架支持同步/异步加载、依赖管理、热更新下载等功能，适用于 Unity 项目的资源管理需求。

## 核心功能

### 资源加载
- **同步加载**: 支持同步加载资源
- **异步加载**: 支持异步加载资源
- **协程支持**: 提供基于协程的加载方式
- **Async/Await 支持**: 提供基于 C# async/await 的现代化加载方式
- **回调支持**: 传统的回调函数加载方式

### 热更新系统
- **AB 包下载**: 支持增量更新和 AB 包下载
- **版本管理**: 完整的版本号文件管理
- **MD5 校验**: 文件完整性校验
- **HybirdCLR 支持**: 支持代码热更新

### 打包系统
- **可视化配置**: 通过 XML 配置文件管理打包规则
- **多种打包模式**: 支持文件级和目录级打包
- **依赖分析**: 自动分析资源依赖关系
- **多平台支持**: 支持 Windows 等平台

## 项目结构

```
UnityUtility/
├── Assets/
│   ├── Script/
│   │   ├── AssetBundleFramework/
│   │   │   ├── Core/
│   │   │   │   ├── Awaiter/          # async/await 支持
│   │   │   │   ├── Bundle/           # Bundle 管理系统
│   │   │   │   ├── HotUpdate/        # 热更新系统
│   │   │   │   └── Resource/         # 资源加载核心
│   │   │   └── Tool/                 # 工具类
│   │   ├── CommonUtility/            # 通用工具
│   │   ├── Event/                    # 事件系统
│   │   └── UIComponent/              # UI 组件
│   ├── Editor/
│   │   └── AssetBundleFramework/    # 编辑器打包工具
│   ├── Demo/                         # 示例场景
│   └── AssetBundle/                  # 资源文件
└── BuildSetting.xml                  # 打包配置文件
```

## 快速开始

### 初始化

```csharp
// 方式一：回调方式
ResourceManager.Instance.Initialize(platform, getFileCallback, editor, offset);

// 方式二：协程方式
yield return ResourceManager.Instance.Initialize(platform, getFileCallback, editor, offset);

// 方式三：Async/Await 方式
await ResourceManager.Instance.LoadWithAwaiter(url);
```

### 加载资源

```csharp
// 同步加载
IResource resource = ResourceManager.Instance.Load(url, false);
GameObject obj = resource.Instantiate();

// 异步加载
IResource resource = ResourceManager.Instance.Load(url, true);
yield return resource;
GameObject obj = resource.Instantiate();

// 使用回调
ResourceManager.Instance.LoadWithCallback(url, true, callback);
```

### 热更新

```csharp
// 启动热更新
HotUpdateManager.Instance.StartHotUpdate();

// 监听下载进度
HotUpdateManager.Instance.OnOneFileDownload += (progress) => { };
HotUpdateManager.Instance.OnStartDownload += () => { };
HotUpdateManager.Instance.OnEndDownload += () => { };
```

## 示例 Demo

项目提供了多个示例场景：

| 场景 | 描述 |
|------|------|
| Test_Callback | 回调方式加载示例 |
| Test_Coroutine | 协程方式加载示例 |
| Test_Await_Async | Async/Await 方式加载示例 |
| Hot_Update | 热更新功能示例 |
| Progress_Bar | 进度条组件示例 |

## 配置说明

### BuildSetting.xml

```xml
<?xml version="1.0" encoding="utf-8"?>
<BuildSetting ProjectName="YourProject">
    <SuffixList>
        <string>.prefab</string>
        <string>.png</string>
    </SuffixList>
    <BuildRoot>AssetBundle/Windows</BuildRoot>
    <Items>
        <BuildItem BundleType="File" ResourceType="Direct" AssetPath="Assets/AssetBundle/UI" Suffix=".prefab" />
    </Items>
</BuildSetting>
```

### 打包类型

- **EBundleType.File**: 每个资源单独打包
- **EBundleType.Directory**: 目录级打包

### 资源类型

- **EResourceType.Direct**: 直接加载
- **EResourceType.Scene**: 场景资源

## 依赖项

- Unity 2019.4+
- .NET Standard 2.0+
- TextMeshPro (UI)

## 技术支持

如有问题，请提交 Issue 或联系维护者。

## 许可证

本项目仅供学习交流使用。