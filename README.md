

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

| 类名 | 说明 |
|------|------|
| `IResource` | 资源接口，定义资源操作规范 |
| `ResourceManager` | 资源管理器，提供统一的资源加载入口 |
| `Resource` / `AResource` | 同步资源实现类 |
| `ResourceAsync` / `AResourceAsync` | 异步资源实现类 |

### AssetBundle 管理 (Bundle)

| 类名 | 说明 |
|------|------|
| `BundleManager` | Bundle 管理器，处理 AssetBundle 的加载与卸载 |
| `ABundle` | Bundle 基类，定义同步加载接口 |
| `ABundleAsync` | 异步 Bundle 基类 |
| `Bundle` / `BundleAsync` | 同步/异步实现 |

### 热更新 (HotUpdate)

| 类名 | 说明 |
|------|------|
| `HotUpdateManager` | 热更新管理器，协调整个热更新流程 |
| `ABDownloader` | 下载器，负责下载资源包 |
| `MD5Manager` | MD5 校验管理器 |

### Awaiter 支持

| 类名 | 说明 |
|------|------|
| `IAwaitable` / `IAwaiter` | awaitable 接口定义 |
| `ICriticalAwaiter` | 关键 awaiter 接口 |
| `ResourceAwaiter` | 资源 awaiter 实现，支持 async/await 语法 |

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

### 资源操作

```csharp
// 获取资源
var resource = await ResourceManager.Instance.LoadWithAwaiter("assets/prefab.ab");
GameObject prefab = resource.GetAsset<GameObject>();

// 实例化资源
GameObject instance = resource.Instantiate();

// 卸载资源
ResourceManager.Instance.Unload(resource);
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

| 类型 | 说明 |
|------|------|
| `File` | 单文件打包，每个资源单独打包 |
| `Directory` | 目录打包，整个目录打包为一个资源包 |

### 资源类型 (EResourceType)

| 类型 | 说明 |
|------|------|
| `Direct` | 直接引用，指定具体资源路径 |
| `Directory` | 目录引用，指定整个目录 |

### 构建操作

在 Unity 编辑器菜单中执行：`Tool > ResourceBuild > Build`

构建流程：
1. 收集资源文件
2. 分析依赖关系
3. 生成资源包信息
4. 执行 AssetBundle 打包
5. 生成版本文件

## 热更新使用

```csharp
HotUpdateManager.Instance.StartHotUpdate();
```

热更新流程：
1. 下载服务器版本文件
2. 对比本地与服务器版本
3. 计算需要更新的资源包列表
4. 下载需要更新的资源包
5. 更新完成，切换到新资源

### 版本文件格式

版本文件采用以下格式记录资源信息：
```
资源包名称|MD5值|文件大小
```

## 依赖

- Unity 2020.3+
- .NET Standard 2.0+

## 许可证

MIT License