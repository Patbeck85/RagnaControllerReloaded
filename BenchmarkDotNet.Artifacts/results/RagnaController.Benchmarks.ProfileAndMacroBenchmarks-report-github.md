```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.9168)
AMD Ryzen 7 8745HS w/ Radeon 780M Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]     : .NET 8.0.26 (8.0.2626.16921), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-FSVFRZ : .NET 8.0.26 (8.0.2626.16921), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Server=True  IterationCount=10  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| &#39;Profile load from memory&#39;              | 35.08 ns | 4.742 ns | 2.480 ns |  1.00 |    0.10 | 0.0012 |     192 B |        1.00 |
| &#39;Profile switch (unload old, load new)&#39; | 23.77 ns | 2.518 ns | 1.666 ns |  0.68 |    0.07 | 0.0008 |     128 B |        0.67 |
