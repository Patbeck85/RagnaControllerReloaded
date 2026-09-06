```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.9168)
AMD Ryzen 7 8745HS w/ Radeon 780M Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]     : .NET 8.0.26 (8.0.2626.16921), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-FSVFRZ : .NET 8.0.26 (8.0.2626.16921), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Server=True  IterationCount=10  LaunchCount=1  
WarmupCount=3  

```
| Method                                            | Mean         | Error        | StdDev     | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------------------- |-------------:|-------------:|-----------:|-------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Struct allocation (no pooling)&#39;                  |    165.59 ns |    10.076 ns |   5.270 ns |   1.00 |    0.04 | 0.0021 |      - |     376 B |        1.00 |
| &#39;Object pool rent/return&#39;                         |     50.97 ns |     3.184 ns |   2.106 ns |   0.31 |    0.02 |      - |      - |         - |        0.00 |
| &#39;List allocation + clear (100 items)&#39;             | 15,596.69 ns | 1,154.466 ns | 763.608 ns |  94.28 |    5.32 | 0.2289 |      - |   38456 B |      102.28 |
| &#39;Array pool rent/return (100 ControllerSnapshot)&#39; | 17,966.64 ns | 1,451.867 ns | 863.982 ns | 108.60 |    6.03 | 0.2289 | 0.0153 |   37600 B |      100.00 |
