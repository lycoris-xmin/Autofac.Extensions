using Castle.DynamicProxy;
using Castle.DynamicProxy.Internal;
using Lycoris.Autofac.Extensions.Options;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Lycoris.Autofac.Extensions.Impl
{
    /// <summary>
    /// Lycoris扩展构建服务
    /// </summary>
    public sealed class ModuleBuilder
    {
        internal readonly List<LycorisRegisterService> RegisterContainer = new();
        internal readonly List<InterceptorOption> InterceptorOptions = new();
        internal bool AutoRegisterInterceptors { get; private set; } = false;

        /// <summary>
        /// 为当前类库添加过滤器 
        /// 需要使用过滤器的服务，请将注册特性 <see cref="AutofacRegisterAttribute"/> 中的 <see langword="AutofacRegister(EnableInterceptor = true)"/> 属性设置为true, 否则过滤器不生效
        /// </summary>
        /// <typeparam name="TInterceptor"></typeparam>
        /// <param name="order"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public ModuleBuilder InterceptedBy<TInterceptor>(int? order = null) where TInterceptor : class, IInterceptor
        {
            if (order.HasValue && order.Value < 0)
                throw new ArgumentOutOfRangeException(nameof(order), "range must be greater than or equal to 0");

            order ??= InterceptorOptions.Count;

            InterceptorOptions.Add(new InterceptorOption(typeof(TInterceptor), order.Value));

            return this;
        }

        /// <summary>
        /// 注册瞬态服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public ModuleBuilder RegisterTransient<T>() where T : class
            => RegisterSelf(typeof(T), ServiceLifeTime.Transient);

        /// <summary>
        /// 注册瞬态服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configure"></param>
        /// <returns></returns>
        public ModuleBuilder RegisterTransient<T>(Action<AutofacSingleBuilder> configure) where T : class
            => RegisterSelf(typeof(T), ServiceLifeTime.Transient, configure);

        /// <summary>
        /// 注册瞬态服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TImpl"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public ModuleBuilder RegisterTransient<T, TImpl>() where TImpl : T where T : class
        {
            if (!typeof(T).IsInterface)
                throw new ArgumentException("must be an interface", nameof(T));

            return RegisterAsType(typeof(T), typeof(TImpl), ServiceLifeTime.Transient);
        }

        /// <summary>
        /// 注册瞬态服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TImpl"></typeparam>
        /// <param name="named"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public ModuleBuilder RegisterTransient<T, TImpl>([NotNull] string named) where TImpl : T where T : class
        {
            if (!typeof(T).IsInterface)
                throw new ArgumentException("must be an interface", nameof(T));

            return RegisterAsType(typeof(T), typeof(TImpl), ServiceLifeTime.Transient, named);
        }

        /// <summary>
        /// 注册瞬态服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TImpl"></typeparam>
        /// <param name="configure"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public ModuleBuilder RegisterTransient<T, TImpl>(Action<AutofacSingleBuilder> configure) where TImpl : T where T : class
        {
            if (!typeof(T).IsInterface)
                throw new ArgumentException("must be an interface", nameof(T));

            var builder = new AutofacSingleBuilder();
            configure(builder);

            if (builder.Interceptors != null && builder.Interceptors.Any())
                builder.EnableInterceptor = true;

            return RegisterAsType(typeof(T), typeof(TImpl), ServiceLifeTime.Transient, configure);
        }

        /// <summary>
        /// 注册瞬态服务
        /// </summary>
        /// <returns></returns>
        public ModuleBuilder RegisterTransient(Type type)
            => RegisterSelf(type, ServiceLifeTime.Transient);

        /// <summary>
        /// 注册瞬态服务
        /// </summary>
        /// <param name="type"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public ModuleBuilder RegisterTransient(Type type, Action<AutofacSingleBuilder> configure)
            => RegisterSelf(type, ServiceLifeTime.Transient, configure);

        /// <summary>
        /// 注册瞬态服务
        /// </summary>
        /// <param name="interface"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public ModuleBuilder RegisterTransient(Type @interface, Type service)
            => RegisterAsType(@interface, service, ServiceLifeTime.Transient);

        /// <summary>
        /// 注册瞬态服务
        /// </summary>
        /// <param name="interface"></param>
        /// <param name="service"></param>
        /// <param name="named"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public ModuleBuilder RegisterTransient(Type @interface, Type service, [NotNull] string named)
            => RegisterAsType(@interface, service, ServiceLifeTime.Transient, named);

        /// <summary>
        /// 注册瞬态服务
        /// </summary>
        /// <param name="interface"></param>
        /// <param name="service"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public ModuleBuilder RegisterTransient(Type @interface, Type service, Action<AutofacSingleBuilder> configure)
            => RegisterAsType(@interface, service, ServiceLifeTime.Transient, configure);


        /// <summary>
        /// 注册作用域服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public ModuleBuilder RegisterScoped<T>() where T : class
          => RegisterSelf(typeof(T), ServiceLifeTime.Scoped);

        /// <summary>
        /// 注册作用域服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configure"></param>
        /// <returns></returns>
        public ModuleBuilder RegisterScoped<T>(Action<AutofacSingleBuilder> configure) where T : class
          => RegisterSelf(typeof(T), ServiceLifeTime.Scoped, configure);

        /// <summary>
        /// 注册作用域服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TImpl"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public ModuleBuilder RegisterScoped<T, TImpl>() where TImpl : T where T : class
        {
            if (!typeof(T).IsInterface)
                throw new ArgumentException("must be an interface", nameof(T));

            return RegisterAsType(typeof(T), typeof(TImpl), ServiceLifeTime.Scoped);
        }

        /// <summary>
        /// 注册作用域服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TImpl"></typeparam>
        /// <param name="named"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public ModuleBuilder RegisterScoped<T, TImpl>(string named) where TImpl : T where T : class
        {
            if (!typeof(T).IsInterface)
                throw new ArgumentException("must be an interface", nameof(T));

            return RegisterAsType(typeof(T), typeof(TImpl), ServiceLifeTime.Scoped, named);
        }

        /// <summary>
        /// 注册作用域服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TImpl"></typeparam>
        /// <param name="configure"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public ModuleBuilder RegisterScoped<T, TImpl>(Action<AutofacSingleBuilder> configure) where TImpl : T where T : class
        {
            if (!typeof(T).IsInterface)
                throw new ArgumentException("must be an interface", nameof(T));

            return RegisterAsType(typeof(T), typeof(TImpl), ServiceLifeTime.Scoped, configure);
        }

        /// <summary>
        /// 注册作用域服务
        /// </summary>
        /// <returns></returns>
        public ModuleBuilder RegisterScoped(Type type)
            => RegisterSelf(type, ServiceLifeTime.Scoped);

        /// <summary>
        /// 注册作用域服务
        /// </summary>
        /// <param name="type"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public ModuleBuilder RegisterScoped(Type type, Action<AutofacSingleBuilder> configure)
            => RegisterSelf(type, ServiceLifeTime.Scoped, configure);

        /// <summary>
        /// 注册作用域服务
        /// </summary>
        /// <param name="interface"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public ModuleBuilder RegisterScoped(Type @interface, Type service)
            => RegisterAsType(@interface, service, ServiceLifeTime.Scoped);

        /// <summary>
        /// 注册作用域服务
        /// </summary>
        /// <param name="interface"></param>
        /// <param name="service"></param>
        /// <param name="named"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public ModuleBuilder RegisterScoped(Type @interface, Type service, [NotNull] string named)
            => RegisterAsType(@interface, service, ServiceLifeTime.Scoped, named);

        /// <summary>
        /// 注册作用域服务
        /// </summary>
        /// <param name="interface"></param>
        /// <param name="service"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public ModuleBuilder RegisterScoped(Type @interface, Type service, Action<AutofacSingleBuilder> configure)
            => RegisterAsType(@interface, service, ServiceLifeTime.Scoped, configure);


        /// <summary>
        /// 注册单例服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public ModuleBuilder RegisterSingleton<T>() where T : class
          => RegisterSelf(typeof(T), ServiceLifeTime.Singleton);

        /// <summary>
        /// 注册单例服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configure"></param>
        /// <returns></returns>
        public ModuleBuilder RegisterSingleton<T>(Action<AutofacSingleBuilder> configure) where T : class
          => RegisterSelf(typeof(T), ServiceLifeTime.Singleton, configure);

        /// <summary>
        /// 注册单例服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TImpl"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public ModuleBuilder RegisterSingleton<T, TImpl>() where TImpl : T where T : class
        {
            if (!typeof(T).IsInterface)
                throw new ArgumentException("must be an interface", nameof(T));

            return RegisterAsType(typeof(T), typeof(TImpl), ServiceLifeTime.Singleton);
        }

        /// <summary>
        /// 注册单例服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TImpl"></typeparam>
        /// <param name="named"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public ModuleBuilder RegisterSingleton<T, TImpl>(string named) where TImpl : T where T : class
        {
            if (!typeof(T).IsInterface)
                throw new ArgumentException("must be an interface", nameof(T));

            return RegisterAsType(typeof(T), typeof(TImpl), ServiceLifeTime.Singleton, named);
        }

        /// <summary>
        /// 注册单例服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TImpl"></typeparam>
        /// <param name="configure"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public ModuleBuilder RegisterSingleton<T, TImpl>(Action<AutofacSingleBuilder> configure) where TImpl : T where T : class
        {
            if (!typeof(T).IsInterface)
                throw new ArgumentException("must be an interface", nameof(T));

            var builder = new AutofacSingleBuilder();
            configure(builder);

            return RegisterAsType(typeof(T), typeof(TImpl), ServiceLifeTime.Singleton, configure);
        }

        /// <summary>
        /// 注册单例服务
        /// </summary>
        /// <returns></returns>
        public ModuleBuilder RegisterSingleton(Type type)
            => RegisterSelf(type, ServiceLifeTime.Singleton);

        /// <summary>
        /// 注册单例服务
        /// </summary>
        /// <param name="type"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public ModuleBuilder RegisterSingleton(Type type, Action<AutofacSingleBuilder> configure)
            => RegisterSelf(type, ServiceLifeTime.Singleton, configure);

        /// <summary>
        /// 注册单例服务
        /// </summary>
        /// <param name="interface"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public ModuleBuilder RegisterSingleton(Type @interface, Type service)
            => RegisterAsType(@interface, service, ServiceLifeTime.Singleton);

        /// <summary>
        /// 注册单例服务
        /// </summary>
        /// <param name="interface"></param>
        /// <param name="service"></param>
        /// <param name="named"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public ModuleBuilder RegisterSingleton(Type @interface, Type service, [NotNull] string named)
            => RegisterAsType(@interface, service, ServiceLifeTime.Singleton, named);

        /// <summary>
        /// 注册单例服务
        /// </summary>
        /// <param name="interface"></param>
        /// <param name="service"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public ModuleBuilder RegisterSingleton(Type @interface, Type service, Action<AutofacSingleBuilder> configure)
            => RegisterAsType(@interface, service, ServiceLifeTime.Singleton, configure);


        #region ==== 生命周期参数化便捷重载 ====

        /// <summary>
        /// 按生命周期注册服务（仅自身）
        /// </summary>
        public ModuleBuilder Register<T>(ServiceLifeTime lifeTime) where T : class
            => lifeTime switch
            {
                ServiceLifeTime.Transient => RegisterTransient<T>(),
                ServiceLifeTime.Scoped => RegisterScoped<T>(),
                ServiceLifeTime.Singleton => RegisterSingleton<T>(),
                _ => this
            };

        /// <summary>
        /// 按生命周期注册服务（仅自身，带配置）
        /// </summary>
        public ModuleBuilder Register<T>(ServiceLifeTime lifeTime, Action<AutofacSingleBuilder> configure) where T : class
            => lifeTime switch
            {
                ServiceLifeTime.Transient => RegisterTransient<T>(configure),
                ServiceLifeTime.Scoped => RegisterScoped<T>(configure),
                ServiceLifeTime.Singleton => RegisterSingleton<T>(configure),
                _ => this
            };

        /// <summary>
        /// 按生命周期注册服务（接口 → 实现）
        /// </summary>
        public ModuleBuilder Register<T, TImpl>(ServiceLifeTime lifeTime) where TImpl : T where T : class
            => lifeTime switch
            {
                ServiceLifeTime.Transient => RegisterTransient<T, TImpl>(),
                ServiceLifeTime.Scoped => RegisterScoped<T, TImpl>(),
                ServiceLifeTime.Singleton => RegisterSingleton<T, TImpl>(),
                _ => this
            };

        /// <summary>
        /// 按生命周期注册服务（接口 → 实现，具名）
        /// </summary>
        public ModuleBuilder Register<T, TImpl>(ServiceLifeTime lifeTime, string named) where TImpl : T where T : class
            => lifeTime switch
            {
                ServiceLifeTime.Transient => RegisterTransient<T, TImpl>(named),
                ServiceLifeTime.Scoped => RegisterScoped<T, TImpl>(named),
                ServiceLifeTime.Singleton => RegisterSingleton<T, TImpl>(named),
                _ => this
            };

        /// <summary>
        /// 按生命周期注册服务（接口 → 实现，带配置）
        /// </summary>
        public ModuleBuilder Register<T, TImpl>(ServiceLifeTime lifeTime, Action<AutofacSingleBuilder> configure) where TImpl : T where T : class
            => lifeTime switch
            {
                ServiceLifeTime.Transient => RegisterTransient<T, TImpl>(configure),
                ServiceLifeTime.Scoped => RegisterScoped<T, TImpl>(configure),
                ServiceLifeTime.Singleton => RegisterSingleton<T, TImpl>(configure),
                _ => this
            };

        /// <summary>
        /// 按生命周期注册服务（Type）
        /// </summary>
        public ModuleBuilder Register(Type type, ServiceLifeTime lifeTime)
            => lifeTime switch
            {
                ServiceLifeTime.Transient => RegisterTransient(type),
                ServiceLifeTime.Scoped => RegisterScoped(type),
                ServiceLifeTime.Singleton => RegisterSingleton(type),
                _ => this
            };

        /// <summary>
        /// 按生命周期注册服务（Type，带配置）
        /// </summary>
        public ModuleBuilder Register(Type type, ServiceLifeTime lifeTime, Action<AutofacSingleBuilder> configure)
            => lifeTime switch
            {
                ServiceLifeTime.Transient => RegisterTransient(type, configure),
                ServiceLifeTime.Scoped => RegisterScoped(type, configure),
                ServiceLifeTime.Singleton => RegisterSingleton(type, configure),
                _ => this
            };

        /// <summary>
        /// 按生命周期注册服务（接口Type → 实现Type）
        /// </summary>
        public ModuleBuilder Register(Type @interface, Type service, ServiceLifeTime lifeTime)
            => lifeTime switch
            {
                ServiceLifeTime.Transient => RegisterTransient(@interface, service),
                ServiceLifeTime.Scoped => RegisterScoped(@interface, service),
                ServiceLifeTime.Singleton => RegisterSingleton(@interface, service),
                _ => this
            };

        /// <summary>
        /// 按生命周期注册服务（接口Type → 实现Type，具名）
        /// </summary>
        public ModuleBuilder Register(Type @interface, Type service, ServiceLifeTime lifeTime, string named)
            => lifeTime switch
            {
                ServiceLifeTime.Transient => RegisterTransient(@interface, service, named),
                ServiceLifeTime.Scoped => RegisterScoped(@interface, service, named),
                ServiceLifeTime.Singleton => RegisterSingleton(@interface, service, named),
                _ => this
            };

        /// <summary>
        /// 按生命周期注册服务（接口Type → 实现Type，带配置）
        /// </summary>
        public ModuleBuilder Register(Type @interface, Type service, ServiceLifeTime lifeTime, Action<AutofacSingleBuilder> configure)
            => lifeTime switch
            {
                ServiceLifeTime.Transient => RegisterTransient(@interface, service, configure),
                ServiceLifeTime.Scoped => RegisterScoped(@interface, service, configure),
                ServiceLifeTime.Singleton => RegisterSingleton(@interface, service, configure),
                _ => this
            };

        #endregion

        #region ==== 条件注册 ====

        /// <summary>
        /// 条件注册（仅自身）
        /// </summary>
        public ModuleBuilder RegisterIf<T>(bool condition, ServiceLifeTime lifeTime) where T : class
            => condition ? Register<T>(lifeTime) : this;

        /// <summary>
        /// 条件注册（仅自身，带配置）
        /// </summary>
        public ModuleBuilder RegisterIf<T>(bool condition, ServiceLifeTime lifeTime, Action<AutofacSingleBuilder> configure) where T : class
            => condition ? Register<T>(lifeTime, configure) : this;

        /// <summary>
        /// 条件注册（接口 → 实现）
        /// </summary>
        public ModuleBuilder RegisterIf<T, TImpl>(bool condition, ServiceLifeTime lifeTime) where TImpl : T where T : class
            => condition ? Register<T, TImpl>(lifeTime) : this;

        /// <summary>
        /// 条件注册（接口 → 实现，具名）
        /// </summary>
        public ModuleBuilder RegisterIf<T, TImpl>(bool condition, ServiceLifeTime lifeTime, string named) where TImpl : T where T : class
            => condition ? Register<T, TImpl>(lifeTime, named) : this;

        /// <summary>
        /// 条件注册（接口 → 实现，带配置）
        /// </summary>
        public ModuleBuilder RegisterIf<T, TImpl>(bool condition, ServiceLifeTime lifeTime, Action<AutofacSingleBuilder> configure) where TImpl : T where T : class
            => condition ? Register<T, TImpl>(lifeTime, configure) : this;

        #endregion

        /// <summary>
        /// 注册同步拦截器，需要继承 <see cref="IInterceptor"/> 接口并实现
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public ModuleBuilder RegisterInterceptor<T>() where T : class, IInterceptor
        {
            RegisterContainer.Add(new LycorisRegisterService()
            {
                Type = typeof(T),
                Option = new AutofacRegisterAttribute(ServiceLifeTime.Scoped)
                {
                    Self = true
                }
            });
            return this;
        }

        /// <summary>
        /// 注册异步拦截器，需要继承 <see cref="IAsyncInterceptor"/> 接口并实现
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public ModuleBuilder RegisterAsyncInterceptor<T>() where T : class, IAsyncInterceptor
        {
            RegisterContainer.Add(new LycorisRegisterService()
            {
                Type = typeof(T),
                Option = new AutofacRegisterAttribute(ServiceLifeTime.Scoped)
                {
                    Self = true
                }
            });
            return this;
        }

        /// <summary>
        /// 注册同步拦截器并添加到模块级拦截器列表（等同于 RegisterInterceptor + InterceptedBy）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="order"></param>
        /// <returns></returns>
        public ModuleBuilder RegisterInterceptorAndUse<T>(int? order = null) where T : class, IInterceptor
        {
            RegisterInterceptor<T>();
            InterceptedBy<T>(order);
            return this;
        }

        /// <summary>
        /// 注册异步拦截器并添加到模块级拦截器列表（等同于 RegisterAsyncInterceptor + InterceptedBy）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="order"></param>
        /// <returns></returns>
        public ModuleBuilder RegisterAsyncInterceptorAndUse<T>(int? order = null) where T : class, IAsyncInterceptor
        {
            RegisterAsyncInterceptor<T>();
            if (order.HasValue && order.Value < 0)
                throw new ArgumentOutOfRangeException(nameof(order), "range must be greater than or equal to 0");
            order ??= InterceptorOptions.Count;
            InterceptorOptions.Add(new InterceptorOption(typeof(T), order.Value));
            return this;
        }

        /// <summary>
        /// 自动扫描程序集中所有 IInterceptor / IAsyncInterceptor 实现并注册
        /// </summary>
        public ModuleBuilder RegisterAssemblyInterceptors()
        {
            AutoRegisterInterceptors = true;
            return this;
        }

        /// <summary>
        /// 注册启动任务，需要继承 <see cref="IHostedService"/> 接口并实现
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public ModuleBuilder RegisterHostedService<T>() where T : class, IHostedService
        {
            RegisterContainer.Add(new LycorisRegisterService()
            {
                Type = typeof(T),
                AsType = typeof(IHostedService),
                Option = new AutofacRegisterAttribute(ServiceLifeTime.Transient)
            });
            return this;
        }

        internal readonly List<AssemblyFilterEntry> AssemblyFilterEntries = new();

        internal sealed class AssemblyFilterEntry
        {
            public Type FilterType { get; }
            public Action<RegisterAssemblyBuilder> Configure { get; }

            public AssemblyFilterEntry(Type filterType, Action<RegisterAssemblyBuilder> configure)
            {
                FilterType = filterType;
                Configure = configure;
            }
        }

        internal readonly List<PredicateAssemblyFilterEntry> PredicateFilterEntries = new();

        internal sealed class PredicateAssemblyFilterEntry
        {
            public Func<Type, bool> Predicate { get; }
            public Action<RegisterAssemblyBuilder> Configure { get; }

            public PredicateAssemblyFilterEntry(Func<Type, bool> predicate, Action<RegisterAssemblyBuilder> configure)
            {
                Predicate = predicate;
                Configure = configure;
            }
        }

        internal readonly List<Assembly> AdditionalAssemblies = new();

        /// <summary>
        /// 注册程序级中继承了泛型的实现类
        /// 注意: 使用了 <see cref="AutofacRegisterAttribute"/> 标注的服务，会被排除在外
        /// 支持多次调用以添加不同的程序集扫描配置
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configure"></param>
        /// <returns></returns>
        public ModuleBuilder RegisterAssemblyBy<T>(Action<RegisterAssemblyBuilder> configure) where T : class
        {
            RegisterAssemblyBy(typeof(T), configure);
            return this;
        }

        /// <summary>
        /// 注册程序级中继承了泛型的实现类
        /// 注意: 使用了 <see cref="AutofacRegisterAttribute"/> 标注的服务，会被排除在外
        /// 支持多次调用以添加不同的程序集扫描配置
        /// </summary>
        /// <param name="type"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public ModuleBuilder RegisterAssemblyBy(Type type, Action<RegisterAssemblyBuilder> configure)
        {
            if (!type.IsClass && !type.IsInterface)
                throw new ArgumentException("type must be a class or interface", nameof(type));

            AssemblyFilterEntries.Add(new AssemblyFilterEntry(type, configure));
            return this;
        }

        /// <summary>
        /// 按命名约定扫描并注册程序集中的服务（例如所有以 "Service" 结尾的类匹配 "I*Service" 接口）
        /// 注意: 使用了 <see cref="AutofacRegisterAttribute"/> 标注的服务，会被排除在外
        /// 支持多次调用以添加不同的扫描规则
        /// </summary>
        /// <param name="predicate">类型筛选条件</param>
        /// <param name="configure">注册配置</param>
        /// <returns></returns>
        public ModuleBuilder RegisterAssemblyByConvention(Func<Type, bool> predicate, Action<RegisterAssemblyBuilder> configure)
        {
            PredicateFilterEntries.Add(new PredicateAssemblyFilterEntry(predicate, configure));
            return this;
        }

        /// <summary>
        /// 从指定程序集扫描所有标记了 <see cref="AutofacRegisterAttribute"/> 的服务并注册
        /// </summary>
        /// <param name="assembly">目标程序集</param>
        /// <returns></returns>
        public ModuleBuilder RegisterAllFromAssembly(Assembly assembly)
        {
            AdditionalAssemblies.Add(assembly);
            return this;
        }

        /// <summary>
        /// 检测注册类继承实现关系
        /// </summary>
        /// <param name="interface"></param>
        /// <param name="service"></param>
        /// <exception cref="ArgumentException"></exception>
        private static void CheckServiceAssignableFrom(Type @interface, Type service)
        {
            if (!@interface.IsInterface)
                throw new ArgumentException("must be an interface", nameof(@interface));

            var interfaces = service.GetAllInterfaces();
            if (service.IsGenericType)
            {
                if (!interfaces.Any(x => x.GetGenericTypeDefinition() == @interface))
                    throw new ArgumentException($"{service.Name} must assignable from {@interface.Name}");
            }
            else
            {
                if (interfaces == null || !interfaces.Contains(@interface))
                    throw new ArgumentException($"{service.Name} must assignable from {@interface.Name}");
            }
        }

        /// <summary>
        /// 仅注册自身
        /// </summary>
        /// <param name="type"></param>
        /// <param name="lifeTime"></param>
        /// <returns></returns>
        private ModuleBuilder RegisterSelf(Type type, ServiceLifeTime lifeTime)
        {
            RegisterContainer.Add(new LycorisRegisterService()
            {
                Type = type,
                Option = new AutofacRegisterAttribute(lifeTime)
                {
                    Self = true
                }
            });

            return this;
        }

        /// <summary>
        /// 仅注册自身
        /// </summary>
        /// <param name="type"></param>
        /// <param name="lifeTime"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        private ModuleBuilder RegisterSelf(Type type, ServiceLifeTime lifeTime, Action<AutofacSingleBuilder> configure)
        {
            var builder = new AutofacSingleBuilder();
            configure(builder);

            if (builder.Interceptors != null && builder.Interceptors.Any())
                builder.EnableInterceptor = true;

            RegisterContainer.Add(new LycorisRegisterService()
            {
                Type = type,
                Option = new AutofacRegisterAttribute(lifeTime)
                {
                    Self = true,
                    EnableInterceptor = builder.EnableInterceptor,
                    PropertiesAutowired = builder.PropertiesAutowired,
                    InterceptionType = builder.InterceptionType,
                    ExcludeInterceptor = builder.ExcludeInterceptor,
                },
                Interceptors = builder.Interceptors
            });

            return this;
        }

        /// <summary>
        /// 注册服务
        /// </summary>
        /// <param name="interface"></param>
        /// <param name="service"></param>
        /// <param name="lifeTime"></param>
        /// <param name="named"></param>
        /// <returns></returns>
        private ModuleBuilder RegisterAsType(Type @interface, Type service, ServiceLifeTime lifeTime, string? named = null)
        {
            CheckServiceAssignableFrom(@interface, service);

            RegisterContainer.Add(new LycorisRegisterService()
            {
                Type = service,
                AsType = @interface,
                Option = new AutofacRegisterAttribute(lifeTime)
                {
                    MultipleNamed = named
                }
            });

            return this;
        }

        /// <summary>
        /// 注册服务
        /// </summary>
        /// <param name="interface"></param>
        /// <param name="service"></param>
        /// <param name="lifeTime"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        private ModuleBuilder RegisterAsType(Type @interface, Type service, ServiceLifeTime lifeTime, Action<AutofacSingleBuilder> configure)
        {
            CheckServiceAssignableFrom(@interface, service);

            var builder = new AutofacSingleBuilder();
            configure(builder);

            if (builder.Interceptors != null && builder.Interceptors.Any())
                builder.EnableInterceptor = true;

            RegisterContainer.Add(new LycorisRegisterService()
            {
                Type = service,
                AsType = @interface,
                Option = new AutofacRegisterAttribute(lifeTime)
                {
                    EnableInterceptor = builder.EnableInterceptor,
                    PropertiesAutowired = builder.PropertiesAutowired,
                    MultipleNamed = builder.Named,
                    Key = builder.Key,
                    InterceptionType = builder.InterceptionType,
                    ExcludeInterceptor = builder.ExcludeInterceptor,
                },
                Interceptors = builder.Interceptors
            });

            return this;
        }
    }
}
