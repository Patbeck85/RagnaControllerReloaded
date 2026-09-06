## .NET 8.0.26 (8.0.2626.16921), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
```assembly
; RagnaController.Benchmarks.InputEmulationBenchmarks.EmulateSingleKeystroke()
;             _queue.KeyDown(VirtualKey.A);
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;             _queue.KeyUp(VirtualKey.A);
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,20
       mov       rbx,rcx
       mov       rsi,[rbx+8]
       cmp       [rsi],sil
       mov       rdi,offset MT_RagnaController.Core.InputCmd
       mov       rcx,rdi
       call      CORINFO_HELP_NEWSFAST
       mov       dword ptr [rax+10],5
       mov       word ptr [rax+1C],0
       mov       dword ptr [rax+14],41
       xor       ecx,ecx
       mov       [rax+18],ecx
       mov       [rax+8],rcx
       mov       rcx,rsi
       mov       rdx,rax
       call      qword ptr [7FF877C57780]; RagnaController.Core.InputCommandQueue.Enqueue(RagnaController.Core.InputCmd)
       mov       rbx,[rbx+8]
       cmp       [rbx],bl
       mov       rcx,rdi
       call      CORINFO_HELP_NEWSFAST
       mov       dword ptr [rax+10],6
       mov       word ptr [rax+1C],0
       mov       dword ptr [rax+14],41
       xor       ecx,ecx
       mov       [rax+18],ecx
       mov       [rax+8],rcx
       mov       rcx,rbx
       mov       rdx,rax
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       jmp       qword ptr [7FF877C57780]; RagnaController.Core.InputCommandQueue.Enqueue(RagnaController.Core.InputCmd)
; Total bytes of code 138
```
```assembly
; RagnaController.Core.InputCommandQueue.Enqueue(RagnaController.Core.InputCmd)
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,38
       mov       rbx,rcx
       mov       rsi,rdx
       cmp       byte ptr [rbx+84],0
       jne       short M01_L01
       mov       rcx,[rbx+8]
       cmp       byte ptr [rcx+38],0
       jne       short M01_L02
       cmp       dword ptr [rcx+34],80000000
       je        short M01_L00
       mov       rcx,[rbx+8]
       cmp       [rcx],cl
       mov       rdx,rsi
       xor       r9d,r9d
       xor       r8d,r8d
       call      qword ptr [7FF877C5D038]; System.Collections.Concurrent.BlockingCollection`1[[System.__Canon, System.Private.CoreLib]].TryAddWithNoTimeValidation(System.__Canon, Int32, System.Threading.CancellationToken)
       mov       rax,[rbx+40]
       test      rax,rax
       jne       short M01_L03
       mov       rdi,[rbx+30]
       test      rdi,rdi
       jne       short M01_L04
M01_L00:
       add       rsp,38
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       ret
M01_L01:
       mov       rcx,offset MT_System.ObjectDisposedException
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       ecx,723C
       mov       rdx,7FF877C62938
       call      CORINFO_HELP_STRCNS
       mov       rdx,rax
       mov       rcx,rsi
       call      qword ptr [7FF877C5EAF0]
       mov       rcx,rsi
       call      CORINFO_HELP_THROW
M01_L02:
       call      qword ptr [7FF877A8EE80]
       int       3
M01_L03:
       mov       rdx,rsi
       mov       rcx,[rax+8]
       call      qword ptr [rax+18]
       mov       rdi,[rbx+30]
       test      rdi,rdi
       je        short M01_L00
M01_L04:
       mov       rbx,[rbx+38]
       cmp       byte ptr [rdi+0D4],0
       jne       short M01_L00
       lea       rax,[rdi+0D0]
       mov       edx,1
       mov       ecx,edx
       lock xadd [rax],ecx
       inc       ecx
       mov       edx,14F8B589
       mov       eax,edx
       imul      ecx
       mov       eax,edx
       shr       eax,1F
       sar       edx,0C
       add       edx,eax
       imul      edx,0C350
       sub       ecx,edx
       mov       rdx,[rdi+8]
       cmp       ecx,[rdx+8]
       jae       near ptr M01_L10
       xor       eax,eax
       mov       [rdx+rcx*8+10],rax
       mov       rsi,[rdi+28]
       mov       rcx,7FF877C63708
       mov       edx,128
       call      CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE
       mov       rcx,21B97805F38
       mov       r8,[rcx]
       test      r8,r8
       jne       short M01_L05
       mov       rcx,offset MT_System.Func`2[[System.String, System.Private.CoreLib],[RagnaController.Core.InputLatencyTracker+ControllerLatencyStats, RagnaController]]
       call      CORINFO_HELP_NEWSFAST
       mov       rbp,rax
       mov       rcx,7FF877C63708
       mov       edx,128
       call      CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE
       mov       rdx,21B97805F30
       mov       rdx,[rdx]
       mov       rcx,rbp
       mov       r8,offset RagnaController.Core.InputLatencyTracker+<>c.<RecordEnqueueLatency>b__30_0(System.String)
       call      qword ptr [7FF8778F4210]; System.MulticastDelegate.CtorClosed(System.Object, IntPtr)
       mov       rcx,7FF877C63708
       mov       edx,128
       call      CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE
       mov       rcx,21B97805F38
       mov       rdx,rbp
       call      CORINFO_HELP_ASSIGN_REF
       mov       r8,rbp
M01_L05:
       mov       rcx,rsi
       mov       rdx,rbx
       cmp       [rcx],ecx
       call      qword ptr [7FF877C16310]; System.Collections.Concurrent.ConcurrentDictionary`2[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].GetOrAdd(System.__Canon, System.Func`2<System.__Canon,System.__Canon>)
       mov       [rsp+28],rax
       cmp       [rax],al
       lea       rcx,[rax+30]
       lock add  qword ptr [rcx],0
       lea       rcx,[rax+8]
       lock inc  qword ptr [rcx]
       lea       rcx,[rax+50]
       xor       edx,edx
       xor       eax,eax
       lock cmpxchg [rcx],rdx
       jmp       short M01_L07
M01_L06:
       mov       rcx,[rsp+28]
       lea       rdx,[rcx+50]
       xor       r8d,r8d
       mov       [rsp+30],rax
       lock cmpxchg [rdx],r8
       cmp       rax,[rsp+30]
       je        short M01_L08
       lea       rdx,[rcx+50]
       xor       r8d,r8d
       xor       eax,eax
       lock cmpxchg [rdx],r8
M01_L07:
       test      rax,rax
       jl        short M01_L06
M01_L08:
       lea       rax,[rdi+58]
       mov       ecx,1
       lock xadd [rax],rcx
       inc       rcx
       cmp       rcx,0C350
       jle       short M01_L09
       lea       rax,[rdi+58]
       mov       ecx,0C350
       xchg      rcx,[rax]
M01_L09:
       add       rdi,0C0
       lock inc  qword ptr [rdi]
       jmp       near ptr M01_L00
M01_L10:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 568
```
```assembly
; System.Collections.Concurrent.BlockingCollection`1[[System.__Canon, System.Private.CoreLib]].TryAddWithNoTimeValidation(System.__Canon, Int32, System.Threading.CancellationToken)
       push      rbp
       push      rsi
       push      rbx
       sub       rsp,50
       lea       rbp,[rsp+60]
       mov       [rbp-40],rsp
       mov       [rbp-18],rcx
       mov       [rbp+10],rcx
       mov       [rbp+18],rdx
       mov       [rbp+28],r9
       mov       ebx,r8d
       cmp       byte ptr [rcx+38],0
       jne       near ptr M02_L17
       cmp       qword ptr [rbp+28],0
       je        short M02_L00
       mov       rax,[rbp+28]
       cmp       dword ptr [rax+20],0
       jne       near ptr M02_L16
M02_L00:
       cmp       byte ptr [rcx+38],0
       jne       near ptr M02_L17
       cmp       dword ptr [rcx+34],80000000
       je        near ptr M02_L18
       mov       dword ptr [rbp-1C],1
       cmp       qword ptr [rcx+10],0
       jne       near ptr M02_L19
M02_L01:
       cmp       dword ptr [rbp-1C],0
       je        near ptr M02_L15
       xor       eax,eax
       mov       [rbp-28],eax
       mov       rcx,[rbp+10]
       mov       eax,[rcx+34]
       test      eax,80000000
       jne       near ptr M02_L06
M02_L02:
       lea       rsi,[rcx+34]
       lea       edx,[rax+1]
       mov       [rbp-30],eax
       lock cmpxchg [rsi],edx
       cmp       eax,[rbp-30]
       jne       near ptr M02_L05
       xor       edx,edx
       mov       [rbp-2C],edx
       jmp       near ptr M02_L09
M02_L03:
       mov       r8,[rcx+10]
       mov       rcx,r8
       xor       r8d,r8d
       xor       edx,edx
       cmp       [rcx],ecx
       call      qword ptr [7FF877C5DC20]; System.Threading.SemaphoreSlim.Wait(Int32, System.Threading.CancellationToken)
       mov       [rbp-1C],eax
       cmp       dword ptr [rbp-1C],0
       jne       short M02_L04
       test      ebx,ebx
       je        short M02_L04
       mov       rsi,[rbp+28]
       mov       rcx,[rbp+10]
       mov       rcx,[rcx+28]
       cmp       [rcx],ecx
       call      qword ptr [7FF877C57C18]; System.Threading.CancellationTokenSource.get_Token()
       mov       rdx,rax
       mov       rcx,rsi
       call      qword ptr [7FF877C57E10]; System.Threading.CancellationTokenSource.CreateLinkedTokenSource(System.Threading.CancellationToken, System.Threading.CancellationToken)
       mov       [rbp-38],rax
       mov       rcx,[rbp+10]
       mov       rsi,[rcx+10]
       mov       rcx,[rbp-38]
       cmp       [rcx],ecx
       call      qword ptr [7FF877C57C18]; System.Threading.CancellationTokenSource.get_Token()
       mov       r8,rax
       mov       rcx,rsi
       mov       edx,ebx
       cmp       [rcx],ecx
       call      qword ptr [7FF877C5DC20]; System.Threading.SemaphoreSlim.Wait(Int32, System.Threading.CancellationToken)
       mov       [rbp-1C],eax
M02_L04:
       mov       rcx,rsp
       call      M02_L20
       jmp       near ptr M02_L01
M02_L05:
       lea       rcx,[rbp-28]
       mov       edx,0FFFFFFFF
       call      qword ptr [7FF877C5F198]
       mov       rcx,[rbp+10]
       mov       eax,[rcx+34]
       test      eax,80000000
       je        near ptr M02_L02
M02_L06:
       xor       edx,edx
       mov       [rbp-28],edx
       jmp       short M02_L08
M02_L07:
       lea       rcx,[rbp-28]
       mov       edx,14
       call      qword ptr [7FF877C5F1B0]; System.Threading.SpinWait.SpinOnceCore(Int32)
       mov       rcx,[rbp+10]
M02_L08:
       cmp       dword ptr [rcx+34],80000000
       jne       short M02_L07
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       call      qword ptr [7FF877D74810]
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FF877AB6718]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M02_L09:
       cmp       qword ptr [rbp+28],0
       je        short M02_L10
       mov       rdx,[rbp+28]
       cmp       dword ptr [rdx+20],0
       jne       short M02_L12
M02_L10:
       mov       rdx,[rcx]
       mov       rax,[rdx+30]
       mov       rax,[rax]
       mov       r11,[rax+30]
       test      r11,r11
       je        short M02_L13
       jmp       short M02_L14
M02_L11:
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       call      qword ptr [7FF877D747B0]
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FF877AB6718]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
M02_L12:
       lea       rcx,[rbp+28]
       call      qword ptr [7FF877A85740]; System.Threading.CancellationToken.ThrowOperationCanceledException()
       int       3
M02_L13:
       mov       rcx,rdx
       mov       rdx,7FF877D1A2E8
       call      CORINFO_HELP_RUNTIMEHANDLE_CLASS
       mov       r11,rax
