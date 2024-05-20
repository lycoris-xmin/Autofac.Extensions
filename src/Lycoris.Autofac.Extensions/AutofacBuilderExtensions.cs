using Autofac;
using Autofac.Extensions.DependencyInjection;
using Lycoris.Autofac.Extensions.Extensions;
using Lycoris.Autofac.Extensions.Impl;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Lycoris.Autofac.Extensions
{
    /// <summary>
    /// Autofac扩展类
    /// </summary>
    public static class AutofacBuilderExtensions
    {
        /// <summary>
        /// 注册扩展
        /// </summary>
        /// <param name="appBuilder"></param>
        /// <param name="configure">注册相关配置</param>
        /// <returns></returns>
        public static WebApplicationBuilder UseAutofacExtensions(this WebApplicationBuilder appBuilder, Action<AutofacBuilder> configure)
        {
            var autofacBuilder = new AutofacBuilder();

            // 执行注册配置构建操作
            configure.Invoke(autofacBuilder);

            // 执行系统相关配置操作
            autofacBuilder.MicrosoftExtensionsBuilder(appBuilder);

            // 使用autofac的容器工厂替换系统默认的容器
            appBuilder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())
                      .ConfigureContainer<ContainerBuilder>(builder =>
                      {
                          // 注册多实现类服务配置
                          if (autofacBuilder.EnabledLycorisMultipleService)
                              builder.RegisterType<AutofacMultipleService>().As<IAutofacMultipleService>().SingleInstance();

                          // 获取所有待注册的服务信息
                          var services = autofacBuilder.GetAllLycorisRegisterService(builder);

                          // 服务注册
                          builder.ServiceRegister(services.ServiceDeduplication());
                      });

            return appBuilder;
        }
    }
}
