# Lycoris.Autofac.Extensions

Autofac 封装扩展库，支持特性自动注册、手动注册、AOP 拦截器、程序集扫描、异步任务执行，兼容 Autofac 原生 Module。

[![NuGet](https://img.shields.io/nuget/v/Lycoris.Autofac.Extensions.svg)](https://www.nuget.org/packages/Lycoris.Autofac.Extensions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## 功能

- **特性自动注册** — 通过 `[AutofacRegister]` 标注类即可自动注册，支持 `Transient`、`Scoped`、`Singleton` 三种生命周期
- **手动注册** — 继承 `AutofacRegisterModule` 在 `ModuleRegister` 中手动注册服务
- **Keyed 服务** — 支持 Autofac 8.x 的 `Keyed` 服务注册（任意对象作为键）
- **多实现解析** — 同一接口多个实现通过 `IAutofacMultipleService` 按名称解析
- **AOP 拦截器** — 基于 Castle.Core 的拦截器，支持同步/异步拦截、接口/类拦截、全局/模块/服务级拦截、拦截器排除
- **程序集扫描** — 按基类、接口、命名约定批量注册，支持多次调用配置不同扫描规则
- **异步任务执行器** — `IAsyncTaskExecutor` 在后台运行任务，支持延迟执行和自定义解析策略
- **便捷注册** — 生命周期参数化注册、条件注册 `RegisterIf`、拦截器一键注册启用
- **属性注入** — 支持 Autofac 原生属性注入
- **原生 Module 兼容** — 可直接添加 Autofac 原生 `Module`

## 安装

```bash
dotnet add package Lycoris.Autofac.Extensions
```

依赖：.NET 8.0+、Autofac 8.3.0+、Castle.Core.AsyncInterceptor 2.1.0+

## 快速开始

```csharp
var builder = WebApplication.CreateBuilder(args);

// 替换系统 DI 容器为 Autofac
builder.UseAutofacExtensions(opt =>
{
    // 启用多实现服务解析（如需使用 IAutofacMultipleService）
    opt.AddMultipleService();

    // 注册 Lycoris 扩展模块
    opt.AddRegisterModule<ApplicationModule>();

    // 注册 Autofac 原生模块
    opt.AddAutofacModule<MyAutofacModule>();

    // 添加全局拦截器（可选）
    opt.AddGlobalInterceptor<UnitOfWorkInterceptor>(0);
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();
app.Run();
```

---

## 一、特性自动注册

在要注册的服务类上添加 `[AutofacRegister]` 特性：

```csharp
// 瞬态
[AutofacRegister(ServiceLifeTime.Transient)]
public class BlogService : IBlogService { }

// 作用域
[AutofacRegister(ServiceLifeTime.Scoped)]
public class BlogService : IBlogService { }

// 单例
[AutofacRegister(ServiceLifeTime.Singleton)]
public class BlogService : IBlogService { }
```

### 特性属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `ServiceLifeTime` | `ServiceLifeTime` | — | 生命周期：`Transient`、`Scoped`、`Singleton` |
| `Self` | `bool` | `false` | 仅注册自身类型，不绑定接口 |
| `Interface` | `Type?` | `null` | 指定注册的接口（多接口时） |
| `MultipleNamed` | `string?` | `null` | 多实现时的名称标识（`.Named()`） |
| `Key` | `object?` | `null` | Autofac 8.x Keyed 服务键（与 `MultipleNamed` 互斥） |
| `PropertiesAutowired` | `bool` | `false` | 启用属性注入 |
| `EnableInterceptor` | `bool` | `false` | 启用 AOP 拦截 |
| `Interceptor` | `Type?` | `null` | 指定 AOP 拦截器（设置后自动启用拦截） |
| `InterceptorOrder` | `int?` | `null` | 拦截器执行优先级，数值越小优先级越高 |
| `InterceptionType` | `InterceptionType` | `Interface` | AOP 拦截类型：`Interface`（接口代理，默认）或 `VirtualClass`（类虚方法代理） |
| `ExcludeInterceptor` | `Type?` | `null` | 排除指定拦截器，该拦截器不会应用到当前服务 |
| `IsInterceptor` | `bool` | `false` | 标记当前类为拦截器 |

### 指定接口注册

当实现类继承了多个接口，可以通过 `Interface` 属性明确指定注册哪个接口：

```csharp
[AutofacRegister(ServiceLifeTime.Scoped, Interface = typeof(IBlogService))]
public class BlogService : BaseService, IBlogService, IOtherService { }
```

如果不指定，扩展默认取**最后一个继承的接口**（代码中 `LastOrDefault()`）或**名称以类名结尾的接口**。

### 仅注册自身

适用于模型类、Job 等不需要接口绑定的场景：

```csharp
[AutofacRegister(ServiceLifeTime.Singleton, Self = true)]
public class MyJob { }
```

### 属性注入

```csharp
[AutofacRegister(ServiceLifeTime.Scoped, PropertiesAutowired = true)]
public class BlogService : IBlogService
{
    // Autofac 会自动注入标记为 public 的属性
    public ILogger<BlogService> Logger { get; set; }
}
```

### 类拦截器（VirtualClass 拦截）

适用于 `Self = true`（未绑定接口）的服务，通过虚方法代理实现拦截：

```csharp
[AutofacRegister(ServiceLifeTime.Scoped, Self = true, EnableInterceptor = true, InterceptionType = InterceptionType.VirtualClass)]
public class MyService
{
    // 方法必须为 virtual 才能被拦截
    public virtual void DoWork() { }
}
```

```csharp
// 手动注册时指定类拦截
builder.Register<MyService>(ServiceLifeTime.Scoped, opt =>
{
    opt.EnableInterceptor = true;
    opt.InterceptionType = InterceptionType.VirtualClass;
});
```

> **注意**：类拦截要求被拦截的方法声明为 `virtual`，泛型注册不支持类拦截。

### 排除指定拦截器

当模块或全局配置了拦截器，但某个服务不想应用特定拦截器时：

```csharp
// 排除 LogInterceptor，其他拦截器照常生效
[AutofacRegister(ServiceLifeTime.Scoped, EnableInterceptor = true, ExcludeInterceptor = typeof(LogInterceptor))]
public class CachedService : ICachedService { }
```

---

## 二、Keyed 服务（Autofac 8.x）

支持 Autofac 8.x 的键控服务，键可以是任意对象（字符串、枚举、整数等）：

```csharp
// 使用枚举作为键
public enum CacheType { Memory, Redis }

[AutofacRegister(ServiceLifeTime.Singleton, Key = CacheType.Memory)]
public class MemoryCache : ICache { }

[AutofacRegister(ServiceLifeTime.Singleton, Key = CacheType.Redis)]
public class RedisCache : ICache { }
```

解析时使用 Autofac 的 `IIndex<K, V>` 或 `ResolveKeyed`：

```csharp
public class Consumer
{
    public Consumer(IIndex<CacheType, ICache> caches)
    {
        var memoryCache = caches[CacheType.Memory];
        var redisCache = caches[CacheType.Redis];
    }
}
```

> **注意**：`Key` 和 `MultipleNamed` 互斥，同时设置会抛出 `InvalidOperationException`。

---

## 三、多实现解析

同一接口多个实现时，通过 `MultipleNamed` 区分：

```csharp
[AutofacRegister(ServiceLifeTime.Scoped, MultipleNamed = "Blog.A")]
public class BlogAService : IBlogService { }

[AutofacRegister(ServiceLifeTime.Scoped, MultipleNamed = "Blog.B")]
public class BlogBService : IBlogService { }
```

在构造函数中注入 `IAutofacMultipleService` 按名称获取：

```csharp
public class Consumer
{
    private readonly IBlogService _blogA;
    private readonly IBlogService _blogB;

    public Consumer(IAutofacMultipleService multipleService)
    {
        // 找不到时抛出 InvalidOperationException
        _blogA = multipleService.GetService<IBlogService>("Blog.A");

        // 找不到时返回 null
        _blogB = multipleService.TryGetService<IBlogService>("Blog.B");
    }
}
```

> 需要先在 `UseAutofacExtensions` 中调用 `AddMultipleService()` 或 `AddTaskExecutor()` 启用该服务。

---

## 四、AOP 拦截器

### 注册拦截器

拦截器需要实现 `Castle.DynamicProxy.IInterceptor`（同步）或 `Castle.DynamicProxy.IAsyncInterceptor`（异步）：

```csharp
// 同步拦截器
[AutofacRegister(ServiceLifeTime.Scoped, IsInterceptor = true)]
public class LogInterceptor : IInterceptor
{
    public void Intercept(IInvocation invocation)
    {
        Console.WriteLine($"调用 {invocation.Method.Name} 前");
        invocation.Proceed();
        Console.WriteLine($"调用 {invocation.Method.Name} 后");
    }
}

// 异步拦截器
[AutofacRegister(ServiceLifeTime.Scoped, IsInterceptor = true)]
public class LogAsyncInterceptor : IAsyncInterceptor { }
```

### 服务级拦截

```csharp
[AutofacRegister(ServiceLifeTime.Scoped, EnableInterceptor = true, Interceptor = typeof(LogInterceptor))]
public class BlogService : IBlogService { }
```

### 模块级拦截

```csharp
public class ApplicationModule : AutofacRegisterModule
{
    public override void ModuleRegister(ModuleBuilder builder)
    {
        builder.RegisterInterceptor<LogInterceptor>();
        builder.InterceptedBy<LogInterceptor>(0); // 当前模块下所有启用拦截的服务生效

        // 或使用便捷方法：注册 + 启用一步完成
        builder.RegisterInterceptorAndUse<LogInterceptor>(0);
        builder.RegisterAsyncInterceptorAndUse<LogAsyncInterceptor>(1);
    }
}
```

### 程序集拦截器自动扫描

自动发现并注册程序集中所有 `IInterceptor` / `IAsyncInterceptor` 实现：

```csharp
public override void ModuleRegister(ModuleBuilder builder)
{
    // 自动扫描并注册所有拦截器
    builder.RegisterAssemblyInterceptors();

    // 手动指定拦截优先级
    builder.InterceptedBy<LogInterceptor>(0);
}
```

### 全局拦截器

```csharp
builder.UseAutofacExtensions(opt =>
{
    opt.AddGlobalInterceptor<UnitOfWorkInterceptor>(0);
});
```

> 拦截器排序：数值越小优先级越高。服务级 > 模块级 > 全局级，同一级别按 `Order` 排序。

---

## 五、模块注册

### Lycoris 扩展模块

继承 `AutofacRegisterModule`，重写 `ModuleRegister`：

```csharp
public class ApplicationModule : AutofacRegisterModule
{
    // Host 扩展注册（如 Serilog）
    public override void HostRegister(ConfigureHostBuilder host)
    {
        host.UseSerilog();
    }

    // IServiceCollection 扩展注册
    public override void ServiceRegister(IServiceCollection services)
    {
        services.AddScoped<ISerService, SerService>();
    }

    // 手动服务注册
    public override void ModuleRegister(ModuleBuilder builder)
    {
        // === 传统生命周期方法 ===
        builder.RegisterScoped<MyService>();
        builder.RegisterSingleton<MyJob>();

        // === 生命周期参数化便捷方法 ===
        builder.Register<MyService>(ServiceLifeTime.Scoped);
        builder.Register<IService, MyService>(ServiceLifeTime.Transient);
        builder.Register<IService, MyService>(ServiceLifeTime.Scoped, "Named.A");
        builder.Register<IService, MyService>(ServiceLifeTime.Singleton, opt =>
        {
            opt.PropertiesAutowired = true;
        });

        // === 条件注册 ===
        builder.RegisterIf<IService, ProdService>(isProduction, ServiceLifeTime.Scoped);
        builder.RegisterIf<IService, DevService>(!isProduction, ServiceLifeTime.Scoped);

        // 接口 → 实现（传统写法）
        builder.RegisterScoped<IService, MyService>();

        // 带命名（多实现）
        builder.RegisterScoped<IService, MyService>("Named.A");

        // 带配置
        builder.RegisterScoped<IService, MyService>(opt =>
        {
            opt.PropertiesAutowired = true;
            opt.EnableInterceptor = true;
            opt.Named = "Service.A";        // 字符串命名
            opt.Key = MyKeyEnum.Value;      // Keyed 服务键
            opt.InterceptedBy<LogInterceptor>(0);
        });

        // 注册拦截器
        builder.RegisterInterceptor<LogInterceptor>();
        builder.RegisterAsyncInterceptor<LogAsyncInterceptor>();

        // 注册后台服务
        builder.RegisterHostedService<MyHostedService>();

        // 程序集批量扫描
        builder.RegisterAssemblyBy<IBaseService>(opt =>
        {
            opt.ServiceLifeTime = ServiceLifeTime.Scoped;
            opt.EnableInterceptor = true;
        });
    }
}
```

### 程序集扫描

`RegisterAssemblyBy` 支持多次调用，配置不同的扫描规则：

```csharp
public override void ModuleRegister(ModuleBuilder builder)
{
    // 扫描所有实现 IScopedService 的类 → 注册为 Scoped
    builder.RegisterAssemblyBy<IScopedService>(opt =>
    {
        opt.ServiceLifeTime = ServiceLifeTime.Scoped;
    });

    // 扫描所有继承 BaseJob 的类 → 注册为 Singleton
    builder.RegisterAssemblyBy<BaseJob>(opt =>
    {
        opt.ServiceLifeTime = ServiceLifeTime.Singleton;
        opt.Self = true;
    });
}
```

### 命名约定扫描

按自定义条件筛选类型进行批量注册，不依赖基类或接口：

```csharp
public override void ModuleRegister(ModuleBuilder builder)
{
    // 注册所有名称以 "Service" 结尾的类
    builder.RegisterAssemblyByConvention(
        t => t.Name.EndsWith("Service"),
        opt =>
        {
            opt.ServiceLifeTime = ServiceLifeTime.Scoped;
        });

    // 注册所有在 "Services" 命名空间下的类
    builder.RegisterAssemblyByConvention(
        t => t.Namespace?.Contains("Services") == true,
        opt =>
        {
            opt.ServiceLifeTime = ServiceLifeTime.Transient;
        });
}
```

### 跨程序集扫描

从其他程序集扫描 `[AutofacRegister]` 标注的服务：

```csharp
public override void ModuleRegister(ModuleBuilder builder)
{
    // 扫描外部程序集中标记了 [AutofacRegister] 的服务
    builder.RegisterAllFromAssembly(typeof(ExternalService).Assembly);
}
```

> 标注了 `[AutofacRegister]` 特性的类会被程序集扫描自动排除，避免重复注册。

### Autofac 原生 Module

```csharp
public class MyAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<MyService>().As<IMyService>().InstancePerLifetimeScope();
    }
}

// 在 UseAutofacExtensions 中添加
builder.UseAutofacExtensions(opt =>
{
    opt.AddAutofacModule<MyAutofacModule>();
});
```

---

## 六、异步任务执行器

```csharp
public class Consumer
{
    public Consumer(IAsyncTaskExecutor executor)
    {
        // 立即执行
        executor.Execute<MyTaskHandler>();

        // 延迟 5 秒执行
        executor.DelayExecute<MyTaskHandler>(5);

        // 带参数执行
        executor.Execute<MyTaskHandler>(new { Id = 1 });
        executor.DelayExecute<MyTaskHandler>(new { Id = 1 }, 10);
    }
}
```

任务处理器：

```csharp
[AutofacRegister(ServiceLifeTime.Scoped, MultipleNamed = "MyTask")]
public class MyTaskHandler : AsyncTaskExecutorHandler
{
    public override async Task ExecuteAsync()
    {
        await Task.Delay(1000);
        Console.WriteLine("任务完成");
    }

    public override async Task ExecuteAsync(object? args)
    {
        Console.WriteLine($"任务完成，参数: {args}");
        await Task.CompletedTask;
    }
}
```

### 自定义解析策略

子类重写 `ResolveHandler` 实现自定义处理器解析：

```csharp
public class KeyedTaskExecutor : AsyncTaskExecutor
{
    public KeyedTaskExecutor(IServiceScopeFactory scopeFactory) : base(scopeFactory) { }

    protected override IAsyncTaskExecutorHandler ResolveHandler(
        IAutofacMultipleService multipleService, Type handlerType)
    {
        // 使用 Key 而非 Named 解析
        var key = handlerType.GetCustomAttribute<AutofacRegisterAttribute>(false)?.Key;
        // ... 自定义解析逻辑
    }
}
```

---

## 七、非 Web 应用（IHostBuilder）

```csharp
Host.CreateDefaultBuilder(args)
    .UseAutofacExtensions(opt =>
    {
        opt.AddRegisterModule<ApplicationModule>();
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
    });
```

---

## API 总览

### AutofacBuilder

| 方法 | 说明 |
|------|------|
| `AddRegisterModule<T>()` | 添加 Lycoris 扩展模块 |
| `AddAutofacModule<T>()` | 添加 Autofac 原生模块 |
| `AddGlobalInterceptor<T>(order?)` | 添加全局拦截器 |
| `AddMultipleService()` | 启用多实现解析服务 |
| `AddTaskExecutor()` | 启用异步任务执行器（同时启用多实现服务） |

### ModuleBuilder

| 方法 | 说明 |
|------|------|
| `RegisterTransient<T>()` | 注册瞬态服务 |
| `RegisterScoped<T>()` | 注册作用域服务 |
| `RegisterSingleton<T>()` | 注册单例服务 |
| `Register<T>(lifeTime)` | 生命周期参数化注册（10 个重载） |
| `RegisterIf<T>(condition, lifeTime)` | 条件注册（5 个重载） |
| `RegisterInterceptor<T>()` | 注册同步拦截器 |
| `RegisterAsyncInterceptor<T>()` | 注册异步拦截器 |
| `RegisterInterceptorAndUse<T>(order?)` | 注册同步拦截器并启用（一步完成） |
| `RegisterAsyncInterceptorAndUse<T>(order?)` | 注册异步拦截器并启用（一步完成） |
| `RegisterAssemblyInterceptors()` | 自动扫描程序集中所有拦截器并注册 |
| `RegisterHostedService<T>()` | 注册后台服务 |
| `InterceptedBy<T>(order?)` | 添加模块级拦截器 |
| `RegisterAssemblyBy<T>(configure)` | 程序集批量扫描（可多次调用） |
| `RegisterAssemblyByConvention(predicate, configure)` | 按命名约定/条件批量扫描 |
| `RegisterAllFromAssembly(assembly)` | 从指定程序集扫描 `[AutofacRegister]` 服务 |

### IAutofacMultipleService

| 方法 | 说明 |
|------|------|
| `GetService<T>(name)` | 按名称获取服务，未找到抛出异常 |
| `TryGetService<T>(name)` | 按名称获取服务，未找到返回 null |

### IAsyncTaskExecutor

| 方法 | 说明 |
|------|------|
| `Execute<T>()` | 立即执行任务 |
| `Execute<T>(args)` | 立即执行带参数任务 |
| `DelayExecute<T>(seconds)` | 延迟执行任务 |
| `DelayExecute<T>(args, seconds)` | 延迟执行带参数任务 |

---

## 许可证

[MIT](LICENSE)

Copyright (c) 2023 Lycoris
