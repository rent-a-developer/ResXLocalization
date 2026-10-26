```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.33650/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host] : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Toolchain=InProcessEmitToolchain  IterationCount=15  IterationTime=100ms  
WarmupCount=3  

```
| Method                | ManagerCount | Mean        | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|---------------------- |------------- |------------:|----------:|----------:|------:|-------:|----------:|------------:|
| **GetStringKeySearchAll** | **1**            |    **55.66 ns** |  **0.442 ns** |  **0.369 ns** |  **1.36** |      **-** |         **-** |          **NA** |
| HandWrittenSearchAll  | 1            |    40.91 ns |  0.137 ns |  0.114 ns |  1.00 |      - |         - |          NA |
|                       |              |             |           |           |       |        |           |             |
| **GetStringKeySearchAll** | **5**            |   **802.78 ns** |  **1.983 ns** |  **1.656 ns** |  **1.04** | **0.0161** |     **352 B** |        **1.00** |
| HandWrittenSearchAll  | 5            |   772.74 ns |  9.194 ns |  8.150 ns |  1.00 | 0.0155 |     352 B |        1.00 |
|                       |              |             |           |           |       |        |           |             |
| **GetStringKeySearchAll** | **20**           | **3,590.10 ns** | **43.921 ns** | **36.676 ns** |  **1.03** | **0.0716** |    **1672 B** |        **1.00** |
| HandWrittenSearchAll  | 20           | 3,493.74 ns | 37.568 ns | 31.371 ns |  1.00 | 0.0727 |    1672 B |        1.00 |
