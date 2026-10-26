```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.33650/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host] : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Toolchain=InProcessEmitToolchain  InvocationCount=1  IterationCount=15  
IterationTime=100ms  UnrollFactor=1  WarmupCount=50  

```
| Method                | Mean     | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|---------------------- |---------:|----------:|----------:|------:|--------:|----------:|------------:|
| Localize              | 8.112 ms | 1.8605 ms | 1.7403 ms |  2.12 |    0.49 |   2.54 MB |        1.19 |
| LocalizeEnum          | 5.258 ms | 1.1490 ms | 1.0748 ms |  1.38 |    0.31 |    2.5 MB |        1.17 |
| LocalizeWithArguments | 4.777 ms | 0.6741 ms | 0.5263 ms |  1.25 |    0.18 |   3.27 MB |        1.53 |
| Static                | 3.855 ms | 0.4214 ms | 0.3941 ms |  1.01 |    0.14 |   2.14 MB |        1.00 |
