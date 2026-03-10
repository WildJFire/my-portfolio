# UnityUtility

Unity Resource Management Framework - AssetBundle Packaging, Loading, and Hot Update Solution

## Project Overview

UnityUtility is a comprehensive Unity resource management framework that provides support for AssetBundle packaging, loading, hot updates, and multiple asynchronous loading modes. The framework is designed to be simple yet feature-rich, making it suitable for resource management needs across various Unity projects.

## Core Features

### 1. AssetBundle Management
- Synchronous/Asynchronous resource loading
- Automatic dependency management
- Reference counting and automatic cleanup
- Editor mode support (test without packaging during development)

### 2. Multiple Asynchronous Loading Modes
- **Coroutine Mode**: Traditional coroutine-based asynchronous loading
- **Callback Mode**: Callback-based asynchronous loading
- **async/await Mode**: Modern C# asynchronous programming support

### 3. Hot Update System
- Version detection and comparison
- Incremental update support
- MD5 checksum verification
- Download progress tracking
- Multi-threaded download management

### 4. Editor Build Tools
- Visual build configuration
- Support for multiple packaging strategies
- Flexible resource type configuration
- Build performance analysis

## Project Structure

```
UnityUtility/
├── Assets/
│   ├── Script/
│   │   ├── AssetBundleFramework/
│   │   │   ├── Core/
│   │   │   │   ├── Awaiter/       # async/await support
│   │   │   │   ├── Bundle/         # AssetBundle core management
│   │   │   │   ├── HotUpdate/      # Hot update module
│   │   │   │   └── Resource/       # Resource loading core
│   │   │   ├── Editor/             # Editor build tools
│   │   │   └── Tool/               # Utility classes
│   │   ├── CommonUtility/          # Common utility classes
│   │   └── UIComponent/            # UI components
│   ├── Demo/                       # Example scenes
│   └── AssetBundle/                # Source asset files
├── AssetBundle/                    # Build output directory
├── TestResourceServer/             # Test resource server
└── BuildSetting.xml                # Build configuration file
```

## Quick Start

### 1. Requirements
- Unity 2020.3 or higher
- .NET Standard 2.0+

### 2. Build AssetBundles

1. Configure the `BuildSetting.xml` file
2. In the Unity Editor, go to: `Tool -> ResourceBuild -> Build`

### 3. Resource Loading

```csharp
// Coroutine mode
IEnumerator LoadResource()
{
    var resource = ResourceManager.Instance.Load("assets/assetbundle/ui/testui.prefab.ab", true);
    yield return resource;
    var prefab = resource.GetAsset<GameObject>();
}

// Callback mode
ResourceManager.Instance.LoadWithCallback("assets/assetbundle/ui/testui.prefab.ab", true, (resource) =>
{
    var prefab = resource.GetAsset<GameObject>();
});

// async/await mode
async Task LoadResourceAsync()
{
    var resource = await ResourceManager.Instance.LoadWithAwaiter("assets/assetbundle/ui/testui.prefab.ab");
    var prefab = resource.GetAsset<GameObject>();
}
```

### 4. Hot Update

```csharp
HotUpdateManager.Instance.StartHotUpdate();
```

## Example Scenes

The project includes multiple demo scenes located in the `Assets/Demo/` directory:

| Scene | Description |
|-------|-------------|
| TestUI | UI resource loading test |
| Test_Coroutine | Coroutine asynchronous loading example |
| Test_Callback | Callback asynchronous loading example |
| Test_Await_Async | async/await asynchronous loading example |
| Hot_Update | Hot update functionality demo |
| Progress_Bar | Progress bar component demo |

## Framework Components

### Core Classes

- **ResourceManager**: Resource manager responsible for loading and unloading resources
- **BundleManager**: AssetBundle manager handling bundle loading and dependencies
- **HotUpdateManager**: Hot update manager handling version checks and resource downloads
- **ABVersionItem**: Version information item storing AB package version data

### Common Utilities

- **Singleton<T>**: Singleton base class
- **MonoSingleton<T>**: MonoBehaviour singleton base class
- **Profiler**: Performance analysis tool
- **IOUtils**: File operation utilities

### UI Components

- **ProgressBar**: Progress bar component supporting dynamic value updates

## License

This project is intended solely for learning and communication purposes.