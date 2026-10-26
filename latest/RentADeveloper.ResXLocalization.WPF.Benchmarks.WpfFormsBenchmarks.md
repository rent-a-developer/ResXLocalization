```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.33650/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host] : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Toolchain=InProcessEmitToolchain  InvocationCount=1  IterationCount=15  
IterationTime=100ms  UnrollFactor=1  WarmupCount=300  

```
| Method                | Mean     | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
|---------------------- |---------:|----------:|----------:|------:|--------:|-----------:|------------:|
| Localize              | 5.704 ms | 0.9023 ms | 0.8440 ms |  2.25 |    0.32 |  932.62 KB |        2.51 |
| LocalizeEnum          | 3.586 ms | 0.1807 ms | 0.1410 ms |  1.42 |    0.06 |  933.09 KB |        2.51 |
| LocalizeWithArguments | 5.563 ms | 0.2112 ms | 0.1649 ms |  2.20 |    0.07 | 1604.48 KB |        4.31 |
| Static                | 2.533 ms | 0.0344 ms | 0.0305 ms |  1.00 |    0.02 |  372.12 KB |        1.00 |
