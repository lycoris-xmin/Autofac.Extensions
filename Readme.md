## Autofac扩展 目前不支持控制器层相关的相关注册服务

### 更新日志

| **更新时间** | **版本** | **更新内容**                                                                                                                    |
| :----------- | :------- | :------------------------------------------------------------------------------------------------------------------------------ |
| 2022-12-06   | 6.0.4    | 修复泛型服务特性自动注册提示封闭类型的bug                                                                                       |
| 2022-01-04   | 6.0.4    | 修复部分特定情况下的bug                                                                                                         |
| 2022-02-01   | 6.0.6    | 修复扩展的模块注册内手动注册服务没有引入拦截器的bug                                                                             |
| 2023-02-03   | 6.0.7    | 优化部分代码，增加自带的`IServiceCollection`服务注册，可以在`LycorisRegisterModule`直接注册，不需要再单独使用一个类来写扩展注册 |
| 2023-03-27   | 6.0.8    | 优化部分代码，增加自带的`ConfigureHostBuilder`服务注册，并将多实现类获取服务更新为按需注册                                      |


### 没有详细测试，本身也只是方便作者及作者朋友使用，可能存在某些特定情况下的bug，请各位使用的大佬手下留情

### **一、替换系统自带的DI容器为Autofac**

```
var builder = WebApplication.CreateBuilder(args);

// 替换系统自带的DI容器为Autofac
// 注意 6.0.7 版本之后 这个位置没有Host了，因为需要支持自带的 IServiceCollection 的扩展服务注册
// 6.0.8 支持自带的 ConfigureHostBuilder 的扩展服务注册
builder.UseAutofacExtensions(builder =>
{
    // 多实现类服务获取服务 默认：false
    // 启用后才能通过扩展的 IAutofacMultipleService 服务多实现类接口
    // 没有设置的话，默认不注册该服务
    builder.EnabledLycorisMultipleService = true;
});

// Add services to the container.

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.Run();
```


### **二、服务注册**
**服务注册依旧使用特性的方式处理，在需要注册的服务类上引用特性`[AutofacRegister(ServiceLifeTime.Scoped)]`**

**`AutofacRegisterAttribute`特性属性详解**

- **`ServiceLifeTime`：服务生命周期,同系统的一样有：`Transient`,`Scoped`,`Singleton`三种**
  
- **`PropertiesAutowired`：属性注入，Autofac特有的注入方式，默认为false**
  
- **`EnableInterceptor`：开启AOP拦截支持，默认为false，只有开启了注册服务时候才会同时注入拦截器**

- **`Self`：仅注册当前类不与接口绑定，适合模型类、Quartz的调度任务注册**
  
- **`Interface`：指定接口，当前类继承了多个接口时，可以通过设置此属性指定注册的接口**

- **`Interceptor`：添加指定AOP拦截器,适合当前类库仅个别服务使用到某些固定的一个拦截器使用，该属性设置后，会默认开启AOP拦截支持，同样的指定的AOP拦截器也需要实现 `Castle.Core` 的 `IInterceptor` 接口**
  
- **`IsInterceptor`：当前服务是否为AOP服务**
  
**以下举几个比较常用的注册举例**

- **1. 最常见的注册**
```csharp
// 瞬态服务注册
[AutofacRegister(ServiceLifeTime.Transient)]
public class BlogAppService : IBlogAppService
{

}

// 作用域服务注册
[AutofacRegister(ServiceLifeTime.Scoped)]
public class BlogAppService : IBlogAppService
{

}

// 单例服务注册
[AutofacRegister(ServiceLifeTime.Singleton)]
public class BlogAppService : IBlogAppService
{

}
```

- **2. 继承了多个接口的的实现，想指定注册接口**

```csharp
[AutofacRegister(ServiceLifeTime.Scoped, PropertiesAutowired = true, Interface = typeof(IBlogAppService))]
public class BlogAppService : ApplicationBaseService<BlogAppService>, IBlogAppService, IBlogBaseAppService
{

}
```

**如果继承了多个接口，没有指定，扩展默认取实现类最后继承的接口**

**例1：**
```csharp
// 接口A
public interface A
{

}

// 接口B
public interface B : A
{

}

// 接口C
public interface C : B
{

}

// 实现类D
public class D : C 
{

}

// 以上情况，不指定注册接口的情况下，扩展默认取 接口C与实现类D进行注册
```

**例2：**
```csharp
// 接口A
public interface A
{

}

// 接口B
public interface B 
{

}

// 接口C
public interface C 
{

}

// 实现类D
public class D : B, A, C
{

}

// 以上情况，不指定注册接口的情况下，扩展默认取 接口B与实现类D进行注册
// 直观一点理解就是取离当前实现类最近的继承接口
```


