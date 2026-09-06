## .NET 8.0.26 (8.0.2626.16921), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
```assembly
; RagnaController.Benchmarks.EngineOrchestratorBenchmarks.GetCommandQueue()
;         public void GetCommandQueue() => _ = _orchestrator.CommandQueue;
;                                          ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
       mov       rax,[rcx+8]
       mov       eax,[rax+48]
       ret
; Total bytes of code 8
```