M02_L14:
       mov       rcx,[rbp+10]
       mov       rcx,[rcx+8]
       mov       rdx,[rbp+18]
       call      qword ptr [r11]
       mov       [rbp-2C],eax
       cmp       dword ptr [rbp-2C],0
       je        short M02_L11
       mov       rcx,[rbp+10]
       mov       rax,[rcx+18]
       cmp       [rax],al
       mov       rcx,rax
       mov       edx,1
       call      qword ptr [7FF877C5DD40]; System.Threading.SemaphoreSlim.Release(Int32)
       lock dec  dword ptr [rsi]
M02_L15:
       mov       eax,[rbp-1C]
       add       rsp,50
       pop       rbx
       pop       rsi
       pop       rbp
       ret
M02_L16:
       lea       rcx,[rbp+28]
       call      qword ptr [7FF877A85740]; System.Threading.CancellationToken.ThrowOperationCanceledException()
       int       3
M02_L17:
       call      qword ptr [7FF877A8EE80]
       int       3
M02_L18:
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       call      qword ptr [7FF877D74810]
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FF877AB6718]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
M02_L19:
       xor       r8d,r8d
       mov       [rbp-38],r8
       jmp       near ptr M02_L03
       push      rbp
       push      rsi
       push      rbx
       sub       rsp,30
       mov       rbp,[rcx+20]
       mov       [rsp+20],rbp
       lea       rbp,[rbp+60]
       lea       rcx,[rbp+28]
       call      qword ptr [7FF877A85728]; System.Threading.CancellationToken.ThrowIfCancellationRequested()
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       call      qword ptr [7FF877D74798]
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FF877AB6718]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M02_L20:
       push      rbp
       push      rsi
       push      rbx
       sub       rsp,30
       mov       rbp,[rcx+20]
       mov       [rsp+20],rbp
       lea       rbp,[rbp+60]
       cmp       qword ptr [rbp-38],0
       je        short M02_L21
       mov       rcx,[rbp-38]
       mov       edx,1
       mov       rax,[rbp-38]
       mov       rax,[rax]
       mov       rax,[rax+40]
       call      qword ptr [rax+28]
       mov       rcx,[rbp-38]
       call      qword ptr [7FF877BD4960]; System.GC.SuppressFinalize(System.Object)
M02_L21:
       nop
       add       rsp,30
       pop       rbx
       pop       rsi
       pop       rbp
       ret
       push      rbp
       push      rsi
       push      rbx
       sub       rsp,30
       mov       rbp,[rcx+20]
       mov       [rsp+20],rbp
       lea       rbp,[rbp+60]
       cmp       dword ptr [rbp-2C],0
       je        short M02_L22
       mov       rcx,[rbp+10]
       mov       rax,[rcx+18]
       cmp       [rax],al
       mov       rcx,rax
       mov       edx,1
       call      qword ptr [7FF877C5DD40]; System.Threading.SemaphoreSlim.Release(Int32)
       jmp       short M02_L23
M02_L22:
       mov       rcx,[rbp+10]
       mov       rdx,[rcx+10]
       test      rdx,rdx
       je        short M02_L23
       mov       rcx,rdx
       mov       edx,1
       call      qword ptr [7FF877C5DD40]; System.Threading.SemaphoreSlim.Release(Int32)
M02_L23:
       mov       rcx,[rbp+10]
       lea       rsi,[rcx+34]
       lock dec  dword ptr [rsi]
       add       rsp,30
       pop       rbx
       pop       rsi
       pop       rbp
       ret
; Total bytes of code 899
```
```assembly
; System.MulticastDelegate.CtorClosed(System.Object, IntPtr)
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rsi,r8
       test      rdx,rdx
       je        short M03_L00
       lea       rcx,[rbx+8]
       call      CORINFO_HELP_ASSIGN_REF
       mov       [rbx+18],rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M03_L00:
       call      qword ptr [7FF8778F41F8]
       int       3
; Total bytes of code 44
```
```assembly
; System.Collections.Concurrent.ConcurrentDictionary`2[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].GetOrAdd(System.__Canon, System.Func`2<System.__Canon,System.__Canon>)
       push      rbp
       push      rbp
       push      rbp
       push      r15
       push      r15
       push      r15
       push      r14
       push      r14
       push      r14
       push      rdi
       push      rdi
       push      rdi
       push      rsi
       push      rsi
       push      rsi
       push      rbx
       push      rbx
       push      rbx
       sub       rsp,58
       sub       rsp,58
       sub       rsp,58
       lea       rbp,[rsp+80]
       lea       rbp,[rsp+80]
       lea       rbp,[rsp+80]
       xor       eax,eax
       xor       eax,eax
       xor       eax,eax
       mov       [rbp-38],rax
       mov       [rbp-38],rax
       mov       [rbp-38],rax
       mov       [rbp-30],rcx
       mov       [rbp-30],rcx
       mov       [rbp-30],rcx
       mov       rsi,rcx
       mov       rsi,rcx
       mov       rsi,rcx
       mov       rbx,rdx
       mov       rbx,rdx
       mov       rbx,rdx
       mov       rdi,r8
       mov       rdi,r8
       mov       rdi,r8
       test      rbx,rbx
       test      rbx,rbx
       test      rbx,rbx
       je        near ptr M04_L04
       je        near ptr M04_L04
       je        near ptr M04_L04
       test      rdi,rdi
       test      rdi,rdi
       test      rdi,rdi
       je        near ptr M04_L05
       je        near ptr M04_L05
       je        near ptr M04_L05
       mov       r14,[rsi+8]
       mov       r14,[rsi+8]
       mov       r14,[rsi+8]
       mov       r15,[r14+8]
       mov       r15,[r14+8]
       mov       r15,[r14+8]
       cmp       byte ptr [rsi+15],0
       cmp       byte ptr [rsi+15],0
       cmp       byte ptr [rsi+15],0
       je        short M04_L02
       je        short M04_L02
       je        short M04_L02
       mov       rcx,rbx
       mov       rcx,rbx
       mov       rcx,rbx
       lea       r11,[7FF94057C0A8]
       lea       r11,[7FF94057C0A8]
       lea       r11,[7FF94057C0A8]
       call      qword ptr [r11]
       call      qword ptr [r11]
       call      qword ptr [r11]
       mov       r15d,eax
       mov       r15d,eax
       mov       r15d,eax
M04_L00:
       mov       rcx,[rsi]
M04_L00:
       mov       rcx,[rsi]
