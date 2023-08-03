# A·VFS

A Virtual File System for .NET (6+)

- Straightforward `IFileSystem` interface, making extension easy
- The `VfsPath` struct represents paths in the VFS and simplifies path manipulation
- Basic implementations of `IFileSystem`
  - `PhysicalFileSystem` represents a physical folder
  - `MountFileSystem` combines multiple file systems together using mount points
  - `ReadOnlyFileSystemDecorator` wraps around another file system, allowing only read operations
- Some basic implementations of `IFileSystem` (physical path, read-only decorator, and mount file system)
- Separate package `Avfs.Abstractions` for the abstractions

## Example

```c#
// Setup A VFS
var fileSystem = new MountFileSystem();

var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
var savesFolderPath = Path.Combine(userProfilePath, "MyGameSaves");
fileSystem.Mount(VfsPath.Parse("/saves/"), new PhysicalFileSystem(savesFolderPath));

var gameDataFolderPath = Path.GetFullPath("Content");
fileSystem.Mount(VfsPath.Parse("/data/"), new PhysicalFileSystem(gameDataFolderPath));

// Create directory
if (!fileSystem.Exists(VfsPath.Parse("/data/config/")))
    fileSystem.Create(VfsPath.Parse("/data/config/"));

// Create a file 
fileSystem.Open(VfsPath.Parse("/saves/1.save"), FileMode.Create, FileAccess.Write, FileShare.Write);

// Enumerate over a directory
fileSystem.Enumerate(VfsPath.Parse("/saves/"), "*.save", SearchOption.TopDirectoryOnly, SearchTargets.File);

// Delete a file/directory
fileSystem.Delete(VfsPath.Parse("/saves/1.save"));
```

## Is it for you?

A·VFS is meant for applications/games to collect all their assets, settings, user data, etc. in a VFS, hiding the complexities of different storage locations or differing assets for configuration (e.g. debug vs production).

A·VFS is **not** meant to replace any and all kinds of IO of your application to improve testability. For such scenarios take a look at other more complex VFS libraries out there.

## Installation

Install via [Nuget](https://www.nuget.org/packages/Avfs). No other dependencies apart from the abstractions package :)

## Usage

Create your desired `IFileSystem`(s) and use them. Nothing more to it.

There are some good-to-know (tm) facts about `VfsPath` as it is designed to be more restrictive than normal paths:

1. Directory separator is always a forward slash (`/`), regardless of platform
2. Paths are always absolute and thus start with a directory separator (e.g. `/saves/1`)
3. Paths to directories always end with a directory separator (e.g. `/saves/`)
4. Path to files do not end with a directory separator (e.g. `/saves/1`)
5. Any part of a path (stuff between/after directory separators) must follow these rules:
   1. Only consist of Unicode letters, Unicode digits, underscore (`_`),  hyphen (`-`), or dot (`.`)
   2. The last character must be a Unicode letter or Unicode digit
   3. Dot characters must not follow each other (-> `..` is not allowed)

## Build

Run `build.cmd` or`build.sh`. Will use `Compile` target by default. Other targets include: `Clean`, `Test`, `Pack`, `FullBuild`