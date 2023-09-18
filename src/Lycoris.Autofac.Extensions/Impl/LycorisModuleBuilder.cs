using Castle.DynamicProxy;
using Castle.DynamicProxy.Internal;
using Lycoris.Autofac.Extensions.Options;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;

namespace Lycoris.Autofac.Extensions.Impl
{
    /// <summary>
    /// Lycoris扩展构建服务
    /// </summary>
    public sealed class LycorisModuleBuilder
    {
        internal readonly List<LycorisRegisterService> RegisterContainer = new();
        internal readonly List<InterceptorOption> InterceptorOptions = new();

        /// <summary>
        /// 为当前类库添加过滤器 
        /// 需要使用过滤器的服务，请将注册特性 <see cref="AutofacRegisterAttribute"/> 中的 <see langword="AutofacRegister(EnableInterceptor = true)"/> 属性设置为true, 否则过滤器不生效
        /// </summary>
        /// <typeparam name="TInterceptor"></typeparam>
        /// <param name="order"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public LycorisModuleBuilder InterceptedBy<TInterceptor>(int? order = null) where TInterceptor : class, IInterceptor
        {
            if (order.HasValue && order.Value < 0)
                throw new ArgumentOutOfRangeException(nameof(order), "range must be greater than or equal to 0");

            order ??= InterceptorOptions.Count;

            InterceptorOptions.Add(new InterceptorOption() { Type = typeof(TInterceptor), Order = order.Value });

            return this;
        }

        /// <summary>
        /// 注册瞬态服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public LycorisModuleBuilder RegisterTransient<T>() where T : class
            => RegisterSelf(typeof(T), ServiceLifeTime.Transient);

        /// <summary>
        /// 注册瞬态服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configure"></param>
        /// <returns></returns>
        public LycorisModuleBuilder RegisterTransient<T>(Action<AutofacSingleBuilder> configure) where T : class
            => RegisterSelf(typeof(T), ServiceLifeTime.Transient, configure);

        /// <summary>
        /// 注册瞬态服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TImpl"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public LycorisModuleBuilder RegisterTransient<T, TImpl>() where TImpl : T where T : class
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
        public LycorisModuleBuilder RegisterTransient<T, TImpl>([NotNull] string named) where TImpl : T where T : class
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
        public LycorisModuleBuilder RegisterTransient<T, TImpl>(Action<AutofacSingleBuilder> configure) where TImpl : T where T : class
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
        public LycorisModuleBuilder RegisterTransient(Type type)
            => RegisterSelf(type, ServiceLifeTime.Transient);

        /// <summary>
        /// 注册瞬态服务
        /// </summary>
        /// <param name="type"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public LycorisModuleBuilder RegisterTransient(Type type, Action<AutofacSingleBuilder> configure)
            => RegisterSelf(type, ServiceLifeTime.Transient, configure);

        /// <summary>
        /// 注册瞬态服务
        /// </summary>
        /// <param name="interface"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public LycorisModuleBuilder RegisterTransient(Type @interface, Type service)
            => RegisterAsType(@interface, service, ServiceLifeTime.Transient);

        /// <summary>
        /// 注册瞬态服务
        /// </summary>
        /// <param name="interface"></param>
        /// <param name="service"></param>
        /// <param name="named"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public LycorisModuleBuilder RegisterTransient(Type @interface, Type service, [NotNull] string named)
            => RegisterAsType(@interface, service, ServiceLifeTime.Transient, named);

        /// <summary>
        /// 注册瞬态服务
        /// </summary>
        /// <param name="interface"></param>
        /// <param name="service"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public LycorisModuleBuilder RegisterTransient(Type @interface, Type service, Action<AutofacSingleBuilder> configure)
            => RegisterAsType(@interface, service, ServiceLifeTime.Transient, configure);


        /// <summary>
        /// 注册作用域服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public LycorisModuleBuilder RegisterScoped<T>() where T : class
          => RegisterSelf(typeof(T), ServiceLifeTime.Scoped);

        /// <summary>
        /// 注册作用域服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configure"></param>
        /// <returns></returns>
        public LycorisModuleBuilder RegisterScoped<T>(Action<AutofacSingleBuilder> configure) where T : class
          => RegisterSelf(typeof(T), ServiceLifeTime.Scoped, configure);

        /// <summary>
        /// 注册作用域服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TImpl"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public LycorisModuleBuilder RegisterScoped<T, TImpl>() where TImpl : T where T : class
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
        public LycorisModuleBuilder RegisterScoped<T, TImpl>(string named) where TImpl : T where T : class
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
        public LycorisModuleBuilder RegisterScoped<T, TImpl>(Action<AutofacSingleBuilder> configure) where TImpl : T where T : class
        {
            if (!typeof(T).IsInterface)
                throw new ArgumentException("must be an interface", nameof(T));

            return RegisterAsType(typeof(T), typeof(TImpl), ServiceLifeTime.Scoped, configure);
        }

        /// <summary>
        /// 注册作用域服务
        /// </summary>
        /// <returns></returns>
        public LycorisModuleBuilder RegisterScoped(Type type)
            => RegisterSelf(type, ServiceLifeTime.Scoped);

        /// <summary>
        /// 注册作用域服务
        /// </summary>
        /// <param name="type"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public LycorisModuleBuilder RegisterScoped(Type type, Action<AutofacSingleBuilder> configure)
            => RegisterSelf(type, ServiceLifeTime.Scoped, configure);

        /// <summary>
        /// 注册作用域服务
        /// </summary>
        /// <param name="interface"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public LycorisModuleBuilder RegisterScoped(Type @interface, Type service)
            => RegisterAsType(@interface, service, ServiceLifeTime.Scoped);

