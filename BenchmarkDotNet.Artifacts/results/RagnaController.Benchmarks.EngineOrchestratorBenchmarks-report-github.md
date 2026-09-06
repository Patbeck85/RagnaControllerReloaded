```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.9168)
AMD Ryzen 7 8745HS w/ Radeon 780M Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]     : .NET 8.0.26 (8.0.2626.16921), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-CFBRYI : .NET 8.0.26 (8.0.2626.16921), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Server=True  IterationCount=10  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean   | Error  | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------------------- |-------:|-------:|------:|--------:|----------:|----------:|------------:|
| &#39;Start/Stop Orchestrator&#39; |     NA |     NA |     ? |       ? |        NA |        NA |           ? |
| &#39;Get CommandQueue&#39;        | 0.0 ns | 0.0 ns |     ? |       ? |       8 B |         - |           ? |

Benchmarks with issues:
  EngineOrchestratorBenchmarks.'Start/Stop Orchestrator': Job-CFBRYI(Server=True, IterationCount=10, LaunchCount=1, WarmupCount=3)
