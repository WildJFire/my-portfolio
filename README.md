

# Unity 资源加载框架 (Unity Utility)

一个基于 Unity 的资源加载与热更新框架，提供多种资源加载方式（协程、回调、async/await），支持 AssetBundle 打包、热更新等功能。

## 特性

- **多种加载方式**：支持协程、回调、async/await 三种异步资源加载模式
- **AssetBundle 管理**：完整的资源包管理机制，支持同步/异步加载
- **热更新系统**：内置热更新管理器，支持增量更新资源
- **依赖管理**：自动处理资源依赖关系
- **编辑器工具**：提供可视化打包配置与构建工具
- **性能分析**：内置性能分析器，用于监控打包流程

## 项目结构

```
UnityUtility/
├── Assets/
│   ├── Script/
│   │   ├── AssetBundleFramework/     # 核心框架
│   │   │   ├── Core/
│   │   │   │   ├── Awaiter/         # async/await 支持
│   │   │   │   ├── Bundle/          # AssetBundle 管理
│   │   │   │   ├── HotUpdate/       # 热更新系统
│   │   │   │   └── Resource/        # 资源加载
│   │   │   ├── Editor/              # 编辑器工具
│   │   │   └── Tool/                # 工具类
│   │   └── CommonUtility/           # 通用工具
│   ├── Demo/                        # 示例场景
│   └── AssetBundle/                 # 资源文件
└── TestResourceServer/              # 测试服务器
```

## 核心模块

### 资源加载 (Resource)

- `IResource` - 资源接口
- `ResourceManager` - 资源管理器
- `AResource` / `Resource` - 同步资源
- `AResourceAsync` / `ResourceAsync` - 异步资源

### AssetBundle 管理 (Bundle)

- `BundleManager` - Bundle 管理器
- `ABundle` - Bundle 基类
- `Bundle` / `BundleAsync` - 同步/异步实现

### 热更新 (HotUpdate)

- `HotUpdateManager` - 热更新管理器
- `ABDownloader` - 下载器

### Awaiter 支持

- `IAwaitable` / `IAwaiter` - awaitable 接口
- `ResourceAwaiter` - 资源 awaiter 实现

## 使用示例

### 1. 协程方式加载

```csharp
private IEnumerator Initialize()
{
    var resource = ResourceManager.Instance.Load("assets/prefab.ab", true);
    yield return resource;
    var gameObject = resource.Instantiate();
}
```

### 2. 回调方式加载

```csharp
ResourceManager.Instance.LoadWithCallback("assets/prefab.ab", true, (resource) => {
    var gameObject = resource.Instantiate();
});
```

### 3. async/await 方式加载

```csharp
private async void Initialize()
{
    var resource = await ResourceManager.Instance.LoadWithAwaiter("assets/prefab.ab");
    var gameObject = resource.Instantiate();
}
```

## 打包配置

框架使用 `BuildSetting.xml` 配置文件定义打包规则：

```xml
<BuildSetting ProjectName="YourProject" SuffixList=".prefab,.fbx">
    <BuildItem BundleType="File" ResourceType="Direct" AssetPath="Assets/Prefabs" Suffix=".prefab" />
    <BuildItem BundleType="Directory" ResourceType="Directory" AssetPath="Assets/Models" />
</BuildSetting>
```

### 打包类型 (EBundleType)

- `File` - 单文件打包
- `Directory` - 目录打包

### 资源类型 (EResourceType)

- `Direct` - 直接引用
- `Directory` - 目录引用

### 构建操作

在 Unity 编辑器中：`Tool > ResourceBuild > Build`

## 热更新使用

```csharp
HotUpdateManager.Instance.StartHotUpdate();
```

热更新流程：
1. 下载版本文件
2. 对比本地与服务器版本
3. 下载需要更新的资源包
4. 更新完成

## 依赖

- Unity 2020.3+
- .NET Standard 2.0+

## 许可证

MIT License