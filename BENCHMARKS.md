# Lite.Procedures — бенчмарки

Реальные прогоны BenchmarkDotNet, .NET 10, MemoryDiagnoser. **Акцент: скорость (Mean) и память (Allocated).**

---

## 1. Lite vs MediatR — один запрос (KillerComparisonBenchmarks)

Один и тот же сценарий: echo handler/процедура, 0 или 3 слоя (interceptors vs pipeline behaviors). Реальный MediatR из NuGet.

| Method                 | Mean      | Allocated |
|------------------------|----------:|----------:|
| **Lite_NoInterceptors**    | **88.75 ns**  | **0 B**   |
| **Lite_ThreeInterceptors** | **147.24 ns** | **0 B**   |
| MediatR_NoBehaviors    | 57.49 ns  | 224 B     |
| MediatR_ThreeBehaviors | 222.16 ns | **968 B** |

**Итог:** при трёх слоях Lite **≈1.5× быстрее** (147 ns vs 222 ns) и **0 аллокаций** против **968 B** у MediatR.

---

## 2. Lite vs MediatR — 10 000 запросов (Per10kRequestsBenchmarks)

Оба с тремя слоями.

| Method                | Mean     | Allocated   |
|-----------------------|---------:|------------:|
| **Lite_10k_Requests**   | **2.155 ms** | **0 B**     |
| MediatR_10k_Requests  | 2.291 ms | **9 680 000 B (~9.25 MB)** |

**Итог:** на 10k вызовов Lite не аллоцирует; MediatR — **~9.25 MB** мусора. Нулевая нагрузка на GC.

---

## 3. Lite vs MessagePipe (MessagePipeComparisonBenchmarks)

Lite с **сгенерированным** пайплайном (3 интерцептора) vs MessagePipe (0 или 3 фильтра). Реальный NuGet.

Запуск: `--filter "*MessagePipeComparison*"` (перед этим закрой все процессы Lite.Procedures.Benchmark). После прогона вставь сюда таблицу из `*MessagePipeComparisonBenchmarks-report-github.md` (Mean, Allocated). Отчёты пишутся в `BenchmarkDotNet.Artifacts/results/` в корне репо или в каталоге сборки.

---

## 4. Сгенерированный пайплайн vs ручная сборка (GeneratedVsManualPipelineBenchmarks)

Source generator эмитит класс пайплайна с фиксированной цепочкой; ручная сборка — `new AsyncProcedurePipeline(procedure, interceptors[])`.

| Method                      | Mean      | Allocated |
|----------------------------|----------:|----------:|
| **Generated_NoInterceptors**   | **38.42 ns**  | **0 B**   |
| Manual_NoInterceptors       | 106.08 ns | 0 B       |
| **Generated_ThreeInterceptors**| **90.95 ns** | **0 B**   |
| Manual_ThreeInterceptors    | 146.38 ns | 0 B       |

**Итог:** кодоген **≈2.8× быстрее** без интерцепторов (38 ns vs 106 ns) и **≈1.6× быстрее** с тремя (91 ns vs 146 ns). Аллокации у обоих 0 B. Сгенерированный пайплайн выигрывает за счёт развёрнутой цепочки вызовов без массива и лишних проверок.

---

## 5. Async pipeline — цепочка интерцепторов (AsyncProcedurePipelineBenchmarks)

| Method                            | Mean        | Allocated |
|-----------------------------------|------------:|----------:|
| NoInterceptors                    | 80.60 ns    | 0 B       |
| OneInterceptor                    | 121.61 ns   | 0 B       |
| ThreeInterceptors                 | 184.20 ns   | 0 B       |
| FiveInterceptors                  | 242.59 ns   | 0 B       |
| TenInterceptors                   | 363.66 ns   | 0 B       |
| FiveInterceptors_MixedSyncAsync   | 215.72 ns   | 0 B       |
| ShortCircuit_First                | 114.02 ns   | 0 B       |
| ShortCircuit_Middle               | 100.49 ns   | 0 B       |
| ShortCircuit_Last                 | 127.25 ns   | 0 B       |
| Fault_First                       | 81.03 ns    | 0 B       |
| Fault_Middle                      | 102.23 ns   | 0 B       |
| ProcedureThrows_ThreeInterceptors | 1,528.94 ns | 200 B     |
| RecoveryAfterThrow                | 1,640.51 ns | 200 B     |

**Память:** 0 B на всём горячем пути; 200 B только при throw (исключение).

---

## 6. Sync pipeline (SyncProcedurePipelineBenchmarks)

| Method            | Mean        | Allocated |
|-------------------|------------:|----------:|
| NoInterceptors    | 14.80 ns    | 0 B       |
| ThreeInterceptors | 20.98 ns    | 0 B       |
| FiveInterceptors  | 26.74 ns    | 0 B       |
| ShortCircuit      | 30.71 ns    | 0 B       |
| Fault             | 31.65 ns    | 0 B       |
| ProcedureThrows   | 1,275.02 ns | 200 B     |

**Память:** 0 B в норме; 200 B только при исключении.

---

## 7. Реальный стек интерцепторов (RealWorldInterceptorsBenchmarks)

CreatePlayer: логирование, валидация, метрики, обработка ошибок.