M04_L00:
       mov       rcx,[rsi]
       call      qword ptr [7FF94057C500]
       call      qword ptr [7FF94057C500]
       call      qword ptr [7FF94057C500]
       mov       rcx,rax
       mov       rcx,rax
       mov       rcx,rax
       lea       rdx,[rbp-38]
       lea       rdx,[rbp-38]
       lea       rdx,[rbp-38]
       mov       [rsp+20],rdx
       mov       [rsp+20],rdx
       mov       [rsp+20],rdx
       mov       rdx,r14
       mov       rdx,r14
       mov       rdx,r14
       mov       r8,rbx
       mov       r8,rbx
       mov       r8,rbx
       mov       r9d,r15d
       mov       r9d,r15d
       mov       r9d,r15d
       call      qword ptr [7FF94057D2D0]; Precode of System.Collections.Concurrent.ConcurrentDictionary`2[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].TryGetValueInternal(Tables<System.__Canon,System.__Canon>, System.__Canon, Int32, System.__Canon ByRef)
       call      qword ptr [7FF94057D2D0]; Precode of System.Collections.Concurrent.ConcurrentDictionary`2[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].TryGetValueInternal(Tables<System.__Canon,System.__Canon>, System.__Canon, Int32, System.__Canon ByRef)
       call      qword ptr [7FF94057D2D0]; Precode of System.Collections.Concurrent.ConcurrentDictionary`2[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].TryGetValueInternal(Tables<System.__Canon,System.__Canon>, System.__Canon, Int32, System.__Canon ByRef)
       test      eax,eax
       test      eax,eax
       test      eax,eax
       je        short M04_L03
       je        short M04_L03
       je        short M04_L03
M04_L01:
       mov       rax,[rbp-38]
M04_L01:
       mov       rax,[rbp-38]
M04_L01:
       mov       rax,[rbp-38]
       add       rsp,58
       add       rsp,58
       add       rsp,58
       pop       rbx
       pop       rbx
       pop       rbx
       pop       rsi
       pop       rsi
       pop       rsi
       pop       rdi
       pop       rdi
       pop       rdi
       pop       r14
       pop       r14
       pop       r14
       pop       r15
       pop       r15
       pop       r15
       pop       rbp
       pop       rbp
       pop       rbp
       ret
       ret
       ret
M04_L02:
       mov       rcx,[rsi]
M04_L02:
       mov       rcx,[rsi]
M04_L02:
       mov       rcx,[rsi]
       call      qword ptr [7FF94057C7D8]
       call      qword ptr [7FF94057C7D8]
       call      qword ptr [7FF94057C7D8]
       mov       rcx,r15
       mov       rcx,r15
       mov       rcx,r15
       mov       r11,rax
       mov       r11,rax
       mov       r11,rax
       mov       rdx,rbx
       mov       rdx,rbx
       mov       rdx,rbx
       call      qword ptr [rax]
       call      qword ptr [rax]
       call      qword ptr [rax]
       mov       r15d,eax
       mov       r15d,eax
       mov       r15d,eax
       jmp       short M04_L00
       jmp       short M04_L00
       jmp       short M04_L00
M04_L03:
       mov       byte ptr [rbp-40],1
M04_L03:
       mov       byte ptr [rbp-40],1
M04_L03:
       mov       byte ptr [rbp-40],1
       mov       [rbp-3C],r15d
       mov       [rbp-3C],r15d
       mov       [rbp-3C],r15d
       mov       rdx,rbx
       mov       rdx,rbx
       mov       rdx,rbx
       mov       rcx,[rdi+8]
       mov       rcx,[rdi+8]
       mov       rcx,[rdi+8]
       call      qword ptr [rdi+18]
       call      qword ptr [rdi+18]
       call      qword ptr [rdi+18]
       xor       edx,edx
       xor       edx,edx
       xor       edx,edx
       mov       [rsp+28],edx
       mov       [rsp+28],edx
       mov       [rsp+28],edx
       mov       dword ptr [rsp+30],1
       mov       dword ptr [rsp+30],1
       mov       dword ptr [rsp+30],1
       lea       rdx,[rbp-38]
       lea       rdx,[rbp-38]
       lea       rdx,[rbp-38]
       mov       [rsp+38],rdx
       mov       [rsp+38],rdx
       mov       [rsp+38],rdx
       mov       [rsp+20],rax
       mov       [rsp+20],rax
       mov       [rsp+20],rax
       mov       rdx,r14
       mov       rdx,r14
       mov       rdx,r14
       mov       r8,rbx
       mov       r8,rbx
       mov       r8,rbx
       mov       r9,[rbp-40]
       mov       r9,[rbp-40]
       mov       r9,[rbp-40]
       mov       rcx,rsi
       mov       rcx,rsi
       mov       rcx,rsi
       call      qword ptr [7FF94057D300]; Precode of System.Collections.Concurrent.ConcurrentDictionary`2[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].TryAddInternal(Tables<System.__Canon,System.__Canon>, System.__Canon, System.Nullable`1<Int32>, System.__Canon, Boolean, Boolean, System.__Canon ByRef)
       call      qword ptr [7FF94057D300]; Precode of System.Collections.Concurrent.ConcurrentDictionary`2[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].TryAddInternal(Tables<System.__Canon,System.__Canon>, System.__Canon, System.Nullable`1<Int32>, System.__Canon, Boolean, Boolean, System.__Canon ByRef)
       call      qword ptr [7FF94057D300]; Precode of System.Collections.Concurrent.ConcurrentDictionary`2[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].TryAddInternal(Tables<System.__Canon,System.__Canon>, System.__Canon, System.Nullable`1<Int32>, System.__Canon, Boolean, Boolean, System.__Canon ByRef)
       jmp       short M04_L01
       jmp       short M04_L01
       jmp       short M04_L01
M04_L04:
       mov       rcx,[7FF94057D710]
M04_L04:
       mov       rcx,[7FF94057D710]
M04_L04:
       mov       rcx,[7FF94057D710]
       mov       rcx,[rcx]
       mov       rcx,[rcx]
       mov       rcx,[rcx]
       call      qword ptr [7FF94057CD50]
       call      qword ptr [7FF94057CD50]
       call      qword ptr [7FF94057CD50]
       int       3
       int       3
       int       3
M04_L05:
       mov       rcx,[7FF94057D858]
M04_L05:
       mov       rcx,[7FF94057D858]
M04_L05:
       mov       rcx,[7FF94057D858]
       mov       rcx,[rcx]
       mov       rcx,[rcx]
       mov       rcx,[rcx]
       call      qword ptr [7FF94057CD50]
       call      qword ptr [7FF94057CD50]
       call      qword ptr [7FF94057CD50]
       int       3
       int       3
       int       3
; Total bytes of code 810
```
```assembly
; System.Threading.SemaphoreSlim.Wait(Int32, System.Threading.CancellationToken)
       push      rbp
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,98
       vzeroupper
       lea       rbp,[rsp+0B0]
       vxorps    xmm4,xmm4,xmm4
       vmovdqa   xmmword ptr [rbp-50],xmm4
       xor       eax,eax
       mov       [rbp-40],rax
       mov       [rbp-80],rsp
       mov       [rbp+10],rcx
       mov       [rbp+20],r8
       mov       ebx,edx
       mov       rdx,[rcx+8]
       cmp       byte ptr [rdx+8],0
       jne       near ptr M05_L23
       cmp       ebx,0FFFFFFFF
       jl        near ptr M05_L24
       cmp       qword ptr [rbp+20],0
       je        short M05_L00
       mov       rdx,[rbp+20]
       cmp       dword ptr [rdx+20],0
       jne       near ptr M05_L25
M05_L00:
       test      ebx,ebx
       jne       short M05_L01
       cmp       dword ptr [rcx+28],0
       je        near ptr M05_L26
M05_L01:
       xor       esi,esi
       cmp       ebx,0FFFFFFFF
       je        short M05_L02
       test      ebx,ebx
       jg        near ptr M05_L27
M05_L02:
       xor       edx,edx
       mov       [rbp-1C],edx
       mov       [rbp-60],rdx
       mov       [rbp-28],edx
       mov       rdx,21B978006B8
       mov       r8,[rdx]
       mov       rax,[rbp+20]
       test      rax,rax
       jne       near ptr M05_L28
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rbp-50],xmm0
M05_L03:
       mov       rdi,[rbp-50]
       mov       [rbp-70],rdi
       mov       rdx,[rbp-48]
       mov       [rbp-58],rdx
       cmp       dword ptr [rcx+28],0
       je        near ptr M05_L09
M05_L04:
       mov       rax,[rcx+8]
       cmp       byte ptr [rbp-28],0
       jne       short M05_L08
       lea       rdx,[rbp-28]
       mov       rcx,rax
       call      System.Threading.Monitor.ReliableEnter(System.Object, Boolean ByRef)
       mov       rcx,[rbp+10]
       inc       dword ptr [rcx+30]
       cmp       qword ptr [rcx+18],0
       jne       short M05_L07
       xor       eax,eax
       mov       [rbp-68],rax
       cmp       dword ptr [rcx+28],0
       je        near ptr M05_L14
M05_L05:
       mov       rcx,[rbp+10]
       cmp       dword ptr [rcx+28],0
       jle       near ptr M05_L13
       mov       dword ptr [rbp-1C],1
       dec       dword ptr [rcx+28]
M05_L06:
       cmp       qword ptr [rcx+10],0
       jne       near ptr M05_L15
       mov       rdi,[rbp-70]
       jmp       near ptr M05_L16
M05_L07:
       mov       edx,ebx
       mov       r8,[rbp+20]
       call      qword ptr [7FF877C5DCC8]
       mov       [rbp-60],rax
       jmp       near ptr M05_L16
M05_L08:
       call      qword ptr [7FF8778FE040]
       int       3
M05_L09:
       xor       edx,edx
       mov       [rbp-30],edx
       jmp       short M05_L11
M05_L10:
       lea       rcx,[rbp-30]
       mov       edx,0FFFFFFFF
       call      qword ptr [7FF877C5F198]
       mov       rcx,[rbp+10]
       cmp       dword ptr [rcx+28],0
       mov       rcx,[rbp+10]
       jne       near ptr M05_L04
M05_L11:
       cmp       dword ptr [rbp-30],8C
       jl        short M05_L10
       jmp       near ptr M05_L04
M05_L12:
       mov       edx,ebx
       mov       r8d,esi
       mov       r9,[rbp+20]
       call      qword ptr [7FF877C5DC38]; System.Threading.SemaphoreSlim.WaitUntilCountOrTimeout(Int32, UInt32, System.Threading.CancellationToken)
       mov       [rbp-1C],eax
       jmp       near ptr M05_L05
M05_L13:
       cmp       qword ptr [rbp-68],0
       je        near ptr M05_L06
       mov       rcx,[rbp-68]
       call      CORINFO_HELP_THROW
M05_L14:
       test      ebx,ebx
       jne       short M05_L12
       xor       edx,edx
       mov       [rbp-34],edx
       jmp       short M05_L20
M05_L15:
       cmp       dword ptr [rcx+28],0
       mov       rdi,[rbp-70]
       jne       short M05_L16
       mov       rcx,[rbp+10]
       mov       rcx,[rcx+10]
       cmp       [rcx],ecx
       call      qword ptr [7FF877D75920]
       nop
M05_L16:
       cmp       byte ptr [rbp-28],0
       je        short M05_L17
       mov       rcx,[rbp+10]
       dec       dword ptr [rcx+30]
       mov       rcx,[rcx+8]
       call      System.Threading.Monitor.Exit(System.Object)
M05_L17:
       test      rdi,rdi
       jne       short M05_L19
M05_L18:
       cmp       qword ptr [rbp-60],0
       jne       short M05_L21
       mov       eax,[rbp-1C]
       add       rsp,98
       pop       rbx
       pop       rsi
       pop       rdi
       pop       rbp
       ret
M05_L19:
       mov       rcx,[rdi+8]
       mov       rdx,[rbp-58]
       mov       r8,rdi
       cmp       [rcx],ecx
       call      qword ptr [7FF877C5EF10]; System.Threading.CancellationTokenSource+Registrations.Unregister(Int64, CallbackNode)
       test      eax,eax
       jne       short M05_L18
       mov       rcx,[rbp-58]
       mov       rdx,rdi
       call      qword ptr [7FF877C5ED30]
       jmp       short M05_L18
M05_L20:
       mov       rcx,rsp
       call      M05_L29
       jmp       short M05_L22
M05_L21:
       mov       rcx,[rbp-60]
       call      qword ptr [7FF877D75110]
       mov       [rbp-40],rax
       lea       rcx,[rbp-40]
       call      qword ptr [7FF877C5F120]
       nop
       add       rsp,98
       pop       rbx
       pop       rsi
       pop       rdi
       pop       rbp
       ret
M05_L22:
       mov       eax,[rbp-34]
       add       rsp,98
       pop       rbx
       pop       rsi
       pop       rdi
       pop       rbp
       ret
M05_L23:
       call      qword ptr [7FF877A8EE80]
       int       3
M05_L24:
       mov       rcx,offset MT_System.Int32
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       [rsi+8],ebx
       mov       rcx,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       ecx,1351
       mov       rdx,7FF8777A4000
       call      CORINFO_HELP_STRCNS
       mov       rdi,rax
       call      qword ptr [7FF877D4CA80]
       mov       r9,rax
       mov       rdx,rdi
       mov       r8,rsi
       mov       rcx,rbx
       call      qword ptr [7FF8779AD4A0]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
M05_L25:
       lea       rcx,[rbp+20]
       call      qword ptr [7FF877A85740]; System.Threading.CancellationToken.ThrowOperationCanceledException()
       int       3
M05_L26:
       xor       eax,eax
       add       rsp,98
       pop       rbx
       pop       rsi
       pop       rdi
       pop       rbp
       ret
M05_L27:
       call      System.Environment.get_TickCount()
       mov       esi,eax
       mov       rcx,[rbp+10]
       jmp       near ptr M05_L02
M05_L28:
       xor       edx,edx
       mov       [rsp+20],rdx
       mov       [rsp+28],rdx
       lea       rdx,[rbp-50]
       mov       rcx,rax
       mov       r9,[rbp+10]
       call      qword ptr [7FF877C57DB0]; System.Threading.CancellationTokenSource.Register(System.Delegate, System.Object, System.Threading.SynchronizationContext, System.Threading.ExecutionContext)
       mov       rcx,[rbp+10]
       jmp       near ptr M05_L03
       push      rbp
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,38
       vzeroupper
       mov       rbp,[rcx+30]
       mov       [rsp+30],rbp
       lea       rbp,[rbp+0B0]
       mov       [rbp-68],rdx
       lea       rax,[M05_L05]
       add       rsp,38
       pop       rbx
       pop       rsi
       pop       rdi
       pop       rbp
       ret
M05_L29:
       push      rbp
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,38
       vzeroupper
       mov       rbp,[rcx+30]
       mov       [rsp+30],rbp
       lea       rbp,[rbp+0B0]
       cmp       byte ptr [rbp-28],0
       je        short M05_L30
       mov       rcx,[rbp+10]
       dec       dword ptr [rcx+30]
       mov       rcx,[rcx+8]
       call      System.Threading.Monitor.Exit(System.Object)
M05_L30:
       mov       rdi,[rbp-70]
       test      rdi,rdi
       je        short M05_L31
       mov       rcx,[rdi+8]
       mov       rdx,[rbp-58]
       mov       r8,rdi
       cmp       [rcx],ecx
       call      qword ptr [7FF877C5EF10]; System.Threading.CancellationTokenSource+Registrations.Unregister(Int64, CallbackNode)
       test      eax,eax
       jne       short M05_L31
       mov       rcx,[rbp-58]
       mov       rdx,rdi
       call      qword ptr [7FF877C5ED30]
M05_L31:
       nop
       add       rsp,38
       pop       rbx
       pop       rsi
       pop       rdi
       pop       rbp
       ret
; Total bytes of code 936
```
```assembly
; System.Threading.CancellationTokenSource.get_Token()
       sub       rsp,28
       cmp       byte ptr [rcx+24],0
       jne       short M06_L00
       mov       rax,rcx
       add       rsp,28
       ret
M06_L00:
       mov       ecx,46
       call      qword ptr [7FF8B04B6988]
       int       3
; Total bytes of code 30
```
```assembly
; System.Threading.CancellationTokenSource.CreateLinkedTokenSource(System.Threading.CancellationToken, System.Threading.CancellationToken)
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,40
       xor       eax,eax
       mov       [rsp+38],rax
       mov       rbx,rcx
       mov       rsi,rdx
       test      rbx,rbx
       je        short M07_L00
       test      rsi,rsi
       je        short M07_L01
       call      qword ptr [7FF8B04AC500]
       mov       rdi,rax
       mov       rcx,rdi
       mov       rdx,rbx
       mov       r8,rsi
       call      qword ptr [7FF8B04B8FF8]
       mov       rax,rdi
       add       rsp,40
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M07_L00:
       mov       rcx,rsi
       call      qword ptr [7FF8B04B8FE0]
       nop
       add       rsp,40
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M07_L01:
       call      qword ptr [7FF8B04AC4F8]
       mov       rsi,rax
       mov       [rsp+38],rbx
       call      qword ptr [7FF8B04A4310]
       mov       r8,[rax+620]
       xor       edx,edx
       mov       [rsp+20],edx
       mov       [rsp+28],edx
       lea       rdx,[rsi+28]
       lea       rcx,[rsp+38]
       mov       r9,rsi
       call      qword ptr [7FF8B04B8F40]; Precode of System.Threading.CancellationToken.Register(System.Delegate, System.Object, Boolean, Boolean)
       mov       rax,rsi
       add       rsp,40
       pop       rbx
       pop       rsi
       pop       rdi
       ret
; Total bytes of code 149
```
```assembly
; System.Threading.SpinWait.SpinOnceCore(Int32)
       push      rbp
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,78
       lea       rbp,[rsp+0B0]
       mov       rbx,rcx
       mov       esi,edx
       mov       [rbp+10],rbx
       mov       edi,[rbx]
       cmp       edi,0A
       jl        short M08_L01
       cmp       edi,esi
       jl        short M08_L00
       test      esi,esi
       jge       short M08_L05
M08_L00:
       lea       eax,[rdi-0A]
       test      al,1
       je        short M08_L05
M08_L01:
       call      qword ptr [7FF8B04A3E60]
       cmp       dword ptr [rax+0A54],1
       je        short M08_L05
       call      qword ptr [7FF8B04B8CF0]; System.Threading.Thread.get_OptimalMaxSpinWaitsPerSpinIteration()
       mov       rbx,[rbp+10]
       cmp       dword ptr [rbx],1E
       jle       near ptr M08_L08
M08_L02:
       mov       ecx,eax
       call      qword ptr [7FF8B04B8C38]; System.Threading.Thread.SpinWaitInternal(Int32)
M08_L03:
       mov       rcx,rbx
       mov       eax,[rcx]
       cmp       eax,7FFFFFFF
       je        near ptr M08_L13
       inc       eax
M08_L04:
       mov       [rbx],eax
       add       rsp,78
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M08_L05:
       cmp       edi,esi
       jl        short M08_L06
       test      esi,esi
       jge       near ptr M08_L11
M08_L06:
       cmp       edi,0A
       jl        near ptr M08_L12
       add       edi,0FFFFFFF6
       mov       ecx,edi
       shr       ecx,1F
       add       ecx,edi
       sar       ecx,1
M08_L07:
       mov       edx,66666667
       mov       eax,edx
       imul      ecx
       mov       eax,edx
       shr       eax,1F
       sar       edx,1
       add       eax,edx
       lea       eax,[rax+rax*4]
       sub       ecx,eax
       cmp       ecx,4
       je        short M08_L09
       lea       rcx,[rbp-90]
       call      qword ptr [7FF8B04A3D68]; CORINFO_HELP_JIT_PINVOKE_BEGIN
       mov       rax,[7FF8B04CB9D8]
       call      qword ptr [rax]
       lea       rcx,[rbp-90]
       call      qword ptr [7FF8B04A3D70]; CORINFO_HELP_JIT_PINVOKE_END
       mov       rbx,[rbp+10]
       jmp       near ptr M08_L03
M08_L08:
       mov       ecx,[rbx]
       mov       edx,1
       shl       edx,cl
       cmp       edx,eax
       jge       near ptr M08_L02
       jmp       short M08_L10
M08_L09:
       xor       ecx,ecx
       call      qword ptr [7FF8B04B8D40]; Precode of System.Threading.Thread.Sleep(Int32)
       mov       rbx,[rbp+10]
       jmp       near ptr M08_L03
M08_L10:
       mov       ecx,[rbx]
       mov       eax,1
       shl       eax,cl
       jmp       near ptr M08_L02
M08_L11:
       mov       ecx,1
       call      qword ptr [7FF8B04B8D40]; Precode of System.Threading.Thread.Sleep(Int32)
       mov       rbx,[rbp+10]
       jmp       near ptr M08_L03
M08_L12:
       mov       ecx,edi
       jmp       near ptr M08_L07
M08_L13:
       mov       eax,0A
       jmp       near ptr M08_L04
; Total bytes of code 326
```
```assembly
; System.Threading.CancellationToken.ThrowOperationCanceledException()
       push      rbp
       sub       rsp,30
       lea       rbp,[rsp+30]
       xor       eax,eax
       mov       [rbp-8],rax
       mov       [rbp-10],rax
       mov       [rbp+10],rcx
       mov       rcx,offset MT_System.OperationCanceledException
       call      CORINFO_HELP_NEWSFAST
       mov       [rbp-8],rax
       call      qword ptr [7FF877D4C558]; System.SR.get_OperationCanceled()
       mov       [rbp-10],rax
       mov       rdx,[rbp-10]
       mov       r8,[rbp+10]
       mov       r8,[r8]
       mov       rcx,[rbp-8]
       call      qword ptr [7FF877C5E4A8]; System.OperationCanceledException..ctor(System.String, System.Threading.CancellationToken)
       mov       rcx,[rbp-8]
       call      CORINFO_HELP_THROW
       int       3
; Total bytes of code 84
```
```assembly
; System.Threading.SemaphoreSlim.Release(Int32)
       push      rbp
       push      r15
       push      r14
       push      r13
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,40
       lea       rbp,[rsp+70]
       mov       [rbp-50],rsp
       mov       rbx,rcx
       mov       esi,edx
       mov       rcx,[rbx+8]
       cmp       byte ptr [rcx+8],0
       jne       near ptr M10_L14
       test      esi,esi
       jle       near ptr M10_L15
       mov       [rbp-40],rcx
       xor       edx,edx
       mov       [rbp-38],edx
       cmp       byte ptr [rbp-38],0
       jne       short M10_L03
       lea       rdx,[rbp-38]
       call      System.Threading.Monitor.ReliableEnter(System.Object, Boolean ByRef)
       jmp       near ptr M10_L09
M10_L00:
       mov       esi,r14d
       sub       esi,r15d
       jmp       short M10_L07
M10_L01:
       cmp       r13d,esi
       cmovg     r13d,esi
       add       [rbx+34],r13d
       xor       esi,esi
       jmp       short M10_L05
M10_L02:
       mov       rcx,offset MT_System.Threading.SemaphoreFullException
       call      CORINFO_HELP_NEWSFAST
       mov       r14,rax
       mov       rcx,r14
       call      qword ptr [7FF877D0EAF0]
       mov       rcx,r14
       call      CORINFO_HELP_THROW
M10_L03:
       call      qword ptr [7FF8778FE040]
       int       3
M10_L04:
       mov       rcx,[rbx+8]
       call      qword ptr [7FF8778FE178]
       inc       esi
M10_L05:
       cmp       esi,r13d
       jl        short M10_L04
       jmp       short M10_L10
M10_L06:
       dec       r14d
       dec       esi
       mov       r15,[rbx+18]
       mov       rcx,rbx
       mov       rdx,r15
       call      qword ptr [7FF877C5DCF8]
       mov       rcx,r15
       mov       edx,1
       cmp       [rcx],ecx
       call      qword ptr [7FF877D75068]
M10_L07:
       test      esi,esi
       jle       short M10_L11
       cmp       qword ptr [rbx+18],0
       jne       short M10_L06
       jmp       short M10_L11
M10_L08:
       test      edi,edi
       jne       short M10_L12
       test      r14d,r14d
       jle       short M10_L12
       mov       rcx,[rbx+10]
       cmp       [rcx],ecx
       call      qword ptr [7FF877D75938]
       jmp       short M10_L12
M10_L09:
       mov       edi,[rbx+28]
       mov       ecx,[rbx+2C]
       sub       ecx,edi
       cmp       ecx,esi
       jl        near ptr M10_L02
       lea       ecx,[rdi+rsi]
       mov       r14d,ecx
       mov       r15d,[rbx+30]
       cmp       r14d,r15d
       mov       r13d,r15d
       cmovle    r13d,r14d
       sub       r13d,[rbx+34]
       test      r13d,r13d
       jg        near ptr M10_L01
M10_L10:
       cmp       qword ptr [rbx+18],0
       jne       near ptr M10_L00
M10_L11:
       mov       [rbx+28],r14d
       cmp       qword ptr [rbx+10],0
       jne       short M10_L08
M10_L12:
       cmp       byte ptr [rbp-38],0
       je        short M10_L13
       mov       rcx,[rbp-40]
       call      System.Threading.Monitor.Exit(System.Object)
M10_L13:
       mov       eax,edi
       add       rsp,40
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M10_L14:
       mov       rcx,rbx
       call      qword ptr [7FF877A8EE80]
       int       3
M10_L15:
       mov       rcx,offset MT_System.Int32
       call      CORINFO_HELP_NEWSFAST
       mov       r13,rax
       mov       [r13+8],esi
       mov       rcx,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       ecx,17185
       mov       rdx,7FF8777A4000
       call      CORINFO_HELP_STRCNS
       mov       rbx,rax
       call      qword ptr [7FF877D4CA68]
       mov       r9,rax
       mov       rdx,rbx
       mov       r8,r13
       mov       rcx,rsi
       call      qword ptr [7FF8779AD4A0]
       mov       rcx,rsi
       call      CORINFO_HELP_THROW
       int       3
       push      rbp
       push      r15
       push      r14
       push      r13
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,30
       mov       rbp,[rcx+20]
       mov       [rsp+20],rbp
       lea       rbp,[rbp+70]
       cmp       byte ptr [rbp-38],0
       je        short M10_L16
       mov       rcx,[rbp-40]
       call      System.Threading.Monitor.Exit(System.Object)
M10_L16:
       nop
       add       rsp,30
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
; Total bytes of code 503
```
```assembly
; System.Threading.CancellationToken.ThrowIfCancellationRequested()
       sub       rsp,28
       cmp       qword ptr [rcx],0
       je        short M11_L00
       mov       rax,[rcx]
       cmp       dword ptr [rax+20],0
       jne       short M11_L01
M11_L00:
       add       rsp,28
       ret
M11_L01:
       call      qword ptr [7FF877A85740]; System.Threading.CancellationToken.ThrowOperationCanceledException()
       int       3
; Total bytes of code 31
```
```assembly
; System.GC.SuppressFinalize(System.Object)
       sub       rsp,28
       test      rcx,rcx
       je        short M12_L00
       add       rsp,28
       jmp       near ptr System.GC._SuppressFinalize(System.Object)
M12_L00:
       mov       ecx,12E9
       mov       rdx,7FF8777A4000
       call      CORINFO_HELP_STRCNS
       mov       rcx,rax
       call      qword ptr [7FF877AB66E8]
       int       3
; Total bytes of code 48
```
**Method was not JITted yet.**
RagnaController.Core.InputLatencyTracker+<>c.<RecordEnqueueLatency>b__30_0(System.String)

## .NET 8.0.26 (8.0.2626.16921), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
```assembly
; RagnaController.Benchmarks.InputEmulationBenchmarks.EmulateKeyChord()
;             _queue.KeyDown(VirtualKey.ControlLeft);
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;             _queue.KeyDown(VirtualKey.ShiftLeft);
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;             _queue.KeyDown(VirtualKey.A);
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;             _queue.KeyUp(VirtualKey.A);
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^
;             _queue.KeyUp(VirtualKey.ShiftLeft);
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;             _queue.KeyUp(VirtualKey.ControlLeft);
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,20
       mov       rbx,rcx
       mov       rsi,[rbx+8]
       cmp       [rsi],sil
       mov       rdi,offset MT_RagnaController.Core.InputCmd
       mov       rcx,rdi
       call      CORINFO_HELP_NEWSFAST
       mov       dword ptr [rax+10],5
       mov       word ptr [rax+1C],0
       mov       dword ptr [rax+14],0A2
       xor       ecx,ecx
       mov       [rax+18],ecx
       mov       [rax+8],rcx
       mov       rcx,rsi
       mov       rdx,rax
       call      qword ptr [7FF877C77780]; RagnaController.Core.InputCommandQueue.Enqueue(RagnaController.Core.InputCmd)
       mov       rsi,[rbx+8]
       cmp       [rsi],sil
       mov       rcx,rdi
       call      CORINFO_HELP_NEWSFAST
       mov       dword ptr [rax+10],5
       mov       word ptr [rax+1C],0
       mov       dword ptr [rax+14],0A0
       xor       ecx,ecx
       mov       [rax+18],ecx
       mov       [rax+8],rcx
       mov       rcx,rsi
       mov       rdx,rax
       call      qword ptr [7FF877C77780]; RagnaController.Core.InputCommandQueue.Enqueue(RagnaController.Core.InputCmd)
       mov       rsi,[rbx+8]
       cmp       [rsi],sil
       mov       rcx,rdi
       call      CORINFO_HELP_NEWSFAST
       mov       dword ptr [rax+10],5
       mov       word ptr [rax+1C],0
       mov       dword ptr [rax+14],41
       xor       ecx,ecx
       mov       [rax+18],ecx
       mov       [rax+8],rcx
       mov       rcx,rsi
       mov       rdx,rax
       call      qword ptr [7FF877C77780]; RagnaController.Core.InputCommandQueue.Enqueue(RagnaController.Core.InputCmd)
       mov       rsi,[rbx+8]
       cmp       [rsi],sil
       mov       rcx,rdi
       call      CORINFO_HELP_NEWSFAST
       mov       dword ptr [rax+10],6
       mov       word ptr [rax+1C],0
       mov       dword ptr [rax+14],41
       xor       ecx,ecx
       mov       [rax+18],ecx
       mov       [rax+8],rcx
       mov       rcx,rsi
       mov       rdx,rax
       call      qword ptr [7FF877C77780]; RagnaController.Core.InputCommandQueue.Enqueue(RagnaController.Core.InputCmd)
       mov       rsi,[rbx+8]
       cmp       [rsi],sil
       mov       rcx,rdi
       call      CORINFO_HELP_NEWSFAST
       mov       dword ptr [rax+10],6
       mov       word ptr [rax+1C],0
       mov       dword ptr [rax+14],0A0
       xor       ecx,ecx
       mov       [rax+18],ecx
       mov       [rax+8],rcx
       mov       rcx,rsi
       mov       rdx,rax
       call      qword ptr [7FF877C77780]; RagnaController.Core.InputCommandQueue.Enqueue(RagnaController.Core.InputCmd)
       mov       rbx,[rbx+8]
       cmp       [rbx],bl
       mov       rcx,rdi
       call      CORINFO_HELP_NEWSFAST
       mov       dword ptr [rax+10],6
       mov       word ptr [rax+1C],0
       mov       dword ptr [rax+14],0A2
       xor       ecx,ecx
       mov       [rax+18],ecx
       mov       [rax+8],rcx
       mov       rcx,rbx
       mov       rdx,rax
       add       rsp,20
       pop       rbx
       pop       rsi
       pop       rdi
       jmp       qword ptr [7FF877C77780]; RagnaController.Core.InputCommandQueue.Enqueue(RagnaController.Core.InputCmd)
; Total bytes of code 362
```
```assembly
; RagnaController.Core.InputCommandQueue.Enqueue(RagnaController.Core.InputCmd)
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,38
       mov       rbx,rcx
       mov       rsi,rdx
       cmp       byte ptr [rbx+84],0
       jne       short M01_L01
       mov       rcx,[rbx+8]
       cmp       byte ptr [rcx+38],0
       jne       short M01_L02
       cmp       dword ptr [rcx+34],80000000
       je        short M01_L00
       mov       rcx,[rbx+8]
       cmp       [rcx],cl
       mov       rdx,rsi
       xor       r9d,r9d
       xor       r8d,r8d
       call      qword ptr [7FF877C7D038]; System.Collections.Concurrent.BlockingCollection`1[[System.__Canon, System.Private.CoreLib]].TryAddWithNoTimeValidation(System.__Canon, Int32, System.Threading.CancellationToken)
       mov       rax,[rbx+40]
       test      rax,rax
       jne       short M01_L03
       mov       rdi,[rbx+30]
       test      rdi,rdi
       jne       short M01_L04
M01_L00:
       add       rsp,38
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       ret
M01_L01:
       mov       rcx,offset MT_System.ObjectDisposedException
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       ecx,723C
       mov       rdx,7FF877C82938
       call      CORINFO_HELP_STRCNS
       mov       rdx,rax
       mov       rcx,rsi
       call      qword ptr [7FF877C7EAF0]
       mov       rcx,rsi
       call      CORINFO_HELP_THROW
M01_L02:
       call      qword ptr [7FF877AAEE80]
       int       3
M01_L03:
       mov       rdx,rsi
       mov       rcx,[rax+8]
       call      qword ptr [rax+18]
       mov       rdi,[rbx+30]
       test      rdi,rdi
       je        short M01_L00
M01_L04:
       mov       rbx,[rbx+38]
       cmp       byte ptr [rdi+0D4],0
       jne       short M01_L00
       lea       rax,[rdi+0D0]
       mov       edx,1
       mov       ecx,edx
       lock xadd [rax],ecx
       inc       ecx
       mov       edx,14F8B589
       mov       eax,edx
       imul      ecx
       mov       eax,edx
       shr       eax,1F
       sar       edx,0C
       add       edx,eax
       imul      edx,0C350
       sub       ecx,edx
       mov       rdx,[rdi+8]
       cmp       ecx,[rdx+8]
       jae       near ptr M01_L10
       xor       eax,eax
       mov       [rdx+rcx*8+10],rax
       mov       rsi,[rdi+28]
       mov       rcx,7FF877C83708
       mov       edx,128
       call      CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE
       mov       rcx,192C8C03F40
       mov       r8,[rcx]
       test      r8,r8
       jne       short M01_L05
       mov       rcx,offset MT_System.Func`2[[System.String, System.Private.CoreLib],[RagnaController.Core.InputLatencyTracker+ControllerLatencyStats, RagnaController]]
       call      CORINFO_HELP_NEWSFAST
       mov       rbp,rax
       mov       rcx,7FF877C83708
       mov       edx,128
       call      CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE
       mov       rdx,192C8C03F38
       mov       rdx,[rdx]
       mov       rcx,rbp
       mov       r8,offset RagnaController.Core.InputLatencyTracker+<>c.<RecordEnqueueLatency>b__30_0(System.String)
       call      qword ptr [7FF877914210]; System.MulticastDelegate.CtorClosed(System.Object, IntPtr)
       mov       rcx,7FF877C83708
       mov       edx,128
       call      CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE
       mov       rcx,192C8C03F40
       mov       rdx,rbp
       call      CORINFO_HELP_ASSIGN_REF
       mov       r8,rbp
M01_L05:
       mov       rcx,rsi
       mov       rdx,rbx
       cmp       [rcx],ecx
       call      qword ptr [7FF877C36310]; System.Collections.Concurrent.ConcurrentDictionary`2[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].GetOrAdd(System.__Canon, System.Func`2<System.__Canon,System.__Canon>)
       mov       [rsp+28],rax
       cmp       [rax],al
       lea       rcx,[rax+30]
       lock add  qword ptr [rcx],0
       lea       rcx,[rax+8]
       lock inc  qword ptr [rcx]
       lea       rcx,[rax+50]
       xor       edx,edx
       xor       eax,eax
       lock cmpxchg [rcx],rdx
       jmp       short M01_L07
M01_L06:
       mov       rcx,[rsp+28]
       lea       rdx,[rcx+50]
       xor       r8d,r8d
       mov       [rsp+30],rax
       lock cmpxchg [rdx],r8
       cmp       rax,[rsp+30]
       je        short M01_L08
       lea       rdx,[rcx+50]
       xor       r8d,r8d
       xor       eax,eax
       lock cmpxchg [rdx],r8
M01_L07:
       test      rax,rax
       jl        short M01_L06
M01_L08:
       lea       rax,[rdi+58]
       mov       ecx,1
       lock xadd [rax],rcx
       inc       rcx
       cmp       rcx,0C350
       jle       short M01_L09
       lea       rax,[rdi+58]
       mov       ecx,0C350
       xchg      rcx,[rax]
M01_L09:
       add       rdi,0C0
       lock inc  qword ptr [rdi]
       jmp       near ptr M01_L00
M01_L10:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 568
```
```assembly
; System.Collections.Concurrent.BlockingCollection`1[[System.__Canon, System.Private.CoreLib]].TryAddWithNoTimeValidation(System.__Canon, Int32, System.Threading.CancellationToken)
       push      rbp
       push      rsi
       push      rbx
       sub       rsp,50
       lea       rbp,[rsp+60]
       mov       [rbp-40],rsp
       mov       [rbp-18],rcx
       mov       [rbp+10],rcx
       mov       [rbp+18],rdx
       mov       [rbp+28],r9
       mov       ebx,r8d
       cmp       byte ptr [rcx+38],0
       jne       near ptr M02_L17
       cmp       qword ptr [rbp+28],0
       je        short M02_L00
       mov       rax,[rbp+28]
       cmp       dword ptr [rax+20],0
       jne       near ptr M02_L16
M02_L00:
       cmp       byte ptr [rcx+38],0
       jne       near ptr M02_L17
       cmp       dword ptr [rcx+34],80000000
       je        near ptr M02_L18
       mov       dword ptr [rbp-1C],1
       cmp       qword ptr [rcx+10],0
       jne       near ptr M02_L19
M02_L01:
       cmp       dword ptr [rbp-1C],0
       je        near ptr M02_L15
       xor       eax,eax
       mov       [rbp-28],eax
       mov       rcx,[rbp+10]
       mov       eax,[rcx+34]
       test      eax,80000000
       jne       near ptr M02_L06
M02_L02:
       lea       rsi,[rcx+34]
       lea       edx,[rax+1]
       mov       [rbp-30],eax
       lock cmpxchg [rsi],edx
       cmp       eax,[rbp-30]
       jne       near ptr M02_L05
       xor       edx,edx
       mov       [rbp-2C],edx
       jmp       near ptr M02_L09
M02_L03:
       mov       r8,[rcx+10]
       mov       rcx,r8
       xor       r8d,r8d
       xor       edx,edx
       cmp       [rcx],ecx
       call      qword ptr [7FF877C7DC20]; System.Threading.SemaphoreSlim.Wait(Int32, System.Threading.CancellationToken)
       mov       [rbp-1C],eax
       cmp       dword ptr [rbp-1C],0
       jne       short M02_L04
       test      ebx,ebx
       je        short M02_L04
       mov       rsi,[rbp+28]
       mov       rcx,[rbp+10]
       mov       rcx,[rcx+28]
       cmp       [rcx],ecx
       call      qword ptr [7FF877C77C18]; System.Threading.CancellationTokenSource.get_Token()
       mov       rdx,rax
       mov       rcx,rsi
       call      qword ptr [7FF877C77E10]; System.Threading.CancellationTokenSource.CreateLinkedTokenSource(System.Threading.CancellationToken, System.Threading.CancellationToken)
       mov       [rbp-38],rax
       mov       rcx,[rbp+10]
       mov       rsi,[rcx+10]
       mov       rcx,[rbp-38]
       cmp       [rcx],ecx
       call      qword ptr [7FF877C77C18]; System.Threading.CancellationTokenSource.get_Token()
       mov       r8,rax
       mov       rcx,rsi
       mov       edx,ebx
       cmp       [rcx],ecx
       call      qword ptr [7FF877C7DC20]; System.Threading.SemaphoreSlim.Wait(Int32, System.Threading.CancellationToken)
       mov       [rbp-1C],eax
M02_L04:
       mov       rcx,rsp
       call      M02_L20
       jmp       near ptr M02_L01
M02_L05:
       lea       rcx,[rbp-28]
       mov       edx,0FFFFFFFF
       call      qword ptr [7FF877C7F210]
       mov       rcx,[rbp+10]
       mov       eax,[rcx+34]
       test      eax,80000000
       je        near ptr M02_L02
M02_L06:
       xor       edx,edx
       mov       [rbp-28],edx
       jmp       short M02_L08
M02_L07:
       lea       rcx,[rbp-28]
       mov       edx,14
       call      qword ptr [7FF877C7F228]; System.Threading.SpinWait.SpinOnceCore(Int32)
       mov       rcx,[rbp+10]
M02_L08:
       cmp       dword ptr [rcx+34],80000000
       jne       short M02_L07
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       call      qword ptr [7FF877D94720]
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FF877AD6718]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M02_L09:
       cmp       qword ptr [rbp+28],0
       je        short M02_L10
       mov       rdx,[rbp+28]
       cmp       dword ptr [rdx+20],0
       jne       short M02_L12
M02_L10:
       mov       rdx,[rcx]
       mov       rax,[rdx+30]
       mov       rax,[rax]
       mov       r11,[rax+30]
       test      r11,r11
       je        short M02_L13
       jmp       short M02_L14
M02_L11:
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       call      qword ptr [7FF877D946C0]
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FF877AD6718]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
M02_L12:
       lea       rcx,[rbp+28]
       call      qword ptr [7FF877AA5740]; System.Threading.CancellationToken.ThrowOperationCanceledException()
       int       3
M02_L13:
       mov       rcx,rdx
       mov       rdx,7FF877D3A3F8
       call      CORINFO_HELP_RUNTIMEHANDLE_CLASS
       mov       r11,rax
M02_L14:
       mov       rcx,[rbp+10]
       mov       rcx,[rcx+8]
       mov       rdx,[rbp+18]
       call      qword ptr [r11]
       mov       [rbp-2C],eax
       cmp       dword ptr [rbp-2C],0
       je        short M02_L11
       mov       rcx,[rbp+10]
       mov       rax,[rcx+18]
       cmp       [rax],al
       mov       rcx,rax
       mov       edx,1
       call      qword ptr [7FF877C7DD40]; System.Threading.SemaphoreSlim.Release(Int32)
       lock dec  dword ptr [rsi]
M02_L15:
       mov       eax,[rbp-1C]
       add       rsp,50
       pop       rbx
       pop       rsi
       pop       rbp
       ret
M02_L16:
       lea       rcx,[rbp+28]
       call      qword ptr [7FF877AA5740]; System.Threading.CancellationToken.ThrowOperationCanceledException()
       int       3
M02_L17:
       call      qword ptr [7FF877AAEE80]
       int       3
M02_L18:
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       call      qword ptr [7FF877D94720]
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FF877AD6718]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
M02_L19:
       xor       r8d,r8d
       mov       [rbp-38],r8
       jmp       near ptr M02_L03
       push      rbp
       push      rsi
       push      rbx
       sub       rsp,30
       mov       rbp,[rcx+20]
       mov       [rsp+20],rbp
       lea       rbp,[rbp+60]
       lea       rcx,[rbp+28]
       call      qword ptr [7FF877AA5728]; System.Threading.CancellationToken.ThrowIfCancellationRequested()
       mov       rcx,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       call      qword ptr [7FF877D946A8]
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FF877AD6718]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M02_L20:
       push      rbp
       push      rsi
       push      rbx
       sub       rsp,30
       mov       rbp,[rcx+20]
       mov       [rsp+20],rbp
       lea       rbp,[rbp+60]
       cmp       qword ptr [rbp-38],0
       je        short M02_L21
       mov       rcx,[rbp-38]
       mov       edx,1
       mov       rax,[rbp-38]
       mov       rax,[rax]
       mov       rax,[rax+40]
       call      qword ptr [rax+28]
       mov       rcx,[rbp-38]
       call      qword ptr [7FF877BF4960]; System.GC.SuppressFinalize(System.Object)
