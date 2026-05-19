using Autofac;

namespace Lycoris.Autofac.Extensions.Impl
{
    /// <summary>
    /// 接口多实现服务类
    /// </summary>
    public sealed class AutofacMultipleService : IAutofacMultipleService
    {
        private readonly IComponentContext _context;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="context"></param>
        public AutofacMultipleService(IComponentContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 获取服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">多实现服务别名</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public T GetService<T>(string name) where T : class
        {
            var service = _context.ResolveNamed<T>(name);

            return service == null ? throw new InvalidOperationException($"the interface implementation named:{name} could not be found") : service;
        }

        /// <summary>
        /// 尝试获取服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">多实现服务别名</param>
        /// <returns></returns>
        public T? TryGetService<T>(string name) where T : class
        {
            try
            {
                return _context.ResolveNamed<T>(name);
            }
            catch
            {
                return default;
            }
        }
    }
}
