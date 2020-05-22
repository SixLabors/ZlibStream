<h1 align="center">

<img src="https://github.com/SixLabors/Branding/raw/master/icons/org/sixlabors.svg?sanitize=true" alt="SixLabors.ImageSharp" width="256"/>
<br/>
SixLabors.ZlibStream
</h1>

<div align="center">

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
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
  - Make sure you have [the .NET Core 3.1 SDK](https://www.microsoft.com/net/core#windows) installed

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

### Current Benchmarks

The following benchmark was designed to highlight the difference in compression of sparse data.
  
|             Method | Compression |                  Mean |              Error |              StdDev |                Median | Ratio | RatioSD |    Bytes |     Gen 0 |     Gen 1 | Gen 2 |  Allocated |
|------------------- |------------ |----------------------:|-------------------:|--------------------:|----------------------:|------:|--------:|---------:|----------:|----------:|------:|-----------:|
| SharpZipLibDeflate |           1 | 1,021,612,566.1765 ns | 20,200,814.4331 ns |  48,399,861.1732 ns | 1,037,427,350.0000 ns |  1.00 |    0.00 | 16315059 | 3000.0000 | 1000.0000 |     - | 49035656 B |
|   SixLaborsDeflate |           1 |   131,608,033.3333 ns |  1,033,111.2241 ns |     966,372.8800 ns |   131,561,325.0000 ns |  0.13 |    0.01 |   825050 |         - |         - |     - |  2097928 B |
|      DotNetDeflate |           1 |    53,836,300.0000 ns |    311,248.4785 ns |     259,906.5760 ns |    53,878,788.8889 ns |  0.05 |    0.00 |   825050 |         - |         - |     - |  2090486 B |
|        ZLibManaged |           1 | 1,213,970,405.0000 ns | 22,988,640.1437 ns |  26,473,752.1755 ns | 1,213,816,750.0000 ns |  1.22 |    0.09 | 16314795 |         - |         - |     - | 83079576 B |
|                    |             |                       |                    |                     |                       |       |         |          |           |           |       |            |
| SharpZipLibDeflate |           6 |   454,864,940.0000 ns |  8,395,919.5257 ns |   7,853,548.3335 ns |   455,084,600.0000 ns |  1.00 |    0.00 |   553805 |         - |         - |     - |  2864872 B |
|   SixLaborsDeflate |           6 |   287,681,200.0000 ns |    997,969.1038 ns |     884,673.6165 ns |   287,792,350.0000 ns |  0.63 |    0.01 |   659280 |         - |         - |     - |  2098608 B |
|      DotNetDeflate |           6 |   124,441,155.7692 ns |    436,466.0863 ns |     364,468.9497 ns |   124,335,950.0000 ns |  0.27 |    0.00 |   742721 |         - |         - |     - |  2090756 B |
|        ZLibManaged |           6 |   491,527,754.5455 ns |  8,499,167.4191 ns |  10,437,740.8637 ns |   487,268,900.0000 ns |  1.09 |    0.04 |   553817 |         - |         - |     - | 51372664 B |
|                    |             |                       |                    |                     |                       |       |         |          |           |           |       |            |
| SharpZipLibDeflate |           9 | 2,706,881,235.7143 ns | 39,566,104.7482 ns |  35,074,321.2825 ns | 2,692,407,600.0000 ns | 1.000 |    0.00 |   553805 |         - |         - |     - |  2864872 B |
|   SixLaborsDeflate |           9 |   269,531,600.0000 ns |  2,136,103.6604 ns |   1,783,743.3011 ns |   268,900,600.0000 ns | 0.100 |    0.00 |   659280 |         - |         - |     - |  2097928 B |
|      DotNetDeflate |           9 |             0.4450 ns |          0.0042 ns |           0.0039 ns |             0.4428 ns | 0.000 |    0.00 |       -1 |         - |         - |     - |          - |
|        ZLibManaged |           9 | 3,015,820,337.7551 ns | 92,630,177.8609 ns | 270,206,576.6429 ns | 2,897,383,800.0000 ns | 1.046 |    0.03 |   553817 |         - |         - |     - | 51372664 B |