M02_L21:
       nop
       add       rsp,30
       pop       rbx
       pop       rsi
       pop       rbp
       ret
       push      rbp
       push      rsi
       push      rbx
       sub       rsp,30
       mov       rbp,[rcx+20]
       mov       [rsp+20],rbp
       lea       rbp,[rbp+60]
       cmp       dword ptr [rbp-2C],0
       je        short M02_L22
       mov       rcx,[rbp+10]
       mov       rax,[rcx+18]
       cmp       [rax],al
       mov       rcx,rax
       mov       edx,1
       call      qword ptr [7FF877C7DD40]; System.Threading.SemaphoreSlim.Release(Int32)
       jmp       short M02_L23
M02_L22:
       mov       rcx,[rbp+10]
       mov       rdx,[rcx+10]
       test      rdx,rdx
       je        short M02_L23
       mov       rcx,rdx
       mov       edx,1
       call      qword ptr [7FF877C7DD40]; System.Threading.SemaphoreSlim.Release(Int32)
M02_L23:
       mov       rcx,[rbp+10]
       lea       rsi,[rcx+34]
       lock dec  dword ptr [rsi]
       add       rsp,30
       pop       rbx
       pop       rsi
       pop       rbp
       ret
; Total bytes of code 899
```
```assembly
; System.MulticastDelegate.CtorClosed(System.Object, IntPtr)
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rsi,r8
       test      rdx,rdx
       je        short M03_L00
       lea       rcx,[rbx+8]
       call      CORINFO_HELP_ASSIGN_REF
       mov       [rbx+18],rsi
       add       rsp,28
       pop       rbx
       pop       rsi
       ret
