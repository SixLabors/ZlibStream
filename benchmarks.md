### Sparse Image Data

```
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.202
  [Host]     : .NET Core 5.0.5 (CoreCLR 5.0.521.16609, CoreFX 5.0.521.16609), X64 RyuJIT
  Job-RHQZMH : .NET Core 3.1.14 (CoreCLR 4.700.21.16201, CoreFX 4.700.21.16208), X64 RyuJIT

Runtime=.NET Core 3.1  IterationCount=3  LaunchCount=1
WarmupCount=3
```

|      Method | Compression |        Mean |      Error |    StdDev | Ratio | RatioSD |    Bytes |     Gen 0 |     Gen 1 | Gen 2 | Allocated |
|------------ |------------ |------------:|-----------:|----------:|------:|--------:|---------:|----------:|----------:|------:|----------:|
|   Microsoft |           1 |    77.55 ms |  17.250 ms |  0.946 ms |  1.00 |    0.00 |   825050 |         - |         - |     - |   1.99 MB |
| SharpZipLib |           1 | 1,286.98 ms | 193.791 ms | 10.622 ms | 16.60 |    0.27 | 16315059 | 3000.0000 | 1000.0000 |     - |  46.76 MB |
|   SixLabors |           1 |    36.68 ms |   8.026 ms |  0.440 ms |  0.47 |    0.01 |   825050 |         - |         - |     - |   2.01 MB |
| ZLibManaged |           1 | 1,524.43 ms | 266.712 ms | 14.619 ms | 19.66 |    0.42 | 16314795 |         - |         - |     - |  79.23 MB |
|             |             |             |            |           |       |         |          |           |           |       |           |
|   Microsoft |           3 |    65.21 ms |  18.058 ms |  0.990 ms |  1.00 |    0.00 |   825050 |         - |         - |     - |   1.99 MB |
| SharpZipLib |           3 |   280.53 ms |  84.971 ms |  4.658 ms |  4.30 |    0.10 |   748793 |         - |         - |     - |   2.75 MB |
|   SixLabors |           3 |    36.27 ms |   6.761 ms |  0.371 ms |  0.56 |    0.01 |   825050 |         - |         - |     - |   2.01 MB |
| ZLibManaged |           3 |   313.04 ms |  76.543 ms |  4.196 ms |  4.80 |    0.04 |   748812 |         - |         - |     - |  48.99 MB |
|             |             |             |            |           |       |         |          |           |           |       |           |
|   Microsoft |           6 |   144.00 ms |  26.165 ms |  1.434 ms |  1.00 |    0.00 |   742721 |         - |         - |     - |      2 MB |
| SharpZipLib |           6 |   489.58 ms |  49.421 ms |  2.709 ms |  3.40 |    0.04 |   553805 |         - |         - |     - |   2.73 MB |
|   SixLabors |           6 |   158.54 ms |  60.508 ms |  3.317 ms |  1.10 |    0.03 |   659280 |         - |         - |     - |   2.03 MB |
| ZLibManaged |           6 |   574.05 ms |  79.048 ms |  4.333 ms |  3.99 |    0.05 |   553817 |         - |         - |     - |  48.99 MB |

```
// * Legends *
  Compression : Value of the 'Compression' parameter
  Mean        : Arithmetic mean of all measurements
  Error       : Half of 99.9% confidence interval
  StdDev      : Standard deviation of all measurements
  Ratio       : Mean of the ratio distribution ([Current]/[Baseline])
  RatioSD     : Standard deviation of the ratio distribution ([Current]/[Baseline])
  Bytes       : Output length in bytes.
  Gen 0       : GC Generation 0 collects per 1000 operations
  Gen 1       : GC Generation 1 collects per 1000 operations
  Gen 2       : GC Generation 2 collects per 1000 operations
  Allocated   : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
  1 ms        : 1 Millisecond (0.001 sec)
```

### Corpus 
```
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.202
  [Host]     : .NET Core 5.0.5 (CoreCLR 5.0.521.16609, CoreFX 5.0.521.16609), X64 RyuJIT
  Job-FQJCTL : .NET Core 3.1.14 (CoreCLR 4.700.21.16201, CoreFX 4.700.21.16208), X64 RyuJIT

Runtime=.NET Core 3.1  IterationCount=3  LaunchCount=1
WarmupCount=3
```