| Method                                  | Mean      | Allocated |
|-----------------------------------------|----------:|----------:|
| NoInterceptors_ValidArgs                | 127.76 ns | 40 B      |
| WithLogging_ValidArgs                   | 151.55 ns | 40 B      |
| WithValidation_ValidArgs               | 147.81 ns | 40 B      |
| WithValidation_InvalidArgs_ShortCircuit | 96.38 ns  | 128 B     |
| WithLoggingAndValidation_ValidArgs      | 174.81 ns | 40 B      |
| FullStack_ValidArgs                     | 218.05 ns | 40 B      |
| FullStack_InvalidArgs_ShortCircuit      | 186.68 ns | 128 B     |

**Память:** 40 B на валидном пути; 128 B при short-circuit (ошибки валидации).

---

## 8. Параллельные вызовы (ConcurrentBenchmarks)

1000 вызовов на поток.

| Method                             | ThreadCount | Mean     | Allocated |
|------------------------------------|-------------|---------:|----------:|
| Concurrent_Invoke                  | 1           | 201.4 μs | 408 B     |
| Concurrent_Invoke_WithCancellation | 1           | 212.5 μs | 503 B     |
| Concurrent_Invoke                  | 4           | 254.2 μs | 1,128 B   |
| Concurrent_Invoke_WithCancellation | 4           | 269.9 μs | 1,032 B   |
| Concurrent_Invoke                  | 8           | 367.0 μs | 1,992 B   |
| Concurrent_Invoke_WithCancellation | 8           | 363.3 μs | 1,640 B   |
| Concurrent_Invoke                  | 16          | 460.5 μs | 3,720 B   |
| Concurrent_Invoke_WithCancellation | 16          | 440.0 μs | 2,856 B   |

---

## 9. Lite vs MediatR (альтернативный прогон, LiteProceduresVsMediatRBenchmarks)

| Method                 | Mean       | Allocated |
|------------------------|-----------:|----------:|
| Lite_NoInterceptors    | 81.56 ns   | 0 B       |
| Lite_ThreeInterceptors | 136.38 ns  | 0 B       |
| MediatR_NoBehaviors    | 4.03 ns    | 72 B      |
| MediatR_ThreeBehaviors | 31.30 ns   | 320 B     |

*Другой сетап (кеш/регистрация). Аллокации: Lite 0 B, MediatR 72–320 B.*

---

## 10. Структуры vs классы (StructVsClassBenchmarks)

Lite с `SmallStruct`/`LargeStruct` (0 B на аргументе) vs MediatR с class-запросами (аллокация на каждый Send).

Запуск: `--filter "*StructVsClass*"`. Вставьте таблицу из `*StructVsClassBenchmarks-report-github.md`.

---

## Сводка: память и скорость

| Сценарий              | Lite (Mean)   | Lite (Allocated) | MediatR (Allocated) |
|-----------------------|---------------|------------------|----------------------|
| Один вызов, 3 слоя    | 147.24 ns     | **0 B**          | 968 B                |
| 10k вызовов, 3 слоя   | 2.155 ms      | **0 B**          | ~9.25 MB             |
| Async 3 interceptors  | 184.20 ns     | **0 B**          | —                    |
| Sync 3 interceptors   | 20.98 ns      | **0 B**          | —                    |
| RealWorld full stack  | 218.05 ns     | 40 B             | —                    |

**Выводы:** на горячем пути **0 аллокаций**; при 10k запросов MediatR даёт **~9.25 MB**, Lite — **0 B**. **Кодоген быстрее ручной сборки в 1.6–2.8×** (см. §4). Полное сравнение с MessagePipe — запусти бенчмарк и подставь таблицу из отчёта (см. ниже).

---

## Запуск бенчмарков

Из корня репозитория:

```bash
dotnet run -c Release --project benchmark/Lite.Procedures.Benchmark/Lite.Procedures.Benchmark.csproj -- --filter "*Killer*"
dotnet run -c Release --project benchmark/Lite.Procedures.Benchmark/Lite.Procedures.Benchmark.csproj -- --filter "*Per10k*"
dotnet run -c Release --project benchmark/Lite.Procedures.Benchmark/Lite.Procedures.Benchmark.csproj -- --filter "*MessagePipeComparison*"
dotnet run -c Release --project benchmark/Lite.Procedures.Benchmark/Lite.Procedures.Benchmark.csproj -- --filter "*GeneratedVsManual*"
dotnet run -c Release --project benchmark/Lite.Procedures.Benchmark/Lite.Procedures.Benchmark.csproj -- --filter "*StructVsClass*"
dotnet run -c Release --project benchmark/Lite.Procedures.Benchmark/Lite.Procedures.Benchmark.csproj -- --filter "*AsyncProcedurePipeline*"
dotnet run -c Release --project benchmark/Lite.Procedures.Benchmark/Lite.Procedures.Benchmark.csproj -- --filter "*SyncProcedurePipeline*"
dotnet run -c Release --project benchmark/Lite.Procedures.Benchmark/Lite.Procedures.Benchmark.csproj -- --filter "*RealWorld*"
dotnet run -c Release --project benchmark/Lite.Procedures.Benchmark/Lite.Procedures.Benchmark.csproj -- --filter "*Concurrent*"
```

Отчёты: `benchmark/Lite.Procedures.Benchmark/bin/Release/net10.0/BenchmarkDotNet.Artifacts/results/` или в корне репо `BenchmarkDotNet.Artifacts/results/`. **Перед запуском закрой все процессы Lite.Procedures.Benchmark**, иначе сборка упадёт из‑за блокировки exe.
