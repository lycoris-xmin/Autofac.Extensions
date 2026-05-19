using Castle.DynamicProxy;
using Castle.DynamicProxy.Internal;
using Lycoris.Autofac.Extensions.Extensions;
using Lycoris.Autofac.Extensions.Impl;
using Lycoris.Autofac.Extensions.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Lycoris.Autofac.Extensions
{
    /// <summary>
    /// Lycoris扩展模块
    /// </summary>
    public abstract class AutofacRegisterModule
    {
        /// <summary>
        /// Host 扩展服务注册
        /// </summary>
        /// <param name="host"></param>
        public virtual void HostRegister(ConfigureHostBuilder host) { }

        /// <summary>
        /// IServiceCollection 扩展服务注册
        /// </summary>
        /// <param name="services"></param>
        public virtual void ServiceRegister(IServiceCollection services) { }

        /// <summary>
        /// Lycoris扩展模块注册
        /// </summary>
        /// <param name="builder"></param>
        public virtual void ModuleRegister(ModuleBuilder builder) { }

        /// <summary>
        /// 构建
        /// </summary>
        /// <param name="globalInterceptor"></param>
        /// <returns></returns>
        internal List<LycorisRegisterService> Build(List<InterceptorOption>? globalInterceptor = null)
        {
            var Builder = new ModuleBuilder();

            // 执行模块注册方法
            ModuleRegister(Builder);

            var assembly = GetType().Assembly;

            // 自动扫描拦截器
            if (Builder.AutoRegisterInterceptors)
            {
                var interceptorTypes = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract)
                    .Where(t => typeof(IInterceptor).IsAssignableFrom(t) || typeof(IAsyncInterceptor).IsAssignableFrom(t));

                foreach (var type in interceptorTypes)
                {
                    Builder.RegisterContainer.Add(new LycorisRegisterService()
                    {
                        Type = type,
                        Option = new AutofacRegisterAttribute(ServiceLifeTime.Scoped)
                        {
                            Self = true
                        }
                    });
                }
            }

            // 程序集扫描
            RegisterAssembly(assembly, Builder);

            // 判断模块是否有AOP拦截器相关的配置
            if (Builder.InterceptorOptions != null && Builder.InterceptorOptions.Count > 0)
            {
                // 存在拦截器相关配置，则将模块中的服务添加拦截器相关配置
                foreach (var item in Builder.RegisterContainer)
                {
                    if (item.Option != null && item.Option.EnableInterceptor)
                    {
                        item.Interceptors ??= new List<InterceptorOption>();
                        item.Interceptors.AddRange(Builder.InterceptorOptions);
                        item.Interceptors = RegistrationExtensions.NormalizeInterceptors(item.Interceptors, item.Option.ExcludeInterceptor);
                    }
                }
            }

            var services = assembly.GetLycorisRegisterServiceList(Builder.InterceptorOptions);

            // 扫描额外程序集
            foreach (var additionalAssembly in Builder.AdditionalAssemblies)
            {
                var additionalServices = additionalAssembly.GetLycorisRegisterServiceList(Builder.InterceptorOptions);
                services.AddRange(additionalServices);
            }

            services.AddRange(Builder.RegisterContainer);

            if (globalInterceptor != null && globalInterceptor.Any())
            {
                services.ForEach(x =>
                {
                    x.Interceptors ??= new List<InterceptorOption>();
                    x.Interceptors.AddRange(globalInterceptor);
                });
            }

            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="builder"></param>
        private static void RegisterAssembly(Assembly assembly, ModuleBuilder builder)
        {
            // 基类/接口过滤扫描
            foreach (var entry in builder.AssemblyFilterEntries)
            {
                var query = assembly.GetTypes()
                                    .Where(x => x.GetCustomAttributes(typeof(AutofacRegisterAttribute), false).Length == 0)
                                    .Where(x => x.IsClass && !x.IsAbstract);

                if (entry.FilterType.IsInterface)
                {
                    query = query.Where(x => IsInterfaceFrom(x, entry.FilterType));
                }
                else
                {
                    query = query.Where(x => x.IsSubclassOf(entry.FilterType));
                }

                var types = query.ToList();

                if (types == null || !types.Any())
                    continue;

                var option = new RegisterAssemblyBuilder();

                entry.Configure(option);

                if (!entry.FilterType.IsInterface)
                    option.Self = true;

                var _builder = option.BuildAutofacSingleBuilder();

                foreach (var type in types)
                {
                    Type? itype = null;

                    if (!option.Self)
                    {
                        var itypes = type!.GetAllInterfaces();

                        if (itypes != null && itypes.Any())
                        {
                            if (itypes.Length > 1)
                            {
                                itype = itypes.Where(x => x.Name.EndsWith(type.Name)).FirstOrDefault();

                                if (itype == null)
                                    itype = itypes.LastOrDefault();
                            }
                            else
                                itype = itypes[0];
                        }

                        if (itype != null && itype.IsGenericType)
                            itype = itype.GetGenericTypeDefinition();
                    }

                    switch (option.ServiceLifeTime)
                    {
                        case ServiceLifeTime.Transient:
                            {
                                if (option.Self || itype == null)
                                    builder.RegisterTransient(type);
                                else
                                    builder.RegisterTransient(itype, type, _builder);
                            }
                            break;
                        case ServiceLifeTime.Scoped:
                            {
                                if (option.Self || itype == null)
                                    builder.RegisterScoped(type);
                                else
                                    builder.RegisterScoped(itype, type, _builder);
                            }
                            break;
                        case ServiceLifeTime.Singleton:
                            {
                                if (option.Self || itype == null)
                                    builder.RegisterSingleton(type);
                                else
                                    builder.RegisterSingleton(itype, type, _builder);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            // 条件断言过滤扫描
            foreach (var entry in builder.PredicateFilterEntries)
            {
                var query = assembly.GetTypes()
                                    .Where(x => x.GetCustomAttributes(typeof(AutofacRegisterAttribute), false).Length == 0)
                                    .Where(x => x.IsClass && !x.IsAbstract)
                                    .Where(entry.Predicate);

                var types = query.ToList();

                if (types == null || !types.Any())
                    continue;

                var option = new RegisterAssemblyBuilder();

                entry.Configure(option);

                var _builder = option.BuildAutofacSingleBuilder();

                foreach (var type in types)
                {
                    Type? itype = null;

                    if (!option.Self)
                    {
                        var itypes = type!.GetAllInterfaces();

                        if (itypes != null && itypes.Any())
                        {
                            if (itypes.Length > 1)
                            {
                                itype = itypes.Where(x => x.Name.EndsWith(type.Name)).FirstOrDefault();

                                if (itype == null)
                                    itype = itypes.LastOrDefault();
                            }
                            else
                                itype = itypes[0];
                        }

                        if (itype != null && itype.IsGenericType)
                            itype = itype.GetGenericTypeDefinition();
                    }

                    switch (option.ServiceLifeTime)
                    {
                        case ServiceLifeTime.Transient:
                            {
                                if (option.Self || itype == null)
                                    builder.RegisterTransient(type);
                                else
                                    builder.RegisterTransient(itype, type, _builder);
                            }
                            break;
                        case ServiceLifeTime.Scoped:
                            {
                                if (option.Self || itype == null)
                                    builder.RegisterScoped(type);
                                else
                                    builder.RegisterScoped(itype, type, _builder);
                            }
                            break;
                        case ServiceLifeTime.Singleton:
                            {
                                if (option.Self || itype == null)
                                    builder.RegisterSingleton(type);
                                else
                                    builder.RegisterSingleton(itype, type, _builder);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// 判断一个类是否实现了某个接口
        /// </summary>
        /// <param name="type"></param>
        /// <param name="interface"></param>
        /// <returns></returns>
        public static bool IsInterfaceFrom(Type type, Type @interface)
        {
            var intarfaces = type.GetInterfaces();
            if (intarfaces == null || intarfaces.Length == 0)
                return false;

            if (@interface.IsGenericType)
            {
                foreach (var item in intarfaces)
                {
                    if (item.GetGenericTypeDefinition() == @interface)
                        return true;
                }
            }
            else
                return intarfaces.Any(x => x == @interface);

            return false;
        }
    }
}
