using Castle.DynamicProxy;

namespace Lycoris.Autofac.Extensions.Options
{
    /// <summary>
    /// 注册相关配置
    /// </summary>
    public class AutofacSingleBuilder
    {
        /// <summary>
        /// 是否启用属性注入(默认：false)
        /// </summary>
        public bool PropertiesAutowired { get; set; } = false;

        /// <summary>
        /// 开启AOP拦截支持(默认：false)
        /// </summary>
        public bool EnableInterceptor { get; set; } = false;

        /// <summary>
        /// 拦截器列表
        /// </summary>
        internal List<InterceptorOption> Interceptors = new();

        /// <summary>
        /// 多实现类别名
        /// </summary>
        public string? Named { get; set; }

        /// <summary>
        /// 使用Aop拦截器
        /// </summary>
        /// <typeparam name="TInterceptor"></typeparam>
        /// <param name="order"></param>
        /// <returns></returns>
        public AutofacSingleBuilder InterceptedBy<TInterceptor>(int? order = null) where TInterceptor : IInterceptor
        {
            Interceptors.Add(new InterceptorOption()
            {
                Type = typeof(TInterceptor),
                Order = order ?? Interceptors.Count
            }); ;

            return this;
        }
    }
}
