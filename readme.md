<h1 align="center">

<img src="https://github.com/SixLabors/Branding/raw/main/icons/org/sixlabors.svg?sanitize=true" alt="SixLabors.ImageSharp" width="256"/>
<br/>
SixLabors.ZlibStream
</h1>

<div align="center">

[![License: Apache 2.0](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
</div>

A WIP fork of [zlib.managed](https://github.com/Elskom/zlib.managed) with target framework, API changes (hence fork) and performance improvements.

The goal is to introduce as near-native performance as possible while implementing missing features from Zlib into the codebase.

Targets netstandard1.3+

## Why?

DeflateStream in the .NET framework is a wrapper around the Intel fork of Zlib.
 This fork [sacrifices compression of sparse data](https://github.com/dotnet/runtime/issues/28235) for performance gains which results in [huge differences](https://github.com/SixLabors/ImageSharp/issues/1027) between the output size of certain images on Windows compared to other platforms. By producing a high performance managed implementation we can guarantee excellent cross platform image compression. 
 
## Building the Project

- Using [Visual Studio 2019](https://visualstudio.microsoft.com/vs/)
  - Make sure you have the latest version installed
  - Make sure you have [the .NET 5 SDK](https://www.microsoft.com/net/core#windows) installed

Alternatively, you can work from command line and/or with a lightweight editor on **both Linux/Unix and Windows**:

- [Visual Studio Code](https://code.visualstudio.com/) with [C# Extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp)
- [.NET Core](https://www.microsoft.com/net/core#linuxubuntu)

To clone ZlibStream locally, click the "Clone in [YOUR_OS]" button above or run the following git commands:

```bash
git clone https://github.com/SixLabors/ZlibStream
```

This repository contains [git submodules](https://blog.github.com/2016-02-01-working-with-submodules/). To add the submodules to the project, navigate to the repository root and type:

``` bash
git submodule update --init --recursive
```

### Benchmarks

Benchmarks against the Canterbury corpus, a collection of files intended for use as a benchmark for testing lossless data compression algorithms can be found [here](benchmarks.md).

