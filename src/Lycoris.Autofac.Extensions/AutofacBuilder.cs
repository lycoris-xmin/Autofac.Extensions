using Autofac;
using Autofac.Core;
using Castle.DynamicProxy;
using Lycoris.Autofac.Extensions.Extensions;
using Lycoris.Autofac.Extensions.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Lycoris.Autofac.Extensions
{
    /// <summary>
    /// Autofac服务构建类
    /// </summary>
    public sealed class AutofacBuilder
    {
        /// <summary>
        /// 全局AOP拦截器容器
        /// </summary>
        internal List<InterceptorOption> GlobalInterceptor { get; set; } = new();

        /// <summary>
        /// Autofac原生模块注册容器
        /// </summary>
        internal List<IModule> AutofacModules { get; set; } = new();

        /// <summary>
        /// LycorisAutofac扩展模块注册容器
        /// </summary>
        internal List<AutofacRegisterModule> LycorisRegisterModules { get; set; } = new();

        /// <summary>
        /// 多实现类服务获取服务 默认：false
        /// 考虑到部分人使用可能有自己的处理方法，所以此处不强制要求注册，可由配置进行注册
        /// </summary>
        public bool EnabledLycorisMultipleService { get; set; } = false;

        internal bool EnabledTaskExecutor { get; set; } = false;

        /// <summary>
        /// Autofac原生Module注册
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public AutofacBuilder AddAutofacModule<T>() where T : IModule, new()
        {
            AutofacModules.Add(new T());
            return this;
        }

        /// <summary>
        /// 添加Lycoris扩展Module
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public AutofacBuilder AddRegisterModule<T>() where T : AutofacRegisterModule, new()
        {
            LycorisRegisterModules.Add(new T());
            return this;
        }

        /// <summary>
        /// 添加全局AOP拦截器
        /// </summary>
        /// <typeparam name="TInterceptor"></typeparam>
        /// <param name="order"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public AutofacBuilder AddGlobalInterceptor<TInterceptor>(int? order = null) where TInterceptor : class, IInterceptor
        {
            if (order.HasValue && order.Value < 0)
                throw new ArgumentOutOfRangeException($"{nameof(order)} range must be greater than or equal to 0");

            var type = typeof(TInterceptor);
            if (!GlobalInterceptor.Any(x => x.Type == type))
            {
                order ??= GlobalInterceptor.Count;
                GlobalInterceptor.Add(new InterceptorOption()
                {
                    Type = type,
                    Order = order.Value
                });
            }

            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public AutofacBuilder AddTaskExecutor()
        {
            this.EnabledTaskExecutor = true;
            this.AddMultipleService();
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public AutofacBuilder AddMultipleService()
        {
            this.EnabledLycorisMultipleService = true;
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        internal void MicrosoftExtensionsBuilder(WebApplicationBuilder builder)
        {
            foreach (var item in LycorisRegisterModules)
            {
                item.HostRegister(builder.Host);
                item.SerivceRegister(builder.Services);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        internal void MicrosoftExtensionsBuilder(IHostBuilder builder)
        {
            builder.ConfigureServices((_, services) =>
            {
                foreach (var item in LycorisRegisterModules)
                {
                    item.SerivceRegister(services);
                }
            });
        }

        /// <summary>
        /// 构建并生成全部服务容器列表
        /// </summary>
        /// <returns></returns>
        internal List<LycorisRegisterService> GetAllLycorisRegisterService(ContainerBuilder containerBuilder)
        {
            var registerServices = new List<LycorisRegisterService>();

            AutofacModules.ForEach(x => containerBuilder.RegisterModuleIfNotRegistered(x));

            if (LycorisRegisterModules.Any())
            {
                foreach (var item in LycorisRegisterModules)
                {
                    // 获取需要注册的服务 
                    var services = item.Build(GlobalInterceptor);
                    registerServices.AddRange(services ?? new List<LycorisRegisterService>());
                }
            }

            return registerServices;
        }
    }
}