```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.9168)
AMD Ryzen 7 8745HS w/ Radeon 780M Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]     : .NET 8.0.26 (8.0.2626.16921), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-FSVFRZ : .NET 8.0.26 (8.0.2626.16921), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Server=True  IterationCount=10  LaunchCount=1  
WarmupCount=3  

```
| Method                                                   | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated  | Alloc Ratio |
|--------------------------------------------------------- |-----------:|----------:|----------:|------:|--------:|-------:|-----------:|------------:|
| &#39;End-to-end latency: KeyDown+KeyUp (hardware→SendInput)&#39; |   581.0 μs | 203.75 μs | 134.77 μs |  1.05 |    0.33 | 0.9766 |   368.4 KB |        1.00 |
| &#39;End-to-end latency: Mouse move + click&#39;                 |   245.5 μs |  26.83 μs |  17.75 μs |  0.44 |    0.10 |      - |  204.43 KB |        0.55 |
| &#39;End-to-end latency: 10 rapid keystrokes (macro)&#39;        | 3,196.4 μs | 955.83 μs | 632.22 μs |  5.76 |    1.66 | 3.9063 | 2151.11 KB |        5.84 |