- **3. 一个接口多个实现类**
  
```csharp
// IBlogAppService 接口第一个实现类，利用 MultipleNamed 属性，进行区分指定
[AutofacRegister(ServiceLifeTime.Scoped, MultipleNamed = "Blog.A")]
public class BlogAAppService : ApplicationBaseService<BlogAppService>, IBlogAppService
{

}

// IBlogAppService 接口第二个实现类，利用 MultipleNamed 属性，进行区分指定
[AutofacRegister(ServiceLifeTime.Scoped, MultipleNamed = "Blog.B")]
public class BlogBAppService : ApplicationBaseService<BlogAppService>, IBlogAppService
{

}
```

**使用扩展内服务 `IAutofacMultipleService` 进行指定的实现类获取**
**注意：`6.0.8`版本及以后需要在添加扩展的时候，配置`EnabledLycorisMultipleService`属性为`true`，才能正常使用该服务，否则扩展不会注册 `IAutofacMultipleService` 服务**

``` csharp
public class Service : IService
{
    private readonly IBlogAppService _blogA;
    private readonly IBlogAppService _blogB;

    public Service(IAutofacMultipleService multipleService)
    {
        // 使用 GetService 获取服务时，如果服务找不到或者服务未注册会抛出 ArgumentNullException 异常
       _blogA = multipleService.GetService<IBlogAppService>("Blog.A");

        // 使用 TryGetService 获取服务时，如果服务找不到或者未注册会返回null
       _blogB = multipleService.TryGetService<IBlogAppService>("Blog.B");
    }

}
```

- **4. 需要使用属性注入的服务注册**
```csharp
[AutofacRegister(ServiceLifeTime.Scoped, PropertiesAutowired = true)]
public class BlogAppService : ApplicationBaseService<BlogAppService>, IBlogAppService
```

- **5. 需要开启AOP拦截的服务注册**
```csharp
[AutofacRegister(ServiceLifeTime.Scoped, PropertiesAutowired = true, EnableInterceptor = true)]
public class BlogAppService : ApplicationBaseService<BlogAppService>, IBlogAppService
```

- **6. `Castle.Core` AOP拦截器注册**
```csharp
[AutofacRegister(ServiceLifeTime.Scoped, IsInterceptor = true)]
public class UnitOfWorkInterceptor : IInterceptor
{

}

[AutofacRegister(ServiceLifeTime.Scoped, IsInterceptor = true)]
public class UnitOfWorkAsyncInterceptor : IAsyncInterceptor
{

}
```

- **7. 当前服务指定额外添加的AOP拦截服务注册**
  
**当你指定了需要使用的AOP拦截器，则扩展注册时候会自动开启`EnableInterceptor`属性**
```csharp
[AutofacRegister(ServiceLifeTime.Scoped, Interceptor = typeof(UnitOfWorkInterceptor))]
public class BlogAppService : IBlogAppService
{

}
```


### **三、模块注册**

**模块注册实现有两种方式**

- **1. 在需要使用到注册服务的类库创建一个类并继承扩展好的基类`LycorisRegisterModule`**
  