M03_L00:
       call      qword ptr [7FF8779141F8]
       int       3
; Total bytes of code 44
```
```assembly
; System.Collections.Concurrent.ConcurrentDictionary`2[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].GetOrAdd(System.__Canon, System.Func`2<System.__Canon,System.__Canon>)
       push      rbp
       push      rbp
       push      rbp
       push      r15
       push      r15
       push      r15
       push      r14
       push      r14
       push      r14
       push      rdi
       push      rdi
       push      rdi
       push      rsi
       push      rsi
       push      rsi
       push      rbx
       push      rbx
       push      rbx
       sub       rsp,58
       sub       rsp,58
       sub       rsp,58
       lea       rbp,[rsp+80]
       lea       rbp,[rsp+80]
       lea       rbp,[rsp+80]
       xor       eax,eax
       xor       eax,eax
       xor       eax,eax
       mov       [rbp-38],rax
       mov       [rbp-38],rax
       mov       [rbp-38],rax
       mov       [rbp-30],rcx
       mov       [rbp-30],rcx
       mov       [rbp-30],rcx
       mov       rsi,rcx
       mov       rsi,rcx
       mov       rsi,rcx
       mov       rbx,rdx
       mov       rbx,rdx
       mov       rbx,rdx
       mov       rdi,r8
       mov       rdi,r8
       mov       rdi,r8
       test      rbx,rbx
       test      rbx,rbx
       test      rbx,rbx
       je        near ptr M04_L04
       je        near ptr M04_L04
       je        near ptr M04_L04
       test      rdi,rdi
       test      rdi,rdi
       test      rdi,rdi
       je        near ptr M04_L05
       je        near ptr M04_L05
       je        near ptr M04_L05
       mov       r14,[rsi+8]
       mov       r14,[rsi+8]
       mov       r14,[rsi+8]
       mov       r15,[r14+8]
       mov       r15,[r14+8]
       mov       r15,[r14+8]
       cmp       byte ptr [rsi+15],0
       cmp       byte ptr [rsi+15],0
       cmp       byte ptr [rsi+15],0
       je        short M04_L02
       je        short M04_L02
       je        short M04_L02
       mov       rcx,rbx
       mov       rcx,rbx
       mov       rcx,rbx
       lea       r11,[7FF94057C0A8]
       lea       r11,[7FF94057C0A8]
       lea       r11,[7FF94057C0A8]
       call      qword ptr [r11]
       call      qword ptr [r11]
       call      qword ptr [r11]
       mov       r15d,eax
       mov       r15d,eax
       mov       r15d,eax
