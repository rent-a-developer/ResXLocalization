```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.33650/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host] : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Toolchain=InProcessEmitToolchain  InvocationCount=1  IterationCount=5  
IterationTime=100ms  UnrollFactor=1  WarmupCount=3  

```
| Method                  | Categories | ElementCount | Mean         | Error      | StdDev      | Ratio | RatioSD | Gen0       | Gen1      | Allocated | Alloc Ratio |
|------------------------ |----------- |------------- |-------------:|-----------:|------------:|------:|--------:|-----------:|----------:|----------:|------------:|
| **OpenLocalize**            | **Open**       | **100**          |     **3.957 ms** |   **1.217 ms** |   **0.3161 ms** |  **1.08** |    **0.11** |          **-** |         **-** |   **2.56 MB** |        **1.19** |
| OpenStatic              | Open       | 100          |     3.688 ms |   1.912 ms |   0.2959 ms |  1.01 |    0.11 |          - |         - |   2.15 MB |        1.00 |
|                         |            |              |              |            |             |       |         |            |           |           |             |
| **OpenLocalize**            | **Open**       | **10000**        |   **610.981 ms** |  **65.839 ms** |  **17.0982 ms** |  **1.34** |    **0.04** | **15000.0000** | **9000.0000** | **239.47 MB** |        **1.20** |
| OpenStatic              | Open       | 10000        |   457.379 ms |  36.382 ms |   9.4484 ms |  1.00 |    0.03 | 12000.0000 | 6000.0000 | 199.13 MB |        1.00 |
|                         |            |              |              |            |             |       |         |            |           |           |             |
| **SwitchLocalize**          | **Switch**     | **100**          |     **7.403 ms** |   **1.093 ms** |   **0.1691 ms** |  **2.53** |    **0.22** |          **-** |         **-** |   **1.93 MB** |        **1.49** |
| SwitchTranslationSource | Switch     | 100          |     2.942 ms |   1.684 ms |   0.2607 ms |  1.01 |    0.12 |          - |         - |    1.3 MB |        1.00 |
|                         |            |              |              |            |             |       |         |            |           |           |             |
| **SwitchLocalize**          | **Switch**     | **10000**        | **1,148.638 ms** | **529.510 ms** | **137.5120 ms** |  **3.05** |    **0.39** | **11000.0000** | **3000.0000** | **178.79 MB** |        **1.55** |
| SwitchTranslationSource | Switch     | 10000        |   378.458 ms | 108.346 ms |  28.1372 ms |  1.00 |    0.10 |  7000.0000 | 2000.0000 | 115.24 MB |        1.00 |
