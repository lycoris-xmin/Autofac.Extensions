using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Core.Registration;
using Autofac.Extras.DynamicProxy;
using Autofac.Features.Scanning;
using Castle.DynamicProxy.Internal;
using Lycoris.Autofac.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Lycoris.Autofac.Extensions.Extensions
{
    /// <summary>
    /// Autofac注册相关扩展
    /// </summary>
    internal static class RegistrationExtensions
    {
        static readonly ConcurrentDictionary<string, int> s_KeyValues = new();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="module"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        internal static IModuleRegistrar RegisterModuleIfNotRegistered([NotNull] this ContainerBuilder builder, IModule module)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (module == null)
                throw new ArgumentNullException(nameof(module));

            var modelName = module.GetType().FullName ?? throw new ArgumentException($"autofac register module fail:{nameof(module)}");

            if (s_KeyValues.ContainsKey(modelName))
                return builder.RegisterModule<NullModule>();

            if (s_KeyValues.TryAdd(modelName, 1))
                return builder.RegisterModule(module);

            throw new ArgumentException($"autofac register module fail:{modelName}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TModule"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        internal static IModuleRegistrar RegisterModuleIfNotRegistered<TModule>([NotNull] this ContainerBuilder builder) where TModule : IModule, new()
            => builder.RegisterModuleIfNotRegistered(new TModule());

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="interceptors"></param>
        /// <returns></returns>
        internal static List<LycorisRegisterService> GetLycorisRegisterServiceList([NotNull] this Assembly assembly, List<InterceptorOption>? interceptors = null)
        {
            var tempList = assembly.GetTypes()
                                   .Where(x => x.GetCustomAttributes(typeof(AutofacRegisterAttribute), false).Length > 0)
                                   .Where(x => x.IsClass && !x.IsAbstract)
                                   .Select(x => new LycorisRegisterService()
                                   {
                                       Option = x.GetCustomAttribute<AutofacRegisterAttribute>(),
                                       Type = x
                                   })
                                   .Where(x => x.Option != null).ToList();

            if (tempList == null || !tempList.Any())
                return new List<LycorisRegisterService>();

            var list = new List<LycorisRegisterService>();
            foreach (var item in tempList)
            {
                if (item.Option == null || item.Type == null)
                    continue;

                if (item.AsType != null && !item.AsType.IsInterface)
                    throw new ArgumentException("must be an interface", item.AsType.Name);

                // 获取当前实现类所有继承接口
                var itypes = item.Type.GetAllInterfaces();

                // 1. 先获取配置上的接口对象
                var itype = item.Option.Interface;
                if (itype != null && !itypes.Any(x => x == item.Option.Interface))
                    throw new ArgumentNullException($"the interface '{item.Option.Interface!.Name}' was not found among the interfaces inherited by '{item.Type.Name}'");

                // 2. 如果接口对象为空,且不是注册自身的情况下,则继续执行下面部分
                if (itype == null && !item.Option.Self)
                {
                    // 如果继承接口不为空
                    if (itypes != null && itypes.Any())
                    {
                        // 继承接口大于1个
                        if (itypes.Length > 1)
                        {
                            // 优先获取名字包含当前实现类的服务
                            // 例：实现类 AService 接口 IAService
                            itype = itypes.Where(x => x.Name.EndsWith(item.Type.Name)).FirstOrDefault();

                            // 如果上述还获取不到，则获取最后一个继承的接口作为注册接口
                            if (itype == null)
                                itype = itypes.LastOrDefault();
                        }
                        else
                            itype = itypes[0];
                    }
                    else
                        item.Option.Self = true;
                }

                // 如果是泛型，则取泛型类定义属性作为注册的类型
                if (itype != null && itype.IsGenericType)
                    itype = itype.GetGenericTypeDefinition();

                // 添加至注册配置列表
                list.Add(new LycorisRegisterService()
                {
                    Type = item.Type,
                    AsType = itype,
                    Option = item.Option,
                    Interceptors = interceptors
                });
            }

            return list;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="services"></param>
        /// <param name="interceptorOption"></param>
        internal static void AutoFacRegister([NotNull] this ContainerBuilder builder, List<LycorisRegisterService> services, List<InterceptorOption>? interceptorOption = null)
        {
            try
            {
                if (services != null && services.Any())
                {
                    foreach (var item in services)
                    {
                        if (item.Option == null || item.Type == null)
                            continue;

                        var temp = interceptorOption ?? new List<InterceptorOption>();
                        temp.AddRange(item.Interceptors ?? new List<InterceptorOption>());
                        // 处理AOP拦截，过滤重复的，过滤器拦截顺序排序
                        var interceptors = GetAllInterceptor(item.Option, temp);

                        if (item.Option.ServiceLifeTime == ServiceLifeTime.Transient)
                            builder.TransientServiceRegister(item.Type, item.AsType, item.Option, interceptors);
                        else if (item.Option.ServiceLifeTime == ServiceLifeTime.Scoped)
                            builder.ScopedServiceRegister(item.Type, item.AsType, item.Option, interceptors);
                        else
                            builder.SingletonServiceRegister(item.Type, item.AsType, item.Option, interceptors);
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        internal static IRegistrationBuilder<TLimit, TScanningActivatorData, TRegistrationStyle> WhereIf<TLimit, TScanningActivatorData, TRegistrationStyle>(this IRegistrationBuilder<TLimit, TScanningActivatorData, TRegistrationStyle> registration, bool condition, Func<Type, bool> predicate) where TScanningActivatorData : ScanningActivatorData
            => condition ? registration.Where(predicate) : registration;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="type"></param>
        /// <param name="interface"></param>
        /// <param name="option"></param>
        /// <param name="interceptors"></param>
        internal static void TransientServiceRegister([NotNull] this ContainerBuilder builder, Type type, Type? @interface, AutofacRegisterAttribute option, Type[]? interceptors)
        {
            if (type == null || option == null)
                return;

            if (type != null && @interface != null)
            {
                if (type.IsGenericType && (@interface != null && @interface.IsGenericType))
                    builder.GenericServiceOptionsBuilder(type, @interface, option, interceptors)?.InstancePerDependency();
                else
                    builder.ServiceOptionsBuilder(type, @interface, option, interceptors)?.InstancePerDependency();
            }
            else if (type != null)
            {
                if (type.IsGenericType)
                    builder.GenericServiceOptionsBuilder(type, @interface, option, interceptors)?.InstancePerDependency();
                else
                    builder.ServiceOptionsBuilder(type, @interface, option, interceptors)?.InstancePerDependency();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="type"></param>
        /// <param name="interface"></param>
        /// <param name="option"></param>
        /// <param name="interceptors"></param>
        internal static void ScopedServiceRegister([NotNull] this ContainerBuilder builder, Type type, Type? @interface, AutofacRegisterAttribute option, Type[]? interceptors)
        {
            if (type == null || option == null)
                return;

            if (type != null && @interface != null)
            {
                if (type.IsGenericType && (@interface != null && @interface.IsGenericType))
                    builder.GenericServiceOptionsBuilder(type, @interface, option, interceptors)?.InstancePerLifetimeScope();
                else
                    builder.ServiceOptionsBuilder(type, @interface, option, interceptors)?.InstancePerLifetimeScope();
            }
            else if (type != null)
            {
                if (type.IsGenericType)
                    builder.GenericServiceOptionsBuilder(type, @interface, option, interceptors)?.InstancePerLifetimeScope();
                else
                    builder.ServiceOptionsBuilder(type, @interface, option, interceptors)?.InstancePerLifetimeScope();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="type"></param>
        /// <param name="interface"></param>
        /// <param name="option"></param>
        /// <param name="interceptors"></param>
        internal static void SingletonServiceRegister([NotNull] this ContainerBuilder builder, Type type, Type? @interface, AutofacRegisterAttribute option, Type[]? interceptors)
        {
            if (type == null || option == null)
                return;

            if (type != null && @interface != null)
            {
                if (type.IsGenericType && (@interface != null && @interface.IsGenericType))
                    builder.GenericServiceOptionsBuilder(type, @interface, option, interceptors)?.SingleInstance();
                else
                    builder.ServiceOptionsBuilder(type, @interface, option, interceptors)?.SingleInstance();
            }
            else if (type != null)
            {
                if (type.IsGenericType)
                    builder.GenericServiceOptionsBuilder(type, @interface, option, interceptors)?.SingleInstance();
                else
                    builder.ServiceOptionsBuilder(type, @interface, option, interceptors)?.SingleInstance();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="type"></param>
        /// <param name="interface"></param>
        /// <param name="option"></param>
        /// <param name="interceptors"></param>
        /// <returns></returns>
        internal static IRegistrationBuilder<object, ConcreteReflectionActivatorData, SingleRegistrationStyle>? ServiceOptionsBuilder([NotNull] this ContainerBuilder builder, Type type, Type? @interface, AutofacRegisterAttribute option, Type?[]? interceptors)
        {
            //
            var build = builder.RegisterType(type);

            // 注入
            // 1. 接口类型不为空
            // 2. 特性没有指定自身注入
            // 3. 不是AOP拦截类型
            if (@interface != null && !option.Self && !option.IsInterceptor)
                build = string.IsNullOrEmpty(option.MultipleNamed) ? build.As(@interface) : build.Named(option.MultipleNamed, @interface);
            else
                build = build.AsSelf();

            // 属性注入
            if (option.PropertiesAutowired)
                build = build.PropertiesAutowired();

            // 允许Aop拦截,且当前要注入的非Aop拦截服务
            if (option.EnableInterceptor && !option.IsInterceptor && interceptors != null && interceptors.Any())
                build = build.EnableInterfaceInterceptors().InterceptedBy(interceptors!);

            return build;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="type"></param>
        /// <param name="interface"></param>
        /// <param name="option"></param>
        /// <param name="interceptors"></param>
        /// <returns></returns>
        internal static IRegistrationBuilder<object, ReflectionActivatorData, DynamicRegistrationStyle>? GenericServiceOptionsBuilder([NotNull] this ContainerBuilder builder, Type type, Type? @interface, AutofacRegisterAttribute option, Type?[]? interceptors)
        {
            //
            var build = builder.RegisterGeneric(type);

            // 注入
            // 1. 接口类型不为空
            // 2. 特性没有指定自身注入
            // 3. 不是AOP拦截类型
            if (@interface != null && !option.Self && !option.IsInterceptor)
                build = string.IsNullOrEmpty(option.MultipleNamed) ? build.As(@interface) : build.Named(option.MultipleNamed, @interface);
            else
                build = build.AsSelf();

            // 属性注入
            if (option.PropertiesAutowired)
                build = build.PropertiesAutowired();

            // 允许Aop拦截,且当前要注入的非Aop拦截服务
            if (option.EnableInterceptor && !option.IsInterceptor && interceptors != null && interceptors.Any())
                build = build.EnableInterfaceInterceptors().InterceptedBy(interceptors!);

            return build;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="option"></param>
        /// <param name="interceptors"></param>
        /// <returns></returns>
        static Type[]? GetAllInterceptor(AutofacRegisterAttribute option, List<InterceptorOption>? interceptors)
        {
            interceptors = interceptors?.DistinctBy(x => x.Type).ToList() ?? new List<InterceptorOption>();

            if (option.Interceptor != null)
            {
                var _tmp = interceptors.FirstOrDefault(x => x.Type == option.Interceptor);
                if (_tmp == null)
                {
                    interceptors.Add(new InterceptorOption()
                    {
                        Type = option.Interceptor,
                        Order = option.InterceptorOrder ?? -1
                    });
                }
                else if (option.InterceptorOrder.HasValue)
                    _tmp.Order = option.InterceptorOrder.Value;
            }

            return interceptors.Where(x => x.Type != null).OrderBy(x => x.Order).Select(x => x.Type).Distinct().ToArray() ?? Array.Empty<Type>();
        }

        /// <summary>
        /// 服务注册
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="services"></param>
        internal static void ServiceRegister(this ContainerBuilder builder, List<LycorisRegisterService> services)
        {
            if (!services.Any())
                return;

            foreach (var item in services)
            {
                if (item.Type == null || item.Option == null)
                    continue;

                // 去重
                Type[]? interceptors = null;
                if (item.Interceptors != null && item.Interceptors.Any())
                {
                    item.Interceptors = item.Interceptors.DistinctBy(x => x.Type).OrderBy(x => x.Order).ToList();
                    interceptors = item.Interceptors.Select(x => x.Type).ToArray();
                }

                switch (item.Option.ServiceLifeTime)
                {
                    case ServiceLifeTime.Transient:
                        builder.TransientServiceRegister(item.Type, item.AsType, item.Option, interceptors);
                        break;
                    case ServiceLifeTime.Scoped:
                        builder.ScopedServiceRegister(item.Type, item.AsType, item.Option, interceptors);
                        break;
                    case ServiceLifeTime.Singleton:
                        builder.SingletonServiceRegister(item.Type, item.AsType, item.Option, interceptors);
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// 服务去重处理
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        internal static List<LycorisRegisterService> ServiceDeduplication(this List<LycorisRegisterService>? services)
        {
            if (services == null || !services.Any())
                return new List<LycorisRegisterService>();

            var groups = services.GroupBy(x => x.Type).ToList();
            if (groups.All(x => x.Count() == 1))
                return services;

            var servicesTemp = new List<LycorisRegisterService>();
            foreach (var item in groups)
            {
                if (item.Count() > 1)
                {
                    var tmp = item.DistinctBy(x => x.AsType).ToList();
                    servicesTemp.AddRange(tmp);
                }
                else if (item.Any())
                {
                    servicesTemp.Add(item.FirstOrDefault()!);
                }
            }

            return servicesTemp;
        }
    }
}
