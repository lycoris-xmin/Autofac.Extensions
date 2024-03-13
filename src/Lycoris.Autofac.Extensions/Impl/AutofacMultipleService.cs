using Autofac;
using Microsoft.Extensions.DependencyInjection;

namespace Lycoris.Autofac.Extensions.Impl
{
    /// <summary>
    /// 接口多实现服务类
    /// </summary>
    public sealed class AutofacMultipleService : IAutofacMultipleService
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="serviceProvider"></param>
        public AutofacMultipleService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 获取服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">多实现服务别名</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public T GetService<T>(string name) where T : notnull
        {
            var context = _serviceProvider.GetService<IComponentContext>();

            var service = context!.ResolveNamed<T>(name);

            return service == null ? throw new ArgumentNullException($"the interface implementation named:{name} could not be found") : service;
        }

        /// <summary>
        /// 尝试获取服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">多实现服务别名</param>
        /// <returns></returns>
        public T? TryGetService<T>(string name) where T : notnull
        {
            try
            {
                var context = _serviceProvider.GetService<IComponentContext>();

                return context!.ResolveNamed<T>(name);
            }
            catch
            {
                return default;
            }
        }
    }
}
