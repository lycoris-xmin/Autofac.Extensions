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
        public virtual void SerivceRegister(IServiceCollection services) { }

        /// <summary>
        /// Lycoris扩展模块注册
        /// </summary>
        /// <param name="builder"></param>
        public virtual void ModuleRegister(LycorisModuleBuilder builder) { }

        /// <summary>
        /// 构建
        /// </summary>
        /// <param name="globalInterceptor"></param>
        /// <returns></returns>
        internal List<LycorisRegisterService> Build(List<InterceptorOption>? globalInterceptor = null)
        {
            var Builder = new LycorisModuleBuilder();

            // 执行模块注册方法
            ModuleRegister(Builder);

            var assembly = GetType().Assembly;

            // 
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
                        item.Interceptors = item.Interceptors.OrderBy(x => x.Order).DistinctBy(x => x.Type).ToList();
                    }
                }
            }

            var services = assembly.GetLycorisRegisterServiceList(Builder.InterceptorOptions);

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
        private static void RegisterAssembly(Assembly assembly, LycorisModuleBuilder builder)
        {
            if (builder.AssemblyConfigure != null)
            {
                //
                var query = assembly.GetTypes()
                                    .Where(x => x.GetCustomAttributes(typeof(AutofacRegisterAttribute), false).Length == 0)
                                    .Where(x => x.IsClass && !x.IsAbstract);

                if (builder.AssemblyFilterType!.IsInterface)
                {
                    query = query.Where(x => IsInterfaceFrom(x, builder.AssemblyFilterType));
                }
                else
                {
                    query = query.Where(x => x.IsSubclassOf(builder.AssemblyFilterType));
                }

                var types = query.Select(x => x).ToList();

                if (types == null || !types.Any())
                    return;

                var option = new RegisterAssemblyBuilder();

                builder.AssemblyConfigure(option);

                if (!builder.AssemblyFilterType!.IsInterface)
                    option.Self = true;

                var _builder = option.BuildAutofacSingleBuilder();

                foreach (var type in types)
                {
                    Type? itype = null;

                    if (!option.Self)
                    {
                        var itypes = type!.GetAllInterfaces();

                        // 如果继承接口不为空
                        if (itypes != null && itypes.Any())
                        {
                            // 继承接口大于1个
                            if (itypes.Length > 1)
                            {
                                // 优先获取名字包含当前实现类的服务
                                // 例：实现类 AService 接口 IAService
                                itype = itypes.Where(x => x.Name.EndsWith(type.Name)).FirstOrDefault();

                                // 如果上述还获取不到，则获取最后一个继承的接口作为注册接口
                                if (itype == null)
                                    itype = itypes.LastOrDefault();
                            }
                            else
                                itype = itypes[0];
                        }

                        // 如果是泛型，则取泛型类定义属性作为注册的类型
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
