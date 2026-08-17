```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.9168)
AMD Ryzen 7 8745HS w/ Radeon 780M Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 8.0.26 (8.0.2626.16921), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 8.0.26 (8.0.2626.16921), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                              | Mean        | Error     | StdDev    | Code Size | Gen0   | Allocated |
|---------------------------------------------------- |------------:|----------:|----------:|----------:|-------:|----------:|
| &#39;ComboEngine.Update (button held)&#39;                  |          NA |        NA |        NA |        NA |     NA |        NA |
| &#39;MovementEngine.Update (stick forward)&#39;             |          NA |        NA |        NA |        NA |     NA |        NA |
| &#39;Messenger.Publish&lt;SnapshotReadyMessage&gt; (10 subs)&#39; |  25.3419 ns | 5.0200 ns | 0.2752 ns |     398 B | 0.0029 |      24 B |
| &#39;ControllerSnapshot init (readonly record struct)&#39;  | 118.4355 ns | 8.4914 ns | 0.4654 ns |     479 B | 0.0440 |     368 B |
| &#39;JitterService.Apply (Random.Shared)&#39;               |   2.5789 ns | 0.4824 ns | 0.0264 ns |     357 B |      - |         - |
| &#39;ComboEngine.Update (button released)&#39;              |   0.4443 ns | 0.2285 ns | 0.0125 ns |     152 B |      - |         - |
| &#39;MovementEngine.Update (no movement)&#39;               |   1.2832 ns | 0.1908 ns | 0.0105 ns |     919 B |      - |         - |
| &#39;Messenger.Publish (empty message)&#39;                 |  24.6125 ns | 0.6735 ns | 0.0369 ns |     398 B | 0.0029 |      24 B |
| &#39;JitterService.Apply (low jitter)&#39;                  |   3.7349 ns | 0.8209 ns | 0.0450 ns |     357 B |      - |         - |
| &#39;JitterService.Apply (high jitter)&#39;                 |   2.5744 ns | 0.1381 ns | 0.0076 ns |     360 B |      - |         - |

Benchmarks with issues:
  EngineBenchmarks.'ComboEngine.Update (button held)': ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  EngineBenchmarks.'MovementEngine.Update (stick forward)': ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
