```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.33650/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host] : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Toolchain=InProcessEmitToolchain  IterationCount=15  IterationTime=100ms  
WarmupCount=3  

```
| Method                 | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| GetWithMessageFormat   | 1.817 μs | 0.0201 μs | 0.0178 μs |  1.66 |    0.02 | 0.1440 |   2.48 KB |        2.14 |
| MessageFormatterDirect | 1.097 μs | 0.0077 μs | 0.0060 μs |  1.00 |    0.01 | 0.0664 |   1.16 KB |        1.00 |