```csharp
 public class ApplicationModule : LycorisRegisterModule
 {
    // 注册扩展服务，如一些Nuget包中的服务需要使用 builder.Host 扩展注册
    // 注意：需要手动重写
    public override void HostRegister(ConfigureHostBuilder host)
    {
        host.UseSerilog();
    }

    // 注册扩展服务，如一些Nuget包中的服务需要使用 IServiceCollection 扩展注册
    // 注意：需要手动重写
    public override void SerivceRegister(IServiceCollection services)
    {
        services.AddScoped<ISerService, SerService>();
    }

    // 注意：由于有些使用的小伙伴提示，使用特性自动注册后，这部分基本都用不到，但是由于 abstract 修饰，所以每次都需要重写
    // 注意：需要手动重写
    public override void ModuleRegister(LycorisModuleBuilder builder)
    {
         // 如果你不习惯使用特性自动注册，也可以在此处自己注册
         // 此处仅是使用示例，请不要照搬代码，也不要纠结我这里注册了多次
         // 注册需要注意不要在此处注册的服务也使用特性自动注册，这样会引发注册异常
         // 此处仅展示几种，实际上里面包含了所有的生命周期的方法
         builder.RegisterScoped<ApplicationAppService>();
         builder.RegisterScoped<IApplicationAppService, ApplicationAppService>();
         builder.RegisterScoped<SalesUserAppService>(opt =>
         {
             // 开启Autofac属性注入
             opt.PropertiesAutowired = true;
             // 开启AOP拦截
             opt.EnableInterceptor = true;
             // 设置AOP拦截器(可以添加多个，添加多个的时候为避免拦截顺序打不到你的预期，请指定拦截器顺序，同上面一直数值越小，优先级越大，但是数值必须大于0)
             // 如果不设置顺序，那顺序编排就会按照添加的顺序
             // 如果添加了拦截器，会自动开启AOP拦截，即使你手动关闭也无效，所以如果当前服务不想开启AOP拦截，就不要添加拦截器
             opt.InterceptedBy<UnitOfWorkInterceptor>(0);
         });

         // 注册处拦截器
         builder.RegisterInterceptor<OperationLogInterceptor>();
         // 注册异步拦截器
         builder.RegisterAsyncInterceptor<OperationLogAsyncInterceptor>();

         /* 
          * 服务使用AOP拦截器注意事项:
          * 1. Aop拦截器需要实现 Castle.Core 的 IInterceptor 接口
          * 2. 添加AOP拦截器，如果有多个拦截器请注明拦截器拦截顺序，数值越小优先级越大，默认为0，数值必须大于等于0
          * 3. 需要使用拦截器的服务请在特性中开启允许AOP拦截，否则即使你添加了拦截器，也无法使用
          * 4. 如果不设置顺序，那顺序编排就会按照添加的顺序
          * 5. 此处添加的拦截器会应用于当前类库下所有开启AOP拦截的服务
          * 6. 为了规范使用，此处添加的拦截器并不会自己注册，需要使用者使用自动特性注册或者在此处手动注册
          */

         // EFCore数据库事务AOP拦截器
         // 手动注册处拦截器（使用自动特性注册的话，就不需要此处再添加了）
         builder.RegisterInterceptor<UnitOfWorkInterceptor>();
         // 手动注册处拦截器（使用自动特性注册的话，就不需要此处再添加了）
         builder.RegisterAsyncInterceptor<UnitOfWorkAsyncInterceptor>();
         // 为当前类库添加拦截器
         builder.InterceptedBy<UnitOfWorkInterceptor>(0);

         // 例子：操作日志AOP拦截器
         // 手动注册处拦截器（使用自动特性注册的话，就不需要此处再添加了）
         builder.RegisterInterceptor<OperationLogInterceptor>();
         // 手动注册处拦截器（使用自动特性注册的话，就不需要此处再添加了）
         builder.RegisterAsyncInterceptor<OperationLogAsyncInterceptor>();
         // 为当前类库添加拦截器
         builder.InterceptedBy<OperationLogInterceptor>(1);
    }
 }
```

- **2. 在需要使用到注册服务的类库创建一个类并继承`Autofac`的`Module`类**
```csharp
public class ApplicationAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // do something
    }
}
```

**在第一步替换系统DI容器的扩展中引入模块注册服务**

```csharp
var builder = WebApplication.CreateBuilder(args);

// 替换系统自带的DI容器为Autofac
// 注意 6.0.6 版本之后 这个位置没有Host了，因为需要支持自带的IServiceCollection的扩展服务注册
builder.UseAutofacExtensions(builder =>
{
    // 如果有多个类库的服务，每个类库都需要新建一个类来扩展封装好的 AutofacRegisterModule 并在此处依次添加即可
    // 添加方式1：
    builder.AddLycorisRegisterModule<ApplicationModule>();
    builder.AddLycorisRegisterModule<ModelModule>();

    // 添加方式2：
    builder.AddLycorisRegisterModule<ApplicationModule>()
           .AddLycorisRegisterModule<ModelModule>();

    // Autofac原生的模块注册
    // 注意使用原生的模块注册，扩展内除 IAutofacMultipleService 服务,其他功能均不支持
    // 原生注册的功能，仅仅是给部分想试水扩展的朋友一个兼容性而已，不需要全部的注册都改造，只需要改造其中一个的部分，其他依旧参照原生的进行测试来决定需不需要使用
    builder.AddAutofacModule<ApplicationAutofacModule>();

    // 如果大部分类库都用到某些固定的拦截器，这里可以使用注入全局拦截器。
    // 需要使用拦截器的服务请在特性中开启允许AOP拦截
    // 注意全局拦截器仅对继承 LycorisRegisterModule 的模块有效
    builder.AddGlobalInterceptor<UnitOfWorkInterceptor>(0);
});

// Add services to the container.

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.Run();
```