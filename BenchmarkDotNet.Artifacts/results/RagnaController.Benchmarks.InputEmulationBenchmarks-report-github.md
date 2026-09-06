```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.9168)
AMD Ryzen 7 8745HS w/ Radeon 780M Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]     : .NET 8.0.26 (8.0.2626.16921), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-FSVFRZ : .NET 8.0.26 (8.0.2626.16921), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Server=True  IterationCount=10  LaunchCount=1  
WarmupCount=3  

```
| Method                                       | Mean         | Error      | StdDev     | Median      | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Allocated | Alloc Ratio |
|--------------------------------------------- |-------------:|-----------:|-----------:|------------:|------:|--------:|-------:|----------:|-------:|----------:|------------:|
| &#39;Single keystroke emulation (KeyDown+KeyUp)&#39; |   294.794 ns | 266.842 ns | 176.500 ns |   234.02 ns |  1.31 |    1.01 | 0.0002 |   4,566 B |      - |     145 B |        1.00 |
| &#39;Key chord (Ctrl+Shift+A)&#39;                   |   651.380 ns | 335.461 ns | 221.887 ns |   541.55 ns |  2.89 |    1.66 | 0.0010 |   4,779 B |      - |     448 B |        3.09 |
| &#39;Mouse move + click (absolute coordinates)&#39;  |   364.594 ns | 240.348 ns | 125.707 ns |   355.19 ns |  1.62 |    0.93 | 0.0002 |        NA | 0.0001 |     112 B |        0.77 |
| &#39;10 rapid keystrokes (macro simulation)&#39;     | 2,213.545 ns | 468.857 ns | 310.120 ns | 2,216.86 ns |  9.82 |    4.66 |      - |        NA |      - |   11535 B |       79.55 |
| &#39;Unicode text input (chat message)&#39;          |     9.379 ns |   3.272 ns |   2.164 ns |    10.09 ns |  0.04 |    0.02 |      - |        NA |      - |         - |        0.00 |