|      Method | Compression |         file |         Mean |         Error |       StdDev | Ratio | RatioSD |  Bytes |    Gen 0 |    Gen 1 |    Gen 2 |  Allocated |
|------------ |------------ |------------- |-------------:|--------------:|-------------:|------:|--------:|-------:|---------:|---------:|---------:|-----------:|
|   Microsoft |           1 |  alice29.txt |  1,933.23 us |  1,285.796 us |    70.479 us |  1.00 |    0.00 |  62042 |  27.3438 |   3.9063 |        - |   120.8 KB |
| SharpZipLib |           1 |  alice29.txt |  3,782.15 us |     33.454 us |     1.834 us |  1.96 |    0.07 |  65134 | 101.5625 |  11.7188 |        - |  454.83 KB |
|   SixLabors |           1 |  alice29.txt |  2,776.97 us |    330.149 us |    18.097 us |  1.44 |    0.04 |  63340 |  31.2500 |   3.9063 |        - |  129.27 KB |
| ZLibManaged |           1 |  alice29.txt |  3,985.28 us |  1,225.125 us |    67.153 us |  2.06 |    0.08 |  65136 | 109.3750 |  54.6875 |  23.4375 |  539.61 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           1 | asyoulik.txt |  1,654.31 us |    391.950 us |    21.484 us |  1.00 |    0.00 |  54186 |  29.2969 |   3.9063 |        - |  120.79 KB |
| SharpZipLib |           1 | asyoulik.txt |  3,760.59 us |  1,756.208 us |    96.264 us |  2.27 |    0.04 |  56798 |  85.9375 |  39.0625 |        - |  448.17 KB |
|   SixLabors |           1 | asyoulik.txt |  3,066.04 us |    398.737 us |    21.856 us |  1.85 |    0.02 |  55139 |  31.2500 |   3.9063 |        - |  129.27 KB |
| ZLibManaged |           1 | asyoulik.txt |  3,906.94 us |  2,619.181 us |   143.566 us |  2.36 |    0.09 |  56797 | 113.2813 |  62.5000 |  27.3438 |  513.14 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           1 |      cp.html |    327.66 us |    134.712 us |     7.384 us |  1.00 |    0.00 |   8713 |   5.8594 |        - |        - |   24.71 KB |
| SharpZipLib |           1 |      cp.html |    649.45 us |    282.198 us |    15.468 us |  1.98 |    0.06 |   9032 |  77.1484 |  11.7188 |        - |  346.15 KB |
|   SixLabors |           1 |      cp.html |    374.05 us |    363.677 us |    19.934 us |  1.14 |    0.04 |   8907 |   7.8125 |   0.4883 |        - |   33.15 KB |
| ZLibManaged |           1 |      cp.html |    657.65 us |    277.051 us |    15.186 us |  2.01 |    0.09 |   9034 |  76.1719 |  25.3906 |        - |  319.01 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           1 |     fields.c |    158.05 us |    152.788 us |     8.375 us |  1.00 |    0.00 |   3601 |   2.6855 |        - |        - |   11.24 KB |
| SharpZipLib |           1 |     fields.c |    346.88 us |    293.725 us |    16.100 us |  2.20 |    0.09 |   3653 |  63.4766 |  27.3438 |        - |  321.85 KB |
|   SixLabors |           1 |     fields.c |    187.45 us |     27.710 us |     1.519 us |  1.19 |    0.07 |   3766 |   2.1973 |        - |        - |    8.98 KB |
| ZLibManaged |           1 |     fields.c |    306.78 us |     44.939 us |     2.463 us |  1.94 |    0.11 |   3653 |  61.0352 |  23.9258 |        - |  281.37 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           1 |  grammar.lsp |     43.59 us |     10.147 us |     0.556 us |  1.00 |    0.00 |   1300 |   1.0376 |        - |        - |    4.49 KB |
| SharpZipLib |           1 |  grammar.lsp |    168.85 us |     49.543 us |     2.716 us |  3.87 |    0.03 |   1332 |  61.2793 |  30.5176 |        - |  317.13 KB |
|   SixLabors |           1 |  grammar.lsp |     58.19 us |      1.181 us |     0.065 us |  1.34 |    0.02 |   1334 |   1.1597 |        - |        - |    4.92 KB |
| ZLibManaged |           1 |  grammar.lsp |    251.60 us |     95.553 us |     5.238 us |  5.77 |    0.08 |   1332 |  57.1289 |  28.3203 |        - |  271.85 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           1 |  kennedy.xls |  6,442.25 us |  3,928.622 us |   215.341 us |  1.00 |    0.00 | 238457 | 125.0000 | 101.5625 | 101.5625 |  504.98 KB |
| SharpZipLib |           1 |  kennedy.xls | 16,653.37 us | 10,666.553 us |   584.670 us |  2.59 |    0.15 | 242302 | 125.0000 |  62.5000 |  31.2500 |   949.6 KB |
|   SixLabors |           1 |  kennedy.xls | 10,507.12 us |  1,875.516 us |   102.803 us |  1.63 |    0.06 | 199356 | 109.3750 |  78.1250 |  78.1250 |  514.95 KB |
| ZLibManaged |           1 |  kennedy.xls | 19,166.89 us |  5,761.627 us |   315.814 us |  2.98 |    0.14 | 242299 | 343.7500 | 312.5000 | 281.2500 | 1782.03 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           1 |   lcet10.txt |  5,110.83 us |    637.193 us |    34.927 us |  1.00 |    0.00 | 166210 | 125.0000 | 101.5625 | 101.5625 |  504.98 KB |
| SharpZipLib |           1 |   lcet10.txt | 11,294.31 us |  3,104.544 us |   170.171 us |  2.21 |    0.05 | 174129 | 156.2500 | 140.6250 |  78.1250 |  863.62 KB |
|   SixLabors |           1 |   lcet10.txt |  7,629.67 us |  2,358.007 us |   129.250 us |  1.49 |    0.03 | 167403 | 132.8125 | 101.5625 | 101.5625 |  513.88 KB |
| ZLibManaged |           1 |   lcet10.txt | 11,894.91 us |  1,823.053 us |    99.928 us |  2.33 |    0.01 | 174130 | 281.2500 | 218.7500 | 203.1250 | 1192.36 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           1 | plrabn12.txt |  6,645.07 us |  1,378.206 us |    75.544 us |  1.00 |    0.00 | 216492 | 125.0000 | 101.5625 | 101.5625 |  504.98 KB |
| SharpZipLib |           1 | plrabn12.txt | 14,295.10 us | 16,837.536 us |   922.922 us |  2.15 |    0.16 | 228886 | 171.8750 | 125.0000 |  78.1250 |  874.13 KB |
|   SixLabors |           1 | plrabn12.txt | 10,250.45 us |  1,664.305 us |    91.226 us |  1.54 |    0.01 | 220181 | 109.3750 |  78.1250 |  78.1250 |  514.95 KB |
| ZLibManaged |           1 | plrabn12.txt | 15,638.59 us | 28,645.604 us | 1,570.162 us |  2.36 |    0.26 | 228889 | 296.8750 | 265.6250 | 218.7500 |  1246.5 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           1 |         ptt5 |  2,264.44 us |    373.667 us |    20.482 us |  1.00 |    0.00 |  64424 |  27.3438 |   3.9063 |        - |   120.8 KB |
| SharpZipLib |           1 |         ptt5 |  6,052.17 us |    727.844 us |    39.896 us |  2.67 |    0.01 |  65559 | 109.3750 |  54.6875 |  23.4375 |  590.31 KB |
|   SixLabors |           1 |         ptt5 |  2,767.67 us |    414.013 us |    22.693 us |  1.22 |    0.01 |  67013 |  62.5000 |  31.2500 |  31.2500 |  257.51 KB |
| ZLibManaged |           1 |         ptt5 |  7,639.48 us |  5,765.891 us |   316.048 us |  3.37 |    0.11 |  65559 | 226.5625 | 179.6875 | 148.4375 | 1021.01 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           1 |          sum |    591.40 us |    156.544 us |     8.581 us |  1.00 |    0.00 |  14436 |   5.8594 |        - |        - |   24.71 KB |
| SharpZipLib |           1 |          sum |  1,006.43 us |    594.642 us |    32.594 us |  1.70 |    0.06 |  14118 |  72.2656 |  23.4375 |        - |  351.49 KB |
|   SixLabors |           1 |          sum |    611.20 us |    114.902 us |     6.298 us |  1.03 |    0.00 |  14694 |   7.8125 |        - |        - |   33.22 KB |
| ZLibManaged |           1 |          sum |  1,155.24 us |    644.930 us |    35.351 us |  1.95 |    0.08 |  14118 |  70.3125 |  23.4375 |        - |  332.32 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           1 |      xargs.1 |     75.04 us |     94.566 us |     5.183 us |  1.00 |    0.00 |   1826 |   1.4648 |        - |        - |    6.03 KB |
| SharpZipLib |           1 |      xargs.1 |    227.56 us |     78.952 us |     4.328 us |  3.04 |    0.19 |   1852 |  61.5234 |  30.5176 |        - |  316.88 KB |
|   SixLabors |           1 |      xargs.1 |     83.14 us |    119.811 us |     6.567 us |  1.11 |    0.01 |   1901 |   1.0986 |        - |        - |    4.92 KB |
| ZLibManaged |           1 |      xargs.1 |    201.11 us |     43.205 us |     2.368 us |  2.69 |    0.20 |   1852 |  66.4063 |  22.2168 |        - |  272.85 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           3 |  alice29.txt |  2,095.56 us |    473.668 us |    25.963 us |  1.00 |    0.00 |  62042 |  27.3438 |   3.9063 |        - |  120.81 KB |
| SharpZipLib |           3 |  alice29.txt |  5,820.83 us | 10,659.979 us |   584.309 us |  2.78 |    0.24 |  59495 |  85.9375 |  39.0625 |        - |   448.3 KB |
|   SixLabors |           3 |  alice29.txt |  4,120.52 us |  1,055.485 us |    57.855 us |  1.97 |    0.05 |  60207 |  31.2500 |   7.8125 |        - |  129.81 KB |
| ZLibManaged |           3 |  alice29.txt |  5,529.83 us |    373.740 us |    20.486 us |  2.64 |    0.03 |  59494 | 109.3750 |  54.6875 |  23.4375 |  539.42 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           3 | asyoulik.txt |  1,748.16 us |    497.250 us |    27.256 us |  1.00 |    0.00 |  54186 |  27.3438 |   3.9063 |        - |  120.81 KB |
| SharpZipLib |           3 | asyoulik.txt |  4,539.99 us |  1,473.949 us |    80.792 us |  2.60 |    0.03 |  52669 |  85.9375 |  39.0625 |        - |   448.1 KB |
|   SixLabors |           3 | asyoulik.txt |  3,568.06 us |  2,357.222 us |   129.207 us |  2.04 |    0.10 |  52914 |  31.2500 |   3.9063 |        - |  129.27 KB |
| ZLibManaged |           3 | asyoulik.txt |  5,600.68 us |  3,540.816 us |   194.084 us |  3.20 |    0.07 |  52669 | 101.5625 |  39.0625 |  15.6250 |  513.16 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           3 |      cp.html |    293.61 us |    225.155 us |    12.341 us |  1.00 |    0.00 |   8713 |   5.8594 |        - |        - |   24.71 KB |
| SharpZipLib |           3 |      cp.html |    687.28 us |    323.301 us |    17.721 us |  2.34 |    0.14 |   8622 |  69.3359 |  29.2969 |        - |  346.09 KB |
|   SixLabors |           3 |      cp.html |    410.20 us |     52.043 us |     2.853 us |  1.40 |    0.07 |   8645 |   7.8125 |   0.4883 |        - |   33.15 KB |
| ZLibManaged |           3 |      cp.html |    728.50 us |     97.641 us |     5.352 us |  2.48 |    0.11 |   8622 |  76.1719 |   0.9766 |        - |  319.01 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           3 |     fields.c |    155.93 us |    205.073 us |    11.241 us |  1.00 |    0.00 |   3601 |   2.6855 |        - |        - |   11.24 KB |
| SharpZipLib |           3 |     fields.c |    381.07 us |     86.431 us |     4.738 us |  2.45 |    0.19 |   3399 |  62.5000 |  30.7617 |        - |  321.85 KB |
|   SixLabors |           3 |     fields.c |    220.97 us |    209.814 us |    11.501 us |  1.42 |    0.16 |   3570 |   2.1973 |        - |        - |    8.98 KB |
| ZLibManaged |           3 |     fields.c |    392.47 us |    279.759 us |    15.335 us |  2.52 |    0.17 |   3399 |  62.0117 |  20.5078 |        - |  281.12 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           3 |  grammar.lsp |     52.59 us |     17.366 us |     0.952 us |  1.00 |    0.00 |   1300 |   1.0376 |        - |        - |    4.49 KB |
| SharpZipLib |           3 |  grammar.lsp |    184.36 us |     50.503 us |     2.768 us |  3.51 |    0.11 |   1307 |  61.2793 |  30.5176 |        - |  317.09 KB |
|   SixLabors |           3 |  grammar.lsp |     65.95 us |      5.783 us |     0.317 us |  1.25 |    0.03 |   1316 |   1.0986 |        - |        - |    4.92 KB |
| ZLibManaged |           3 |  grammar.lsp |    195.69 us |     21.067 us |     1.155 us |  3.72 |    0.06 |   1307 |  59.3262 |  23.6816 |        - |  271.83 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           3 |  kennedy.xls |  5,785.62 us |    334.637 us |    18.343 us |  1.00 |    0.00 | 238457 | 125.0000 | 101.5625 | 101.5625 |  504.98 KB |
| SharpZipLib |           3 |  kennedy.xls | 17,192.46 us |  4,279.586 us |   234.579 us |  2.97 |    0.05 | 228231 | 125.0000 |  93.7500 |  31.2500 |  931.87 KB |
|   SixLabors |           3 |  kennedy.xls | 22,094.81 us |  7,760.473 us |   425.378 us |  3.82 |    0.09 | 203717 |  62.5000 |  31.2500 |  31.2500 |  517.11 KB |
| ZLibManaged |           3 |  kennedy.xls | 17,998.17 us |  3,512.849 us |   192.551 us |  3.11 |    0.03 | 228228 | 343.7500 | 312.5000 | 281.2500 | 1782.03 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           3 |   lcet10.txt |  5,138.29 us |    569.796 us |    31.232 us |  1.00 |    0.00 | 166210 | 125.0000 | 101.5625 | 101.5625 |  504.98 KB |
| SharpZipLib |           3 |   lcet10.txt | 14,936.35 us |    678.914 us |    37.214 us |  2.91 |    0.01 | 158788 | 156.2500 | 125.0000 |  78.1250 |  856.18 KB |
|   SixLabors |           3 |   lcet10.txt |  9,139.32 us |  1,867.190 us |   102.347 us |  1.78 |    0.01 | 160050 |  93.7500 |  78.1250 |  78.1250 |  512.77 KB |
| ZLibManaged |           3 |   lcet10.txt | 15,490.66 us |    748.986 us |    41.054 us |  3.01 |    0.01 | 158790 | 281.2500 | 218.7500 | 203.1250 | 1192.31 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           3 | plrabn12.txt |  6,896.80 us |  1,129.074 us |    61.888 us |  1.00 |    0.00 | 216492 | 117.1875 |  93.7500 |  93.7500 |  504.91 KB |
| SharpZipLib |           3 | plrabn12.txt | 20,121.76 us |  1,281.001 us |    70.216 us |  2.92 |    0.02 | 209413 | 125.0000 |  62.5000 |  31.2500 |  866.99 KB |
|   SixLabors |           3 | plrabn12.txt | 14,287.00 us |    868.693 us |    47.616 us |  2.07 |    0.01 | 209933 |  93.7500 |  78.1250 |  78.1250 |  512.77 KB |
| ZLibManaged |           3 | plrabn12.txt | 21,063.35 us |  3,852.506 us |   211.169 us |  3.05 |    0.06 | 209414 | 281.2500 | 218.7500 | 187.5000 | 1246.32 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           3 |         ptt5 |  2,420.69 us |    633.814 us |    34.741 us |  1.00 |    0.00 |  64424 |  27.3438 |   3.9063 |        - |  120.77 KB |
| SharpZipLib |           3 |         ptt5 |  7,398.55 us |    454.399 us |    24.907 us |  3.06 |    0.04 |  62521 |  93.7500 |  31.2500 |        - |   462.1 KB |
|   SixLabors |           3 |         ptt5 |  3,525.19 us |    236.267 us |    12.951 us |  1.46 |    0.02 |  60164 |  27.3438 |   3.9063 |        - |  128.75 KB |
| ZLibManaged |           3 |         ptt5 |  8,024.24 us |    474.519 us |    26.010 us |  3.32 |    0.06 |  62425 | 187.5000 | 140.6250 |  93.7500 |   892.4 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           3 |          sum |    483.33 us |    100.888 us |     5.530 us |  1.00 |    0.00 |  14436 |   5.8594 |        - |        - |    24.7 KB |
| SharpZipLib |           3 |          sum |  1,363.19 us |    226.332 us |    12.406 us |  2.82 |    0.06 |  13801 |  72.2656 |  23.4375 |        - |  351.49 KB |
|   SixLabors |           3 |          sum |    784.95 us |    330.018 us |    18.089 us |  1.62 |    0.06 |  14383 |   7.8125 |        - |        - |   33.09 KB |
| ZLibManaged |           3 |          sum |  1,556.49 us |    424.398 us |    23.263 us |  3.22 |    0.08 |  13741 |  70.3125 |  23.4375 |        - |  332.19 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           3 |      xargs.1 |     68.75 us |     23.934 us |     1.312 us |  1.00 |    0.00 |   1826 |   1.4648 |        - |        - |    6.03 KB |
| SharpZipLib |           3 |      xargs.1 |    224.34 us |    112.085 us |     6.144 us |  3.26 |    0.15 |   1814 |  65.9180 |  21.9727 |        - |  316.91 KB |
|   SixLabors |           3 |      xargs.1 |     94.43 us |     33.904 us |     1.858 us |  1.37 |    0.00 |   1876 |   1.0986 |        - |        - |    4.91 KB |
| ZLibManaged |           3 |      xargs.1 |    230.79 us |    119.116 us |     6.529 us |  3.36 |    0.07 |   1814 |  66.6504 |  21.7285 |        - |  272.81 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           6 |  alice29.txt |  5,509.85 us |  1,099.929 us |    60.291 us |  1.00 |    0.00 |  54930 |  23.4375 |        - |        - |  120.77 KB |
| SharpZipLib |           6 |  alice29.txt | 11,132.77 us |  1,731.752 us |    94.923 us |  2.02 |    0.02 |  54394 |  78.1250 |  15.6250 |        - |   448.4 KB |
|   SixLabors |           6 |  alice29.txt |  6,883.36 us |    973.752 us |    53.375 us |  1.25 |    0.01 |  55818 |  23.4375 |        - |        - |  128.73 KB |
| ZLibManaged |           6 |  alice29.txt | 11,633.98 us |  2,042.308 us |   111.946 us |  2.11 |    0.01 |  54404 |  78.1250 |  15.6250 |        - |  539.42 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           6 | asyoulik.txt |  4,513.07 us |    655.177 us |    35.912 us |  1.00 |    0.00 |  49675 |  23.4375 |        - |        - |  120.77 KB |
| SharpZipLib |           6 | asyoulik.txt |  9,556.94 us |  1,139.027 us |    62.434 us |  2.12 |    0.03 |  48897 |  78.1250 |  15.6250 |        - |  448.15 KB |
|   SixLabors |           6 | asyoulik.txt |  5,494.48 us |    257.628 us |    14.121 us |  1.22 |    0.01 |  50068 |  31.2500 |   7.8125 |        - |  129.86 KB |
| ZLibManaged |           6 | asyoulik.txt | 10,945.30 us | 22,511.993 us | 1,233.958 us |  2.43 |    0.28 |  48897 |  78.1250 |  15.6250 |        - |  513.14 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           6 |      cp.html |    503.64 us |     70.528 us |     3.866 us |  1.00 |    0.00 |   8029 |   5.8594 |        - |        - |   24.21 KB |
| SharpZipLib |           6 |      cp.html |    900.79 us |     65.166 us |     3.572 us |  1.79 |    0.02 |   7950 |  64.4531 |  31.2500 |        - |     330 KB |
|   SixLabors |           6 |      cp.html |    585.20 us |    172.618 us |     9.462 us |  1.16 |    0.01 |   8233 |   7.8125 |        - |        - |   33.09 KB |
| ZLibManaged |           6 |      cp.html |    952.65 us |     66.121 us |     3.624 us |  1.89 |    0.01 |   7961 |  62.5000 |  20.5078 |        - |  302.98 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           6 |     fields.c |    220.33 us |     51.928 us |     2.846 us |  1.00 |    0.00 |   3134 |   2.1973 |        - |        - |    9.87 KB |
| SharpZipLib |           6 |     fields.c |    500.91 us |     74.890 us |     4.105 us |  2.27 |    0.02 |   3121 |  62.5000 |  30.2734 |        - |  321.88 KB |
|   SixLabors |           6 |     fields.c |    262.11 us |     63.018 us |     3.454 us |  1.19 |    0.01 |   3280 |   1.9531 |        - |        - |    8.95 KB |
| ZLibManaged |           6 |     fields.c |    489.91 us |     45.963 us |     2.519 us |  2.22 |    0.02 |   3122 |  61.5234 |  20.5078 |        - |  280.85 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           6 |  grammar.lsp |     79.12 us |      3.645 us |     0.200 us |  1.00 |    0.00 |   1233 |   0.9766 |        - |        - |     4.3 KB |
| SharpZipLib |           6 |  grammar.lsp |    199.30 us |     32.215 us |     1.766 us |  2.52 |    0.02 |   1222 |  65.6738 |  21.9727 |        - |  317.09 KB |
|   SixLabors |           6 |  grammar.lsp |     93.42 us |      2.901 us |     0.159 us |  1.18 |    0.00 |   1251 |   1.0986 |        - |        - |    4.91 KB |
| ZLibManaged |           6 |  grammar.lsp |    251.52 us |     73.268 us |     4.016 us |  3.18 |    0.06 |   1222 |  61.5234 |  18.0664 |        - |  271.74 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           6 |  kennedy.xls | 14,740.28 us |  6,782.653 us |   371.780 us |  1.00 |    0.00 | 220024 |  93.7500 |  78.1250 |  78.1250 |  504.91 KB |
| SharpZipLib |           6 |  kennedy.xls | 54,855.56 us |  9,785.671 us |   536.386 us |  3.72 |    0.12 | 203991 | 100.0000 |        - |        - |  935.62 KB |
|   SixLabors |           6 |  kennedy.xls | 69,952.45 us | 21,800.687 us | 1,194.969 us |  4.75 |    0.12 | 187289 |        - |        - |        - |  512.79 KB |
| ZLibManaged |           6 |  kennedy.xls | 64,310.82 us | 48,627.535 us | 2,665.439 us |  4.36 |    0.15 | 203665 | 125.0000 | 125.0000 | 125.0000 | 1782.02 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           6 |   lcet10.txt | 16,758.56 us |  6,368.885 us |   349.100 us |  1.00 |    0.00 | 146144 |  31.2500 |  31.2500 |  31.2500 |  504.91 KB |
| SharpZipLib |           6 |   lcet10.txt | 34,129.12 us | 20,151.964 us | 1,104.597 us |  2.04 |    0.06 | 144887 |  66.6667 |        - |        - |  856.12 KB |
|   SixLabors |           6 |   lcet10.txt | 18,539.29 us | 22,386.257 us | 1,227.066 us |  1.11 |    0.05 | 147916 |  31.2500 |  31.2500 |  31.2500 |  512.77 KB |
| ZLibManaged |           6 |   lcet10.txt | 31,429.20 us |  7,571.897 us |   415.041 us |  1.88 |    0.03 | 144904 | 125.0000 |  62.5000 |  62.5000 | 1192.19 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           6 | plrabn12.txt | 19,822.12 us |  2,697.798 us |   147.875 us |  1.00 |    0.00 | 198193 |  31.2500 |  31.2500 |  31.2500 |  504.91 KB |
| SharpZipLib |           6 | plrabn12.txt | 43,316.68 us |  5,813.190 us |   318.641 us |  2.19 |    0.00 | 195255 |  83.3333 |        - |        - |  867.47 KB |
|   SixLabors |           6 | plrabn12.txt | 26,382.84 us |  1,822.052 us |    99.873 us |  1.33 |    0.01 | 199026 |  31.2500 |  31.2500 |  31.2500 |  512.79 KB |
| ZLibManaged |           6 | plrabn12.txt | 46,806.00 us |  9,858.513 us |   540.378 us |  2.36 |    0.02 | 195261 |  90.9091 |        - |        - | 1246.67 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           6 |         ptt5 |  5,922.14 us |  1,292.063 us |    70.822 us |  1.00 |    0.00 |  54621 |  23.4375 |        - |        - |  120.77 KB |
| SharpZipLib |           6 |         ptt5 | 15,325.81 us |  1,103.219 us |    60.471 us |  2.59 |    0.04 |  55941 |  93.7500 |  31.2500 |        - |   462.1 KB |
|   SixLabors |           6 |         ptt5 |  9,765.79 us |  1,181.227 us |    64.747 us |  1.65 |    0.02 |  59946 |  15.6250 |        - |        - |  128.73 KB |
| ZLibManaged |           6 |         ptt5 | 16,652.76 us |  1,325.708 us |    72.667 us |  2.81 |    0.02 |  56224 | 156.2500 |  93.7500 |  62.5000 |  892.81 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           6 |          sum |    835.47 us |     62.578 us |     3.430 us |  1.00 |    0.00 |  12968 |   5.8594 |        - |        - |    24.7 KB |
| SharpZipLib |           6 |          sum |  2,308.78 us |    219.613 us |    12.038 us |  2.76 |    0.02 |  12986 |  70.3125 |  19.5313 |        - |  351.49 KB |
|   SixLabors |           6 |          sum |  1,435.98 us |    636.660 us |    34.897 us |  1.72 |    0.04 |  14002 |   7.8125 |        - |        - |   33.09 KB |
| ZLibManaged |           6 |          sum |  3,278.53 us |  1,212.539 us |    66.463 us |  3.92 |    0.09 |  12957 |  70.3125 |  23.4375 |        - |  332.19 KB |
|             |             |              |              |               |              |       |         |        |          |          |          |            |
|   Microsoft |           6 |      xargs.1 |    134.08 us |     61.866 us |     3.391 us |  1.00 |    0.00 |   1747 |   1.2207 |        - |        - |     5.8 KB |
| SharpZipLib |           6 |      xargs.1 |    310.32 us |    164.785 us |     9.032 us |  2.32 |    0.08 |   1736 |  75.1953 |   2.4414 |        - |  316.94 KB |
|   SixLabors |           6 |      xargs.1 |    128.04 us |     75.286 us |     4.127 us |  0.95 |    0.02 |   1828 |   0.9766 |        - |        - |    4.91 KB |
| ZLibManaged |           6 |      xargs.1 |    279.31 us |     28.436 us |     1.559 us |  2.08 |    0.06 |   1736 |  66.4063 |        - |        - |  272.73 KB |

```
// * Legends *
  Compression : Value of the 'Compression' parameter
  file        : Value of the 'file' parameter
  Mean        : Arithmetic mean of all measurements
  Error       : Half of 99.9% confidence interval
  StdDev      : Standard deviation of all measurements
  Ratio       : Mean of the ratio distribution ([Current]/[Baseline])
  RatioSD     : Standard deviation of the ratio distribution ([Current]/[Baseline])
  Bytes       : Output length in bytes.
  Gen 0       : GC Generation 0 collects per 1000 operations
  Gen 1       : GC Generation 1 collects per 1000 operations
  Gen 2       : GC Generation 2 collects per 1000 operations
  Allocated   : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
  1 us        : 1 Microsecond (0.000001 sec)
```