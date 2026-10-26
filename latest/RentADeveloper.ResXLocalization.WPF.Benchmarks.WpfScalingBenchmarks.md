```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.33650/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host] : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Toolchain=InProcessEmitToolchain  InvocationCount=1  IterationCount=5  
IterationTime=100ms  UnrollFactor=1  WarmupCount=3  

```
| Method                  | Categories | ElementCount | Mean         | Error        | StdDev       | Ratio | RatioSD | Gen0      | Gen1      | Allocated   | Alloc Ratio |
|------------------------ |----------- |------------- |-------------:|-------------:|-------------:|------:|--------:|----------:|----------:|------------:|------------:|
| **OpenLocalize**            | **Open**       | **100**          |   **3,216.7 μs** |  **1,086.47 μs** |    **282.15 μs** |  **1.03** |    **0.10** |         **-** |         **-** |   **938.96 KB** |        **2.51** |
| OpenStatic              | Open       | 100          |   3,139.3 μs |  1,215.99 μs |    188.18 μs |  1.00 |    0.07 |         - |         - |   374.35 KB |        1.00 |
|                         |            |              |              |              |              |       |         |           |           |             |             |
| **OpenLocalize**            | **Open**       | **10000**        | **426,991.2 μs** | **56,648.08 μs** | **14,711.33 μs** |  **2.76** |    **0.17** | **5000.0000** | **4000.0000** | **92466.21 KB** |        **2.71** |
| OpenStatic              | Open       | 10000        | 155,085.3 μs | 35,833.45 μs |  9,305.83 μs |  1.00 |    0.08 | 2000.0000 | 1000.0000 | 34071.14 KB |        1.00 |
|                         |            |              |              |              |              |       |         |           |           |             |             |
| **SwitchLocalize**          | **Switch**     | **100**          |     **282.5 μs** |     **50.26 μs** |     **13.05 μs** |  **1.63** |    **0.16** |         **-** |         **-** |    **60.37 KB** |        **5.25** |
| SwitchTranslationSource | Switch     | 100          |     174.4 μs |    109.79 μs |     16.99 μs |  1.01 |    0.13 |         - |         - |    11.51 KB |        1.00 |
|                         |            |              |              |              |              |       |         |           |           |             |             |
| **SwitchLocalize**          | **Switch**     | **10000**        |  **17,205.0 μs** |  **2,716.64 μs** |    **420.40 μs** |  **1.28** |    **0.03** |         **-** |         **-** |  **5242.59 KB** |       **11.02** |
| SwitchTranslationSource | Switch     | 10000        |  13,485.4 μs |  1,278.03 μs |    197.78 μs |  1.00 |    0.02 |         - |         - |   475.73 KB |        1.00 |
