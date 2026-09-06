```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.9168)
AMD Ryzen 7 8745HS w/ Radeon 780M Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]     : .NET 8.0.26 (8.0.2626.16921), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-CFBRYI : .NET 8.0.26 (8.0.2626.16921), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Server=True  IterationCount=10  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|----------------------------------------------- |-----:|------:|------:|--------:|------------:|
| &#39;Single controller poll (no device connected)&#39; |   NA |    NA |     ? |       ? |           ? |
| &#39;Poll + button state check (hot path)&#39;         |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  ControllerPollingBenchmarks.'Single controller poll (no device connected)': Job-CFBRYI(Server=True, IterationCount=10, LaunchCount=1, WarmupCount=3)
  ControllerPollingBenchmarks.'Poll + button state check (hot path)': Job-CFBRYI(Server=True, IterationCount=10, LaunchCount=1, WarmupCount=3)
