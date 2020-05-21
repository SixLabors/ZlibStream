# ZlibStream

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A WIP fork of [zlib.managed](https://github.com/Elskom/zlib.managed) with target framework, API changes (hence fork) and performance improvements.

The goal is to introduce as near-native performance as possible while implementing missing features from Zlib into the codebase.

Targets netstandard1.3+

## Why?

DeflateStream in the .NET framework is a wrapper around the Intel fork of Zlib.
 This fork [sacrifices compression of sparse data](https://github.com/dotnet/runtime/issues/28235) for performance gains which results in [huge differences](https://github.com/SixLabors/ImageSharp/issues/1027) between the output size of certain images on Windows compared to other platforms. By producing a high performance managed implementation we can guarantee excellent cross platform image compression. 

### Current Benchmarks

The following benchmark was designed to highlight the difference in compression of sparse data.
  
Notes.
- DeflateStream uses a different (and vastly superior) compression for level 1 which we should investigate. It is unknown how compression level 2 performs using that implementation as it is not possible to configure.
- DeflateStream produces a 34% larger file at compression level 6.
- DeflateStream does not allow compression levels > 6. No improvement by other libraries for compression level 9.

|             Method | Compression |                  Mean |               Error |              StdDev |                Median | Ratio | RatioSD |    Bytes |     Gen 0 |     Gen 1 | Gen 2 |  Allocated |
|------------------- |------------ |----------------------:|--------------------:|--------------------:|----------------------:|------:|--------:|---------:|----------:|----------:|------:|-----------:|
| SharpZipLibDeflate |           1 |   948,804,438.4615 ns |  15,288,318.3073 ns |  12,766,438.1980 ns |   945,567,200.0000 ns |  1.00 |    0.00 | 16315059 | 3000.0000 | 1000.0000 |     - | 49035656 B |
|   SixLaborsDeflate |           1 |   763,000,940.0000 ns |  15,120,368.6884 ns |  24,843,199.3665 ns |   771,921,000.0000 ns |  0.79 |    0.04 | 16314795 |         - |         - |     - | 33555312 B |
|      DotNetDeflate |           1 |    58,979,298.3333 ns |     617,958.3530 ns |     578,038.6268 ns |    59,031,400.0000 ns |  0.06 |    0.00 |   825050 |         - |         - |     - |  2090579 B |
|        ZLibManaged |           1 | 1,300,861,753.3333 ns |  13,766,846.9279 ns |  12,877,517.1579 ns | 1,296,915,200.0000 ns |  1.37 |    0.02 | 16314795 |         - |         - |     - | 83076152 B |
|                    |             |                       |                     |                     |                       |       |         |          |           |           |       |            |
| SharpZipLibDeflate |           6 |   448,527,580.0000 ns |   4,104,952.1749 ns |   3,839,774.8113 ns |   450,048,400.0000 ns |  1.00 |    0.00 |   553805 |         - |         - |     - |  2864872 B |
|   SixLaborsDeflate |           6 |   335,074,064.5161 ns |   6,407,390.9173 ns |  14,592,871.4925 ns |   328,221,200.0000 ns |  0.77 |    0.04 |   553817 |         - |         - |     - |  2097936 B |
|      DotNetDeflate |           6 |   121,943,424.8000 ns |     652,739.5048 ns |     871,388.4903 ns |   121,880,020.0000 ns |  0.27 |    0.00 |   742721 |         - |         - |     - |  2090328 B |
|        ZLibManaged |           6 |   482,885,114.2857 ns |   3,337,426.9603 ns |   2,958,542.0705 ns |   482,562,550.0000 ns |  1.08 |    0.01 |   553817 |         - |         - |     - | 51372664 B |
|                    |             |                       |                     |                     |                       |       |         |          |           |           |       |            |
| SharpZipLibDeflate |           9 | 3,408,726,524.0000 ns | 103,764,690.4266 ns | 305,952,530.8947 ns | 3,452,724,400.0000 ns | 1.000 |    0.00 |   553805 |         - |         - |     - |  2864872 B |
|   SixLaborsDeflate |           9 | 2,678,341,764.5833 ns |  65,592,033.2611 ns | 189,248,013.3052 ns | 2,775,386,100.0000 ns | 0.795 |    0.10 |   553817 |         - |         - |     - |  2097936 B |
|      DotNetDeflate |           9 |             0.5426 ns |           0.0154 ns |           0.0121 ns |             0.5400 ns | 0.000 |    0.00 |       -1 |         - |         - |     - |          - |
|        ZLibManaged |           9 | 3,273,357,383.3333 ns |  16,249,629.1646 ns |  12,686,641.5966 ns | 3,269,930,200.0000 ns | 1.075 |    0.16 |   553817 |         - |         - |     - | 51372664 B |

