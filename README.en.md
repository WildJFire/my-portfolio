# Unity Resource Loading Framework (Unity Utility)

A resource loading and hot-update framework based on Unity, supporting multiple resource loading methods (coroutines, callbacks, async/await), and features such as AssetBundle packaging and hot updates.

## Features

- **Multiple Loading Methods**: Supports three asynchronous resource loading modes: coroutines, callbacks, and async/await
- **AssetBundle Management**: Complete asset bundle management system with support for synchronous and asynchronous loading
- **Hot Update System**: Built-in hot update manager supporting incremental resource updates
- **Dependency Management**: Automatically handles resource dependencies
- **Editor Tools**: Provides visual configuration and build tools
- **Performance Analysis**: Built-in performance analyzer to monitor the packaging process

## Project Structure

```
UnityUtility/
├── Assets/
│   ├── Script/
│   │   ├── AssetBundleFramework/     # Core framework
│   │   │   ├── Core/
│   │   │   │   ├── Awaiter/         # async/await support
│   │   │   │   ├── Bundle/          # AssetBundle management
│   │   │   │   ├── HotUpdate/       # Hot update system
│   │   │   │   └── Resource/        # Resource loading
│   │   │   ├── Editor/              # Editor tools
│   │   │   └── Tool/                # Utility classes
│   │   └── CommonUtility/           # General utilities
│   ├── Demo/                        # Demo scenes
│   └── AssetBundle/                 # Resource files
└── TestResourceServer/              # Test server
```

## Core Modules

### Resource Loading (Resource)

- `IResource` - Resource interface
- `ResourceManager` - Resource manager
- `AResource` / `Resource` - Synchronous resource
- `AResourceAsync` / `ResourceAsync` - Asynchronous resource

### AssetBundle Management (Bundle)

- `BundleManager` - Bundle manager
- `ABundle` - Bundle base class
- `Bundle` / `BundleAsync` - Synchronous/asynchronous implementations

### Hot Update (HotUpdate)

- `HotUpdateManager` - Hot update manager
- `ABDownloader` - Downloader

### Awaiter Support

- `IAwaitable` / `IAwaiter` - Awaitable interface
- `ResourceAwaiter` - Resource awaiter implementation

## Usage Examples

### 1. Loading with Coroutines

```csharp
private IEnumerator Initialize()
{
    var resource = ResourceManager.Instance.Load("assets/prefab.ab", true);
    yield return resource;
    var gameObject = resource.Instantiate();
}
```

### 2. Loading with Callbacks

```csharp
ResourceManager.Instance.LoadWithCallback("assets/prefab.ab", true, (resource) => {
    var gameObject = resource.Instantiate();
});
```

### 3. Loading with async/await

```csharp
private async void Initialize()
{
    var resource = await ResourceManager.Instance.LoadWithAwaiter("assets/prefab.ab");
    var gameObject = resource.Instantiate();
}
```

## Packaging Configuration

The framework uses a `BuildSetting.xml` configuration file to define packaging rules:

```xml
<BuildSetting ProjectName="YourProject" SuffixList=".prefab,.fbx">
    <BuildItem BundleType="File" ResourceType="Direct" AssetPath="Assets/Prefabs" Suffix=".prefab" />
    <BuildItem BundleType="Directory" ResourceType="Directory" AssetPath="Assets/Models" />
</BuildSetting>
```

### Bundle Types (EBundleType)

- `File` - Single file packaging
- `Directory` - Directory packaging

### Resource Types (EResourceType)

- `Direct` - Direct reference
- `Directory` - Directory reference

### Build Operations

In the Unity Editor: `Tool > ResourceBuild > Build`

## Hot Update Usage

```csharp
HotUpdateManager.Instance.StartHotUpdate();
```

Hot update workflow:
1. Download version file
2. Compare local and server versions
3. Download required resource bundles
4. Complete update

## Dependencies

- Unity 2020.3+
- .NET Standard 2.0+

## License

MIT License