        /// <summary>
        /// 注册作用域服务
        /// </summary>
        /// <param name="interface"></param>
        /// <param name="service"></param>
        /// <param name="named"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public LycorisModuleBuilder RegisterScoped(Type @interface, Type service, [NotNull] string named)
            => RegisterAsType(@interface, service, ServiceLifeTime.Scoped, named);

        /// <summary>
        /// 注册作用域服务
        /// </summary>
        /// <param name="interface"></param>
        /// <param name="service"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public LycorisModuleBuilder RegisterScoped(Type @interface, Type service, Action<AutofacSingleBuilder> configure)
            => RegisterAsType(@interface, service, ServiceLifeTime.Scoped, configure);


        /// <summary>
        /// 注册单例服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public LycorisModuleBuilder RegisterSingleton<T>() where T : class
          => RegisterSelf(typeof(T), ServiceLifeTime.Singleton);

        /// <summary>
        /// 注册单例服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configure"></param>
        /// <returns></returns>
        public LycorisModuleBuilder RegisterSingleton<T>(Action<AutofacSingleBuilder> configure) where T : class
          => RegisterSelf(typeof(T), ServiceLifeTime.Singleton, configure);

        /// <summary>
        /// 注册单例服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TImpl"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public LycorisModuleBuilder RegisterSingleton<T, TImpl>() where TImpl : T where T : class
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
        public LycorisModuleBuilder RegisterSingleton<T, TImpl>(string named) where TImpl : T where T : class
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
        public LycorisModuleBuilder RegisterSingleton<T, TImpl>(Action<AutofacSingleBuilder> configure) where TImpl : T where T : class
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
        public LycorisModuleBuilder RegisterSingleton(Type type)
            => RegisterSelf(type, ServiceLifeTime.Singleton);

        /// <summary>
        /// 注册单例服务
        /// </summary>
        /// <param name="type"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public LycorisModuleBuilder RegisterSingleton(Type type, Action<AutofacSingleBuilder> configure)
            => RegisterSelf(type, ServiceLifeTime.Singleton, configure);

        /// <summary>
        /// 注册单例服务
        /// </summary>
        /// <param name="interface"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public LycorisModuleBuilder RegisterSingleton(Type @interface, Type service)
            => RegisterAsType(@interface, service, ServiceLifeTime.Singleton);

        /// <summary>
        /// 注册单例服务
        /// </summary>
        /// <param name="interface"></param>
        /// <param name="service"></param>
        /// <param name="named"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public LycorisModuleBuilder RegisterSingleton(Type @interface, Type service, [NotNull] string named)
            => RegisterAsType(@interface, service, ServiceLifeTime.Singleton, named);

        /// <summary>
        /// 注册单例服务
        /// </summary>
        /// <param name="interface"></param>
        /// <param name="service"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public LycorisModuleBuilder RegisterSingleton(Type @interface, Type service, Action<AutofacSingleBuilder> configure)
            => RegisterAsType(@interface, service, ServiceLifeTime.Singleton, configure);


        /// <summary>
        /// 注册同步拦截器，需要继承 <see cref="IInterceptor"/> 接口并实现
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public LycorisModuleBuilder RegisterInterceptor<T>() where T : class, IInterceptor
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
        public LycorisModuleBuilder RegisterAsyncInterceptor<T>() where T : class, IAsyncInterceptor
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
        /// 注册启动任务，需要继承 <see cref="IHostedService"/> 接口并实现
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public LycorisModuleBuilder RegisterHostedService<T>() where T : class, IHostedService
        {
            RegisterContainer.Add(new LycorisRegisterService()
            {
                Type = typeof(T),
                AsType = typeof(IHostedService),
                Option = new AutofacRegisterAttribute(ServiceLifeTime.Transient)
            });
            return this;
        }

        internal Type? AssemblyFilterType { get; private set; } = null;
        internal Action<RegisterAssemblyBuilder>? AssemblyConfigure { get; private set; } = null;

        /// <summary>
        /// 注册程序级中继承了泛型的实现类
        /// 注意: 使用了 <see cref="AutofacRegisterAttribute"/> 标注的服务，会被排除在外
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configure"></param>
        /// <returns></returns>
        public LycorisModuleBuilder RegisterAssemblyBy<T>(Action<RegisterAssemblyBuilder> configure) where T : class
        {
            RegisterAssemblyBy(typeof(T), configure);
            return this;
        }

        /// <summary>
        /// 注册程序级中继承了泛型的实现类
        /// 注意: 使用了 <see cref="AutofacRegisterAttribute"/> 标注的服务，会被排除在外</summary>
        /// <param name="type"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public LycorisModuleBuilder RegisterAssemblyBy(Type type, Action<RegisterAssemblyBuilder> configure)
        {
            if (!type.IsClass && !type.IsInterface)
                throw new Exception("type must be class or interface");

            this.AssemblyFilterType = type;
            this.AssemblyConfigure = configure;
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
        private LycorisModuleBuilder RegisterSelf(Type type, ServiceLifeTime lifeTime)
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
        private LycorisModuleBuilder RegisterSelf(Type type, ServiceLifeTime lifeTime, Action<AutofacSingleBuilder> configure)
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
        private LycorisModuleBuilder RegisterAsType(Type @interface, Type service, ServiceLifeTime lifeTime, string? named = null)
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
        private LycorisModuleBuilder RegisterAsType(Type @interface, Type service, ServiceLifeTime lifeTime, Action<AutofacSingleBuilder> configure)
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
                    MultipleNamed = builder.Named
                },
                Interceptors = builder.Interceptors
            });

            return this;
        }
    }
}
