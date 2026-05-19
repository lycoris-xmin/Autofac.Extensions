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
        /// AOP 拦截类型，接口拦截(默认)或类拦截
        /// </summary>
        public InterceptionType InterceptionType { get; set; } = InterceptionType.Interface;

        /// <summary>
        /// 排除指定的拦截器类型
        /// </summary>
        public Type? ExcludeInterceptor { get; set; } = null;

        /// <summary>
        /// 拦截器列表
        /// </summary>
        internal List<InterceptorOption> Interceptors = new();

        /// <summary>
        /// 多实现类别名
        /// </summary>
        public string? Named { get; set; }

        /// <summary>
        /// 多实现类键 (Autofac 8.x Keyed 服务, 任意对象类型)
        /// 与 Named 互斥，只能设置其中一个
        /// </summary>
        public object? Key { get; set; }

        /// <summary>
        /// 使用Aop拦截器
        /// </summary>
        /// <typeparam name="TInterceptor"></typeparam>
        /// <param name="order"></param>
        /// <returns></returns>
        public AutofacSingleBuilder InterceptedBy<TInterceptor>(int? order = null) where TInterceptor : IInterceptor
        {
            Interceptors.Add(new InterceptorOption(typeof(TInterceptor), order ?? Interceptors.Count));

            return this;
        }
    }
}
