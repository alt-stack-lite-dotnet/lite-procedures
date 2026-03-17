# Benchmarks

Описание наборов бенчмарков Lite.Procedures и инструкции по запуску.

## Запуск

Из корня репозитория:

```bash
# Все бенчмарки (консольный проект)
dotnet run -c Release --project benchmark/Lite.Procedures.Benchmark/Lite.Procedures.Benchmark.csproj

# Только выбранный класс (например, Async pipeline)
dotnet run -c Release --project benchmark/Lite.Procedures.Benchmark/Lite.Procedures.Benchmark.csproj -- --filter "*AsyncProcedurePipelineBenchmarks*"

# Бенчмарки в контексте ASP.NET Core приложения
dotnet run -c Release --project benchmark/Lite.Procedures.Benchmark.AspNet/Lite.Procedures.Benchmark.AspNet.csproj
```

Результаты (Markdown, HTML, CSV) пишутся в  
`benchmark/Lite.Procedures.Benchmark/bin/Release/net10.0/BenchmarkDotNet.Artifacts/results/`  
и в аналогичную папку для ASP.NET проекта.

---

## 1. Консольные бенчмарки (Lite.Procedures.Benchmark)

### 1.1 AsyncProcedurePipelineBenchmarks

Измеряет вызов **асинхронного пайплайна** с разным числом и типом интерцепторов.

| Сценарий | Описание |
|----------|----------|
| NoInterceptors | Базовый вызов процедуры без интерцепторов |
| OneInterceptor … TenInterceptors | Цепочка из N no-op async-интерцепторов |
| FiveInterceptors_MixedSyncAsync | Смесь sync/async интерцепторов |
| ShortCircuit_First / Middle / Last | Ранний выход из цепочки (результат от интерцептора) |
| Fault_First / Middle | Интерцептор возвращает ошибку |
| ProcedureThrows_ThreeInterceptors | Процедура бросает исключение |
| RecoveryAfterThrow | Интерцептор после процедуры обрабатывает исключение |

**Метрики:** время на один вызов `InvokeAsync`, аллокации (если включён MemoryDiagnoser).

---

### 1.2 SyncProcedurePipelineBenchmarks

То же для **синхронного пайплайна**: NoInterceptors, 3/5 интерцепторов, short-circuit, fault, procedure throws.

---

### 1.3 DiResolveBenchmarks

Горячий путь DI: разрешение пайплайна из контейнера и вызов.

| Метод | Описание |
|-------|----------|
| DirectReference | Один раз разрешили пайплайн, многократно вызываем (кеш ссылки) |
| ResolveAndInvoke | Каждый раз GetRequiredService + InvokeAsync |
| ResolveFromScope_AndInvoke | CreateAsyncScope, разрешение из scope + вызов |

---

### 1.4 RealWorldInterceptorsBenchmarks

Реалистичная цепочка: процедура `CreatePlayer` с интерцепторами логирования, валидации, метрик, обработки ошибок.

| Сценарий | Цепочка |
|----------|---------|
| NoInterceptors_ValidArgs | Только процедура |
| WithLogging_ValidArgs | Logging |
| WithValidation_ValidArgs / InvalidArgs_ShortCircuit | Validation, short-circuit при невалидных данных |
| WithLoggingAndValidation_ValidArgs | Logging + Validation |
| FullStack_ValidArgs / InvalidArgs_ShortCircuit | Logging + Validation + Metrics + ErrorHandling |

---

### 1.5 LiteProceduresVsMediatRBenchmarks

Сравнение с условным «MediatR» (fake): Lite без интерцепторов, Lite с тремя интерцепторами, MediatR без/с тремя behaviors.  
Тип возврата разный (OneOf vs Task&lt;T&gt;), сравнение ориентировочное.

---

### 1.6 LiteProceduresVsMessagePipeBenchmarks

Сравнение с условным «MessagePipe» (fake): Lite с тремя интерцепторами vs MessagePipe с тремя фильтрами.

---

### 1.7 ConcurrentBenchmarks

Параллельные вызовы одного пайплайна с разным числом потоков (Params: 1, 4, 8, 16).  
Метрика — время на выполнение фиксированного числа вызовов (1000 на поток) всеми потоками.

---

## 2. ASP.NET Core бенчмарки (Lite.Procedures.Benchmark.AspNet)

Бенчмарки в контексте **реального веб-приложения**: поднимается минимальный ASP.NET Core хост с зарегистрированными Lite.Procedures, затем измеряется:

- **ResolveFromRequestServices_AndInvoke** — разрешение пайплайна из `HttpContext.RequestServices` (как в middleware/controller) и вызов.
- **DirectPipelineInvoke** — вызов того же пайплайна по прямой ссылке (как baseline).
- **HttpGet_MinimalApi** — полный HTTP roundtrip до minimal API, который внутри вызывает процедуру через DI.

Окружение: тот же DI, те же регистрации процедур и интерцепторов, что и в типичном ASP.NET приложении.  
Запуск: см. команду выше для проекта `Lite.Procedures.Benchmark.AspNet`.

---

## Результаты (пример таблицы)

После прогона можно вставить сюда сводку из сгенерированного `*-report-github.md`. Пример формата:

| Benchmark | Mean | Error | Allocated |
|-----------|-----:|------:|----------:|
| NoInterceptors | XXX μs | X.XX μs | XXX B |
| ThreeInterceptors | XXX μs | X.XX μs | XXX B |
| … | | | |

Конкретные числа зависят от железа и версии .NET; для воспроизводимости указывайте в отчёте: ОС, CPU, .NET SDK/Runtime.
