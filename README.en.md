# Unity Utility (AssetBundle Framework)

Unity Resource Management and Hot Update Framework – A complete Unity AssetBundle solution.

## Overview

Unity Utility is a powerful Unity resource management framework that provides comprehensive AssetBundle packaging, loading, and hot update functionality. The framework supports synchronous/asynchronous loading, dependency management, hot update downloads, and more, meeting the resource management needs of Unity projects.

## Core Features

### Resource Loading
- **Synchronous Loading**: Supports synchronous resource loading
- **Asynchronous Loading**: Supports asynchronous resource loading
- **Coroutine Support**: Provides coroutine-based loading methods
- **Async/Await Support**: Offers modern C# async/await-based loading
- **Callback Support**: Traditional callback-based loading approach

### Hot Update System
- **AB Package Download**: Supports incremental updates and AB package downloads
- **Version Management**: Complete version file management
- **MD5 Verification**: File integrity checking
- **HybridCLR Support**: Supports code hot updates

### Packaging System
- **Visual Configuration**: Manages packaging rules via XML configuration files
- **Multiple Packaging Modes**: Supports both file-level and directory-level packaging
- **Dependency Analysis**: Automatically analyzes resource dependencies
- **Multi-Platform Support**: Supports platforms such as Windows

## Project Structure

```
UnityUtility/
├── Assets/
│   ├── Script/
│   │   ├── AssetBundleFramework/
│   │   │   ├── Core/
│   │   │   │   ├── Awaiter/          # async/await support
│   │   │   │   ├── Bundle/           # Bundle management system
│   │   │   │   ├── HotUpdate/        # Hot update system
│   │   │   │   └── Resource/         # Core resource loading
│   │   │   └── Tool/                 # Utility classes
│   │   ├── CommonUtility/            # General utilities
│   │   ├── Event/                    # Event system
│   │   └── UIComponent/              # UI components
│   ├── Editor/
│   │   └── AssetBundleFramework/    # Editor packaging tools
│   ├── Demo/                         # Sample scenes
│   └── AssetBundle/                  # Resource files
└── BuildSetting.xml                  # Packaging configuration file
```

## Quick Start

### Initialization

```csharp
// Method 1: Callback approach
ResourceManager.Instance.Initialize(platform, getFileCallback, editor, offset);

// Method 2: Coroutine approach
yield return ResourceManager.Instance.Initialize(platform, getFileCallback, editor, offset);

// Method 3: Async/Await approach
await ResourceManager.Instance.LoadWithAwaiter(url);
```

### Loading Resources

```csharp
// Synchronous loading
IResource resource = ResourceManager.Instance.Load(url, false);
GameObject obj = resource.Instantiate();

// Asynchronous loading
IResource resource = ResourceManager.Instance.Load(url, true);
yield return resource;
GameObject obj = resource.Instantiate();

// Using callback
ResourceManager.Instance.LoadWithCallback(url, true, callback);
```

### Hot Update

```csharp
// Start hot update
HotUpdateManager.Instance.StartHotUpdate();

// Listen to download progress
HotUpdateManager.Instance.OnOneFileDownload += (progress) => { };
HotUpdateManager.Instance.OnStartDownload += () => { };
HotUpdateManager.Instance.OnEndDownload += () => { };
```

## Example Demos

The project includes multiple sample scenes:

| Scene | Description |
|-------|-------------|
| Test_Callback | Example using callback-based loading |
| Test_Coroutine | Example using coroutine-based loading |
| Test_Await_Async | Example using async/await-based loading |
| Hot_Update | Example demonstrating hot update functionality |
| Progress_Bar | Example showcasing progress bar component |

## Configuration Details

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

### Packaging Types

- **EBundleType.File**: Each resource packaged individually
- **EBundleType.Directory**: Directory-level packaging

### Resource Types

- **EResourceType.Direct**: Direct loading
- **EResourceType.Scene**: Scene resource

## Dependencies

- Unity 2019.4+
- .NET Standard 2.0+
- TextMeshPro (UI)

## Technical Support

For issues, please submit an Issue or contact the maintainer.

## License

This project is intended solely for learning and communication purposes.