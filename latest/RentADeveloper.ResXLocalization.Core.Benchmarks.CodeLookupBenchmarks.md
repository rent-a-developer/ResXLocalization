```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.33650/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host] : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Toolchain=InProcessEmitToolchain  IterationCount=15  IterationTime=100ms  
WarmupCount=3  

```
| Method                | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------- |----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| GetEnum               | 114.93 ns | 0.496 ns | 0.440 ns |  2.61 |    0.08 | 0.0049 |      96 B |          NA |
| GetStringKeyScoped    |  54.01 ns | 0.287 ns | 0.255 ns |  1.22 |    0.04 |      - |         - |          NA |
| GetStringKeySearchAll |  55.42 ns | 0.192 ns | 0.160 ns |  1.26 |    0.04 |      - |         - |          NA |
| GetTypedKey           |  54.54 ns | 0.167 ns | 0.139 ns |  1.24 |    0.04 |      - |         - |          NA |
| ResXDesignerProperty  |  44.14 ns | 1.651 ns | 1.464 ns |  1.00 |    0.04 |      - |         - |          NA |
