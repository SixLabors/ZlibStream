# ZlibStream

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A WIP fork of [zlib.managed](https://github.com/Elskom/zlib.managed) with target framework, API changes (hence fork) and performance improvements.

The goal is to introduce as near-native performance as possible while implementing missing features from Zlib into the codebase.

Targets netstandard1.3+

## Why?

DeflateStream in the .NET framework is a wrapper around the Intel fork of Zlib.
 This fork [sacrifices compression of sparse data](https://github.com/dotnet/runtime/issues/28235) for performance gains which results in [huge differences](https://github.com/SixLabors/ImageSharp/issues/1027) between the output size of certain images on Windows compared to other platforms. By producing a high performance managed implementation we can guarantee excellent cross platform image compression. 

### Current Benchmarks

|             Method | Compression |                  Mean |               Error |              StdDev |                Median | Ratio | RatioSD |    Bytes |     Gen 0 |     Gen 1 | Gen 2 |  Allocated |
|------------------- |------------ |----------------------:|--------------------:|--------------------:|----------------------:|------:|--------:|---------:|----------:|----------:|------:|-----------:|
| SharpZipLibDeflate |           1 |   969,242,100.0000 ns |  15,807,210.1065 ns |  14,012,680.0296 ns |   967,734,750.0000 ns |  1.00 |    0.00 | 16315059 | 3000.0000 | 1000.0000 |     - | 49047200 B |
|   SixLaborsDeflate |           1 |   779,437,193.9394 ns |  14,810,909.8745 ns |  23,491,672.5767 ns |   776,239,000.0000 ns |  0.81 |    0.03 | 16313211 |         - |         - |     - | 33555312 B |
|      DotNetDeflate |           1 |    54,581,923.1250 ns |     775,676.7455 ns |     761,818.2813 ns |    54,392,245.0000 ns |  0.06 |    0.00 |   825050 |         - |         - |     - |  2090328 B |
|        ZLibManaged |           1 | 1,318,257,345.5882 ns |  25,998,348.6614 ns |  62,290,382.9008 ns | 1,346,934,550.0000 ns |  1.27 |    0.04 | 16314795 |         - |         - |     - | 83074816 B |
|                    |             |                       |                     |                     |                       |       |         |          |           |           |       |            |
| SharpZipLibDeflate |           6 |   485,907,420.0000 ns |   2,595,217.1653 ns |   2,427,567.7466 ns |   486,221,000.0000 ns |  1.00 |    0.00 |   553805 |         - |         - |     - |  2866208 B |
|   SixLaborsDeflate |           6 |   361,981,969.2308 ns |   4,485,233.1557 ns |   3,745,372.8222 ns |   360,618,300.0000 ns |  0.74 |    0.01 |   553817 |         - |         - |     - |  2097936 B |
|      DotNetDeflate |           6 |   124,568,429.9107 ns |   1,743,103.2036 ns |   3,752,213.5371 ns |   124,862,025.0000 ns |  0.25 |    0.01 |   742721 |         - |         - |     - |  2090328 B |
|        ZLibManaged |           6 |   509,143,557.1429 ns |   7,191,293.9563 ns |   6,374,894.7809 ns |   506,223,200.0000 ns |  1.05 |    0.02 |   553817 |         - |         - |     - | 51372664 B |
|                    |             |                       |                     |                     |                       |       |         |          |           |           |       |            |
| SharpZipLibDeflate |           9 | 2,927,441,495.0000 ns | 100,974,470.4352 ns | 297,725,504.2964 ns | 2,847,240,750.0000 ns | 1.000 |    0.00 |   553805 |         - |         - |     - |  2864872 B |
|   SixLaborsDeflate |           9 | 2,278,564,593.3333 ns |  18,056,458.1756 ns |  16,890,022.1805 ns | 2,274,972,000.0000 ns | 0.730 |    0.09 |   553817 |         - |         - |     - |  2099296 B |
|      DotNetDeflate |           9 |             0.5439 ns |           0.0197 ns |           0.0242 ns |             0.5363 ns | 0.000 |    0.00 |       -1 |         - |         - |     - |          - |
|        ZLibManaged |           9 | 2,916,993,316.6667 ns |  27,913,133.3542 ns |  21,792,738.4751 ns | 2,918,461,850.0000 ns | 0.954 |    0.11 |   553817 |         - |         - |     - | 51372664 B |

Notes.
- DeflateStream uses a different (and vastly superior) compression for level 1 which we should investigate.
- DeflateStream produces a 34% larger file at compression level 6.
- DeflateStream does not allow compression levels > 6.