M04_L00:
       mov       rcx,[rsi]
M04_L00:
       mov       rcx,[rsi]
M04_L00:
       mov       rcx,[rsi]
       call      qword ptr [7FF94057C500]
       call      qword ptr [7FF94057C500]
       call      qword ptr [7FF94057C500]
       mov       rcx,rax
       mov       rcx,rax
       mov       rcx,rax
       lea       rdx,[rbp-38]
       lea       rdx,[rbp-38]
       lea       rdx,[rbp-38]
       mov       [rsp+20],rdx
       mov       [rsp+20],rdx
       mov       [rsp+20],rdx
       mov       rdx,r14
       mov       rdx,r14
       mov       rdx,r14
       mov       r8,rbx
       mov       r8,rbx
       mov       r8,rbx
       mov       r9d,r15d
       mov       r9d,r15d
       mov       r9d,r15d
       call      qword ptr [7FF94057D2D0]; Precode of System.Collections.Concurrent.ConcurrentDictionary`2[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].TryGetValueInternal(Tables<System.__Canon,System.__Canon>, System.__Canon, Int32, System.__Canon ByRef)
       call      qword ptr [7FF94057D2D0]; Precode of System.Collections.Concurrent.ConcurrentDictionary`2[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].TryGetValueInternal(Tables<System.__Canon,System.__Canon>, System.__Canon, Int32, System.__Canon ByRef)
       call      qword ptr [7FF94057D2D0]; Precode of System.Collections.Concurrent.ConcurrentDictionary`2[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].TryGetValueInternal(Tables<System.__Canon,System.__Canon>, System.__Canon, Int32, System.__Canon ByRef)
       test      eax,eax
       test      eax,eax
       test      eax,eax
       je        short M04_L03
       je        short M04_L03
       je        short M04_L03
M04_L01:
       mov       rax,[rbp-38]
M04_L01:
       mov       rax,[rbp-38]
M04_L01:
       mov       rax,[rbp-38]
       add       rsp,58
       add       rsp,58
       add       rsp,58
       pop       rbx
       pop       rbx
       pop       rbx
       pop       rsi
       pop       rsi
       pop       rsi
       pop       rdi
       pop       rdi
       pop       rdi
       pop       r14
       pop       r14
       pop       r14
       pop       r15
       pop       r15
       pop       r15
       pop       rbp
       pop       rbp
       pop       rbp
       ret
       ret
       ret
M04_L02:
       mov       rcx,[rsi]
M04_L02:
       mov       rcx,[rsi]
M04_L02:
       mov       rcx,[rsi]
       call      qword ptr [7FF94057C7D8]
       call      qword ptr [7FF94057C7D8]
       call      qword ptr [7FF94057C7D8]
       mov       rcx,r15
       mov       rcx,r15
       mov       rcx,r15
       mov       r11,rax
       mov       r11,rax
       mov       r11,rax
       mov       rdx,rbx
       mov       rdx,rbx
       mov       rdx,rbx
       call      qword ptr [rax]
       call      qword ptr [rax]
       call      qword ptr [rax]
       mov       r15d,eax
       mov       r15d,eax
       mov       r15d,eax
       jmp       short M04_L00
       jmp       short M04_L00
       jmp       short M04_L00
M04_L03:
       mov       byte ptr [rbp-40],1
M04_L03:
       mov       byte ptr [rbp-40],1
M04_L03:
       mov       byte ptr [rbp-40],1
       mov       [rbp-3C],r15d
       mov       [rbp-3C],r15d
       mov       [rbp-3C],r15d
       mov       rdx,rbx
       mov       rdx,rbx
       mov       rdx,rbx
       mov       rcx,[rdi+8]
       mov       rcx,[rdi+8]
       mov       rcx,[rdi+8]
       call      qword ptr [rdi+18]
       call      qword ptr [rdi+18]
       call      qword ptr [rdi+18]
       xor       edx,edx
       xor       edx,edx
       xor       edx,edx
       mov       [rsp+28],edx
       mov       [rsp+28],edx
       mov       [rsp+28],edx
       mov       dword ptr [rsp+30],1
       mov       dword ptr [rsp+30],1
       mov       dword ptr [rsp+30],1
       lea       rdx,[rbp-38]
       lea       rdx,[rbp-38]
       lea       rdx,[rbp-38]
       mov       [rsp+38],rdx
       mov       [rsp+38],rdx
       mov       [rsp+38],rdx
       mov       [rsp+20],rax
       mov       [rsp+20],rax
       mov       [rsp+20],rax
       mov       rdx,r14
       mov       rdx,r14
       mov       rdx,r14
       mov       r8,rbx
       mov       r8,rbx
       mov       r8,rbx
       mov       r9,[rbp-40]
       mov       r9,[rbp-40]
       mov       r9,[rbp-40]
       mov       rcx,rsi
       mov       rcx,rsi
       mov       rcx,rsi
       call      qword ptr [7FF94057D300]; Precode of System.Collections.Concurrent.ConcurrentDictionary`2[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].TryAddInternal(Tables<System.__Canon,System.__Canon>, System.__Canon, System.Nullable`1<Int32>, System.__Canon, Boolean, Boolean, System.__Canon ByRef)
       call      qword ptr [7FF94057D300]; Precode of System.Collections.Concurrent.ConcurrentDictionary`2[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].TryAddInternal(Tables<System.__Canon,System.__Canon>, System.__Canon, System.Nullable`1<Int32>, System.__Canon, Boolean, Boolean, System.__Canon ByRef)
       call      qword ptr [7FF94057D300]; Precode of System.Collections.Concurrent.ConcurrentDictionary`2[[System.__Canon, System.Private.CoreLib],[System.__Canon, System.Private.CoreLib]].TryAddInternal(Tables<System.__Canon,System.__Canon>, System.__Canon, System.Nullable`1<Int32>, System.__Canon, Boolean, Boolean, System.__Canon ByRef)
       jmp       short M04_L01
       jmp       short M04_L01
       jmp       short M04_L01
M04_L04:
       mov       rcx,[7FF94057D710]
M04_L04:
       mov       rcx,[7FF94057D710]
M04_L04:
       mov       rcx,[7FF94057D710]
       mov       rcx,[rcx]
       mov       rcx,[rcx]
       mov       rcx,[rcx]
       call      qword ptr [7FF94057CD50]
       call      qword ptr [7FF94057CD50]
       call      qword ptr [7FF94057CD50]
       int       3
       int       3
       int       3
M04_L05:
       mov       rcx,[7FF94057D858]
M04_L05:
       mov       rcx,[7FF94057D858]
M04_L05:
       mov       rcx,[7FF94057D858]
       mov       rcx,[rcx]
       mov       rcx,[rcx]
       mov       rcx,[rcx]
       call      qword ptr [7FF94057CD50]
       call      qword ptr [7FF94057CD50]
       call      qword ptr [7FF94057CD50]
       int       3
       int       3
       int       3
; Total bytes of code 810
```
```assembly
; System.Threading.SemaphoreSlim.Wait(Int32, System.Threading.CancellationToken)
       push      rbp
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,98
       vzeroupper
       lea       rbp,[rsp+0B0]
       vxorps    xmm4,xmm4,xmm4
       vmovdqa   xmmword ptr [rbp-50],xmm4
       xor       eax,eax
       mov       [rbp-40],rax
       mov       [rbp-80],rsp
       mov       [rbp+10],rcx
       mov       [rbp+20],r8
       mov       ebx,edx
       mov       rdx,[rcx+8]
       cmp       byte ptr [rdx+8],0
       jne       near ptr M05_L24
       cmp       ebx,0FFFFFFFF
       jl        near ptr M05_L25
       cmp       qword ptr [rbp+20],0
       je        short M05_L00
       mov       rdx,[rbp+20]
       cmp       dword ptr [rdx+20],0
       jne       near ptr M05_L26
M05_L00:
       test      ebx,ebx
       jne       short M05_L01
       cmp       dword ptr [rcx+28],0
       je        near ptr M05_L27
M05_L01:
       xor       esi,esi
       cmp       ebx,0FFFFFFFF
       je        short M05_L02
       test      ebx,ebx
       jg        near ptr M05_L28
M05_L02:
       xor       edx,edx
       mov       [rbp-1C],edx
       mov       [rbp-60],rdx
       mov       [rbp-28],edx
       mov       rdx,192D8C006B8
       mov       r8,[rdx]
       mov       r9,[rbp+20]
       test      r9,r9
       jne       short M05_L03
       vxorps    xmm0,xmm0,xmm0
       vmovdqu   xmmword ptr [rbp-50],xmm0
       jmp       short M05_L04
M05_L03:
       xor       edx,edx
       mov       [rsp+20],rdx
       mov       [rsp+28],rdx
       lea       rdx,[rbp-50]
       mov       rcx,r9
       mov       r9,[rbp+10]
       call      qword ptr [7FF877C77DB0]; System.Threading.CancellationTokenSource.Register(System.Delegate, System.Object, System.Threading.SynchronizationContext, System.Threading.ExecutionContext)
M05_L04:
       mov       rdi,[rbp-50]
       mov       [rbp-70],rdi
       mov       rdx,[rbp-48]
       mov       [rbp-58],rdx
       mov       rcx,[rbp+10]
       cmp       dword ptr [rcx+28],0
       je        near ptr M05_L10
M05_L05:
       mov       rax,[rcx+8]
       cmp       byte ptr [rbp-28],0
       jne       short M05_L09
       lea       rdx,[rbp-28]
       mov       rcx,rax
       call      System.Threading.Monitor.ReliableEnter(System.Object, Boolean ByRef)
       mov       rcx,[rbp+10]
       inc       dword ptr [rcx+30]
       cmp       qword ptr [rcx+18],0
       jne       short M05_L08
       xor       eax,eax
       mov       [rbp-68],rax
       cmp       dword ptr [rcx+28],0
       je        near ptr M05_L15
M05_L06:
       mov       rcx,[rbp+10]
       cmp       dword ptr [rcx+28],0
       jle       near ptr M05_L14
       mov       dword ptr [rbp-1C],1
       dec       dword ptr [rcx+28]
M05_L07:
       cmp       qword ptr [rcx+10],0
       jne       near ptr M05_L16
       mov       rdi,[rbp-70]
       jmp       near ptr M05_L17
M05_L08:
       mov       edx,ebx
       mov       r8,[rbp+20]
       call      qword ptr [7FF877C7DCC8]
       mov       [rbp-60],rax
       jmp       near ptr M05_L17
M05_L09:
       call      qword ptr [7FF87791E040]
       int       3
M05_L10:
       xor       edx,edx
       mov       [rbp-30],edx
       jmp       short M05_L12
M05_L11:
       lea       rcx,[rbp-30]
       mov       edx,0FFFFFFFF
       call      qword ptr [7FF877C7F210]
       mov       rcx,[rbp+10]
       cmp       dword ptr [rcx+28],0
       mov       rcx,[rbp+10]
       jne       near ptr M05_L05
M05_L12:
       cmp       dword ptr [rbp-30],8C
       jl        short M05_L11
       jmp       near ptr M05_L05
M05_L13:
       mov       edx,ebx
       mov       r8d,esi
       mov       r9,[rbp+20]
       call      qword ptr [7FF877C7DC38]; System.Threading.SemaphoreSlim.WaitUntilCountOrTimeout(Int32, UInt32, System.Threading.CancellationToken)
       mov       [rbp-1C],eax
       jmp       near ptr M05_L06
M05_L14:
       cmp       qword ptr [rbp-68],0
       je        near ptr M05_L07
       mov       rcx,[rbp-68]
       call      CORINFO_HELP_THROW
M05_L15:
       test      ebx,ebx
       jne       short M05_L13
       xor       edx,edx
       mov       [rbp-34],edx
       jmp       short M05_L21
M05_L16:
       cmp       dword ptr [rcx+28],0
       mov       rdi,[rbp-70]
       jne       short M05_L17
       mov       rcx,[rbp+10]
       mov       rcx,[rcx+10]
       cmp       [rcx],ecx
       call      qword ptr [7FF877D95830]
       nop
M05_L17:
       cmp       byte ptr [rbp-28],0
       je        short M05_L18
       mov       rcx,[rbp+10]
       dec       dword ptr [rcx+30]
       mov       rcx,[rcx+8]
       call      System.Threading.Monitor.Exit(System.Object)
M05_L18:
       test      rdi,rdi
       jne       short M05_L20
M05_L19:
       cmp       qword ptr [rbp-60],0
       jne       short M05_L22
       mov       eax,[rbp-1C]
       add       rsp,98
       pop       rbx
       pop       rsi
       pop       rdi
       pop       rbp
       ret
M05_L20:
       mov       rcx,[rdi+8]
       mov       rdx,[rbp-58]
       mov       r8,rdi
       cmp       [rcx],ecx
       call      qword ptr [7FF877C7EF10]; System.Threading.CancellationTokenSource+Registrations.Unregister(Int64, CallbackNode)
       test      eax,eax
       jne       short M05_L19
       mov       rcx,[rbp-58]
       mov       rdx,rdi
       call      qword ptr [7FF877C7ED30]
       jmp       short M05_L19
M05_L21:
       mov       rcx,rsp
       call      M05_L29
       jmp       short M05_L23
M05_L22:
       mov       rcx,[rbp-60]
       call      qword ptr [7FF877D95020]
       mov       [rbp-40],rax
       lea       rcx,[rbp-40]
       call      qword ptr [7FF877C7F120]
       nop
       add       rsp,98
       pop       rbx
       pop       rsi
       pop       rdi
       pop       rbp
       ret
M05_L23:
       mov       eax,[rbp-34]
       add       rsp,98
       pop       rbx
       pop       rsi
       pop       rdi
       pop       rbp
       ret
M05_L24:
       call      qword ptr [7FF877AAEE80]
       int       3
M05_L25:
       mov       rcx,offset MT_System.Int32
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       [rsi+8],ebx
       mov       rcx,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       ecx,1351
       mov       rdx,7FF8777C4000
       call      CORINFO_HELP_STRCNS
       mov       rdi,rax
       call      qword ptr [7FF877D6D278]
       mov       r9,rax
       mov       rdx,rdi
       mov       r8,rsi
       mov       rcx,rbx
       call      qword ptr [7FF8779CD4A0]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
M05_L26:
       lea       rcx,[rbp+20]
       call      qword ptr [7FF877AA5740]; System.Threading.CancellationToken.ThrowOperationCanceledException()
       int       3
M05_L27:
       xor       eax,eax
       add       rsp,98
       pop       rbx
       pop       rsi
       pop       rdi
       pop       rbp
       ret
M05_L28:
       call      System.Environment.get_TickCount()
       mov       esi,eax
       jmp       near ptr M05_L02
       push      rbp
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,38
       vzeroupper
       mov       rbp,[rcx+30]
       mov       [rsp+30],rbp
       lea       rbp,[rbp+0B0]
       mov       [rbp-68],rdx
       lea       rax,[M05_L06]
       add       rsp,38
       pop       rbx
       pop       rsi
       pop       rdi
       pop       rbp
       ret
M05_L29:
       push      rbp
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,38
       vzeroupper
       mov       rbp,[rcx+30]
       mov       [rsp+30],rbp
       lea       rbp,[rbp+0B0]
       cmp       byte ptr [rbp-28],0
       je        short M05_L30
       mov       rcx,[rbp+10]
       dec       dword ptr [rcx+30]
       mov       rcx,[rcx+8]
       call      System.Threading.Monitor.Exit(System.Object)
M05_L30:
       mov       rdi,[rbp-70]
       test      rdi,rdi
       je        short M05_L31
       mov       rcx,[rdi+8]
       mov       rdx,[rbp-58]
       mov       r8,rdi
       cmp       [rcx],ecx
       call      qword ptr [7FF877C7EF10]; System.Threading.CancellationTokenSource+Registrations.Unregister(Int64, CallbackNode)
       test      eax,eax
       jne       short M05_L31
       mov       rcx,[rbp-58]
       mov       rdx,rdi
       call      qword ptr [7FF877C7ED30]
M05_L31:
       nop
       add       rsp,38
       pop       rbx
       pop       rsi
       pop       rdi
       pop       rbp
       ret
; Total bytes of code 925
```
```assembly
; System.Threading.CancellationTokenSource.get_Token()
       sub       rsp,28
       cmp       byte ptr [rcx+24],0
       jne       short M06_L00
       mov       rax,rcx
       add       rsp,28
       ret
M06_L00:
       mov       ecx,46
       call      qword ptr [7FF8B04B6988]
       int       3
; Total bytes of code 30
```
```assembly
; System.Threading.CancellationTokenSource.CreateLinkedTokenSource(System.Threading.CancellationToken, System.Threading.CancellationToken)
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,40
       xor       eax,eax
       mov       [rsp+38],rax
       mov       rbx,rcx
       mov       rsi,rdx
       test      rbx,rbx
       je        short M07_L00
       test      rsi,rsi
       je        short M07_L01
       call      qword ptr [7FF8B04AC500]
       mov       rdi,rax
       mov       rcx,rdi
       mov       rdx,rbx
       mov       r8,rsi
       call      qword ptr [7FF8B04B8FF8]
       mov       rax,rdi
       add       rsp,40
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M07_L00:
       mov       rcx,rsi
       call      qword ptr [7FF8B04B8FE0]
       nop
       add       rsp,40
       pop       rbx
       pop       rsi
       pop       rdi
       ret
M07_L01:
       call      qword ptr [7FF8B04AC4F8]
       mov       rsi,rax
       mov       [rsp+38],rbx
       call      qword ptr [7FF8B04A4310]
       mov       r8,[rax+620]
       xor       edx,edx
       mov       [rsp+20],edx
       mov       [rsp+28],edx
       lea       rdx,[rsi+28]
       lea       rcx,[rsp+38]
       mov       r9,rsi
       call      qword ptr [7FF8B04B8F40]; Precode of System.Threading.CancellationToken.Register(System.Delegate, System.Object, Boolean, Boolean)
       mov       rax,rsi
       add       rsp,40
       pop       rbx
       pop       rsi
       pop       rdi
       ret
; Total bytes of code 149
```
```assembly
; System.Threading.SpinWait.SpinOnceCore(Int32)
       push      rbp
       push      r15
       push      r14
       push      r13
       push      r12
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,78
       lea       rbp,[rsp+0B0]
       mov       rbx,rcx
       mov       esi,edx
       mov       [rbp+10],rbx
       mov       edi,[rbx]
       cmp       edi,0A
       jl        short M08_L01
       cmp       edi,esi
       jl        short M08_L00
       test      esi,esi
       jge       short M08_L05
M08_L00:
       lea       eax,[rdi-0A]
       test      al,1
       je        short M08_L05
M08_L01:
       call      qword ptr [7FF8B04A3E60]
       cmp       dword ptr [rax+0A54],1
       je        short M08_L05
       call      qword ptr [7FF8B04B8CF0]; System.Threading.Thread.get_OptimalMaxSpinWaitsPerSpinIteration()
       mov       rbx,[rbp+10]
       cmp       dword ptr [rbx],1E
       jle       near ptr M08_L08
M08_L02:
       mov       ecx,eax
       call      qword ptr [7FF8B04B8C38]; System.Threading.Thread.SpinWaitInternal(Int32)
M08_L03:
       mov       rcx,rbx
       mov       eax,[rcx]
       cmp       eax,7FFFFFFF
       je        near ptr M08_L13
       inc       eax
M08_L04:
       mov       [rbx],eax
       add       rsp,78
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M08_L05:
       cmp       edi,esi
       jl        short M08_L06
       test      esi,esi
       jge       near ptr M08_L11
M08_L06:
       cmp       edi,0A
       jl        near ptr M08_L12
       add       edi,0FFFFFFF6
       mov       ecx,edi
       shr       ecx,1F
       add       ecx,edi
       sar       ecx,1
M08_L07:
       mov       edx,66666667
       mov       eax,edx
       imul      ecx
       mov       eax,edx
       shr       eax,1F
       sar       edx,1
       add       eax,edx
       lea       eax,[rax+rax*4]
       sub       ecx,eax
       cmp       ecx,4
       je        short M08_L09
       lea       rcx,[rbp-90]
       call      qword ptr [7FF8B04A3D68]; CORINFO_HELP_JIT_PINVOKE_BEGIN
       mov       rax,[7FF8B04CB9D8]
       call      qword ptr [rax]
       lea       rcx,[rbp-90]
       call      qword ptr [7FF8B04A3D70]; CORINFO_HELP_JIT_PINVOKE_END
       mov       rbx,[rbp+10]
       jmp       near ptr M08_L03
M08_L08:
       mov       ecx,[rbx]
       mov       edx,1
       shl       edx,cl
       cmp       edx,eax
       jge       near ptr M08_L02
       jmp       short M08_L10
M08_L09:
       xor       ecx,ecx
       call      qword ptr [7FF8B04B8D40]; Precode of System.Threading.Thread.Sleep(Int32)
       mov       rbx,[rbp+10]
       jmp       near ptr M08_L03
M08_L10:
       mov       ecx,[rbx]
       mov       eax,1
       shl       eax,cl
       jmp       near ptr M08_L02
M08_L11:
       mov       ecx,1
       call      qword ptr [7FF8B04B8D40]; Precode of System.Threading.Thread.Sleep(Int32)
       mov       rbx,[rbp+10]
       jmp       near ptr M08_L03
M08_L12:
       mov       ecx,edi
       jmp       near ptr M08_L07
M08_L13:
       mov       eax,0A
       jmp       near ptr M08_L04
; Total bytes of code 326
```
```assembly
; System.Threading.CancellationToken.ThrowOperationCanceledException()
       push      rbp
       sub       rsp,30
       lea       rbp,[rsp+30]
       xor       eax,eax
       mov       [rbp-8],rax
       mov       [rbp-10],rax
       mov       [rbp+10],rcx
       mov       rcx,offset MT_System.OperationCanceledException
       call      CORINFO_HELP_NEWSFAST
       mov       [rbp-8],rax
       call      qword ptr [7FF877D6CD50]; System.SR.get_OperationCanceled()
       mov       [rbp-10],rax
       mov       rdx,[rbp-10]
       mov       r8,[rbp+10]
       mov       r8,[r8]
       mov       rcx,[rbp-8]
       call      qword ptr [7FF877C7E4A8]; System.OperationCanceledException..ctor(System.String, System.Threading.CancellationToken)
       mov       rcx,[rbp-8]
       call      CORINFO_HELP_THROW
       int       3
; Total bytes of code 84
```
```assembly
; System.Threading.SemaphoreSlim.Release(Int32)
       push      rbp
       push      r15
       push      r14
       push      r13
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,40
       lea       rbp,[rsp+70]
       mov       [rbp-50],rsp
       mov       rbx,rcx
       mov       esi,edx
       mov       rcx,[rbx+8]
       cmp       byte ptr [rcx+8],0
       jne       near ptr M10_L14
       test      esi,esi
       jle       near ptr M10_L15
       mov       [rbp-40],rcx
       xor       edx,edx
       mov       [rbp-38],edx
       cmp       byte ptr [rbp-38],0
       jne       short M10_L03
       lea       rdx,[rbp-38]
       call      System.Threading.Monitor.ReliableEnter(System.Object, Boolean ByRef)
       jmp       near ptr M10_L09
M10_L00:
       mov       esi,r14d
       sub       esi,r15d
       jmp       short M10_L07
M10_L01:
       cmp       r13d,esi
       cmovg     r13d,esi
       add       [rbx+34],r13d
       xor       esi,esi
       jmp       short M10_L05
M10_L02:
       mov       rcx,offset MT_System.Threading.SemaphoreFullException
       call      CORINFO_HELP_NEWSFAST
       mov       r14,rax
       mov       rcx,r14
       call      qword ptr [7FF877D2EAF0]
       mov       rcx,r14
       call      CORINFO_HELP_THROW
M10_L03:
       call      qword ptr [7FF87791E040]
       int       3
M10_L04:
       mov       rcx,[rbx+8]
       call      qword ptr [7FF87791E178]
       inc       esi
M10_L05:
       cmp       esi,r13d
       jl        short M10_L04
       jmp       short M10_L10
M10_L06:
       dec       r14d
       dec       esi
       mov       r15,[rbx+18]
       mov       rcx,rbx
       mov       rdx,r15
       call      qword ptr [7FF877C7DCF8]
       mov       rcx,r15
       mov       edx,1
       cmp       [rcx],ecx
       call      qword ptr [7FF877D94F78]
M10_L07:
       test      esi,esi
       jle       short M10_L11
       cmp       qword ptr [rbx+18],0
       jne       short M10_L06
       jmp       short M10_L11
M10_L08:
       test      edi,edi
       jne       short M10_L12
       test      r14d,r14d
       jle       short M10_L12
       mov       rcx,[rbx+10]
       cmp       [rcx],ecx
       call      qword ptr [7FF877D95848]
       jmp       short M10_L12
M10_L09:
       mov       edi,[rbx+28]
       mov       ecx,[rbx+2C]
       sub       ecx,edi
       cmp       ecx,esi
       jl        near ptr M10_L02
       lea       ecx,[rdi+rsi]
       mov       r14d,ecx
       mov       r15d,[rbx+30]
       cmp       r14d,r15d
       mov       r13d,r15d
       cmovle    r13d,r14d
       sub       r13d,[rbx+34]
       test      r13d,r13d
       jg        near ptr M10_L01
M10_L10:
       cmp       qword ptr [rbx+18],0
       jne       near ptr M10_L00
M10_L11:
       mov       [rbx+28],r14d
       cmp       qword ptr [rbx+10],0
       jne       short M10_L08
M10_L12:
       cmp       byte ptr [rbp-38],0
       je        short M10_L13
       mov       rcx,[rbp-40]
       call      System.Threading.Monitor.Exit(System.Object)
M10_L13:
       mov       eax,edi
       add       rsp,40
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M10_L14:
       mov       rcx,rbx
       call      qword ptr [7FF877AAEE80]
       int       3
M10_L15:
       mov       rcx,offset MT_System.Int32
       call      CORINFO_HELP_NEWSFAST
       mov       r13,rax
       mov       [r13+8],esi
       mov       rcx,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       mov       ecx,17185
       mov       rdx,7FF8777C4000
       call      CORINFO_HELP_STRCNS
       mov       rbx,rax
       call      qword ptr [7FF877D6D260]
       mov       r9,rax
       mov       rdx,rbx
       mov       r8,r13
       mov       rcx,rsi
       call      qword ptr [7FF8779CD4A0]
       mov       rcx,rsi
       call      CORINFO_HELP_THROW
       int       3
       push      rbp
       push      r15
       push      r14
       push      r13
       push      rdi
       push      rsi
       push      rbx
       sub       rsp,30
       mov       rbp,[rcx+20]
       mov       [rsp+20],rbp
       lea       rbp,[rbp+70]
       cmp       byte ptr [rbp-38],0
       je        short M10_L16
       mov       rcx,[rbp-40]
       call      System.Threading.Monitor.Exit(System.Object)
M10_L16:
       nop
       add       rsp,30
       pop       rbx
       pop       rsi
       pop       rdi
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
; Total bytes of code 503
```
```assembly
; System.Threading.CancellationToken.ThrowIfCancellationRequested()
       sub       rsp,28
       cmp       qword ptr [rcx],0
       je        short M11_L00
       mov       rax,[rcx]
       cmp       dword ptr [rax+20],0
       jne       short M11_L01
M11_L00:
       add       rsp,28
       ret
M11_L01:
       call      qword ptr [7FF877AA5740]; System.Threading.CancellationToken.ThrowOperationCanceledException()
       int       3
; Total bytes of code 31
```
```assembly
; System.GC.SuppressFinalize(System.Object)
       sub       rsp,28
       test      rcx,rcx
       je        short M12_L00
       add       rsp,28
       jmp       near ptr System.GC._SuppressFinalize(System.Object)
M12_L00:
       mov       ecx,12E9
       mov       rdx,7FF8777C4000
       call      CORINFO_HELP_STRCNS
       mov       rcx,rax
       call      qword ptr [7FF877AD66E8]
       int       3
; Total bytes of code 48
```
**Method was not JITted yet.**
RagnaController.Core.InputLatencyTracker+<>c.<RecordEnqueueLatency>b__30_0(System.String)

