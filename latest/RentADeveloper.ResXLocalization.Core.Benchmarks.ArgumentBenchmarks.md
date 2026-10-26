```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.33650/24H2/2024Update/HudsonValley) (Hyper-V)
AMD EPYC 7763 2.44GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host] : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Toolchain=InProcessEmitToolchain  IterationCount=15  IterationTime=100ms  
WarmupCount=3  

```
| Method                       | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| GetWithFromObject            | 944.6 ns | 98.09 ns | 86.95 ns |  6.74 |    0.60 | 0.0873 |    1512 B |       14.54 |
| GetWithFromTuples            | 819.0 ns | 28.21 ns | 23.56 ns |  5.85 |    0.17 | 0.0795 |    1392 B |       13.38 |
| GetWithFromValues            | 571.7 ns | 36.64 ns | 34.28 ns |  4.08 |    0.24 | 0.0501 |     856 B |        8.23 |
| StringFormatDesignerProperty | 140.1 ns |  0.97 ns |  0.86 ns |  1.00 |    0.01 | 0.0058 |     104 B |        1.00 |
