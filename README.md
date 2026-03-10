

# UnityUtility

Unity 资源管理框架 - AssetBundle 打包、加载与热更新解决方案

## 项目简介

UnityUtility 是一个完整的 Unity 资源管理框架，提供了 AssetBundle 打包、加载、热更新以及多种异步加载模式的支持。该框架设计简洁、功能完善，适用于各类 Unity 项目的资源管理需求。

## 核心功能

### 1. AssetBundle 管理
- 同步/异步资源加载
- 依赖关系自动管理
- 引用计数与自动释放
- 编辑器模式支持（开发阶段无需打包即可测试）

### 2. 多种异步加载模式
- **协程模式 (Coroutine)**：传统的协程异步加载
- **回调模式 (Callback)**：基于回调的异步加载
- **async/await 模式**：现代 C# 异步编程支持

### 3. 热更新系统
- 版本检测与比对
- 增量更新支持
- MD5 校验
- 下载进度跟踪
- 多线程下载管理

### 4. 编辑器构建工具
- 可视化构建配置
- 多种打包策略支持
- 灵活的资源类型配置
- 构建性能分析

## 项目结构

```
UnityUtility/
├── Assets/
│   ├── Script/
│   │   ├── AssetBundleFramework/
│   │   │   ├── Core/
│   │   │   │   ├── Awaiter/       # async/await 支持
│   │   │   │   ├── Bundle/         # AssetBundle 核心管理
│   │   │   │   ├── HotUpdate/      # 热更新模块
│   │   │   │   └── Resource/       # 资源加载核心
│   │   │   ├── Editor/             # 编辑器构建工具
│   │   │   └── Tool/               # 工具类
│   │   ├── CommonUtility/          # 公共工具类
│   │   └── UIComponent/            # UI 组件
│   ├── Demo/                       # 示例场景
│   └── AssetBundle/                # 资源源文件
├── AssetBundle/                    # 构建输出目录
├── TestResourceServer/             # 测试资源服务器
└── BuildSetting.xml                # 构建配置文件
```

## 快速开始

### 1. 环境要求
- Unity 2020.3 或更高版本
- .NET Standard 2.0+

### 2. 构建 AssetBundle

1. 配置 `BuildSetting.xml` 文件
2. 在 Unity 编辑器中执行：`Tool -> ResourceBuild -> Build`

### 3. 资源加载

```csharp
// 协程模式
IEnumerator LoadResource()
{
    var resource = ResourceManager.Instance.Load("assets/assetbundle/ui/testui.prefab.ab", true);
    yield return resource;
    var prefab = resource.GetAsset<GameObject>();
}

// 回调模式
ResourceManager.Instance.LoadWithCallback("assets/assetbundle/ui/testui.prefab.ab", true, (resource) =>
{
    var prefab = resource.GetAsset<GameObject>();
});

// async/await 模式
async Task LoadResourceAsync()
{
    var resource = await ResourceManager.Instance.LoadWithAwaiter("assets/assetbundle/ui/testui.prefab.ab");
    var prefab = resource.GetAsset<GameObject>();
}
```

### 4. 热更新

```csharp
HotUpdateManager.Instance.StartHotUpdate();
```

## 示例场景

项目提供了多个演示场景，位于 `Assets/Demo/` 目录下：

| 场景 | 说明 |
|------|------|
| TestUI | UI 资源加载测试 |
| Test_Coroutine | 协程异步加载示例 |
| Test_Callback | 回调异步加载示例 |
| Test_Await_Async | async/await 异步加载示例 |
| Hot_Update | 热更新功能演示 |
| Progress_Bar | 进度条组件演示 |

## 框架组件

### 核心类

- **ResourceManager**：资源管理器，负责资源的加载与释放
- **BundleManager**：AssetBundle 管理器，处理 Bundle 的加载与依赖
- **HotUpdateManager**：热更新管理器，处理版本检测与资源下载
- **ABVersionItem**：版本信息项，存储 AB 包版本数据

### 公共工具

- **Singleton<T>**：单例基类
- **MonoSingleton<T>**：MonoBehaviour 单例基类
- **Profiler**：性能分析工具
- **IOUtils**：文件操作工具

### UI 组件

- **ProgressBar**：进度条组件，支持动态数值变化

## 许可证

本项目仅供学习交流使用。

##开发计划
- **代码热更**：后续会使用HybirdCLR做代码热更
- **对象池**：后续会制作泛型对象池
- **时间系统**：后续会制作多线程时间系统