namespace Lycoris.Autofac.Extensions.Options
{
    /// <summary>
    /// 拦截器配置
    /// </summary>
    public class InterceptorOption
    {
        /// <summary>
        /// 无参构造函数（向后兼容）。
        /// 新代码建议使用 <see cref="InterceptorOption(Type, int)"/>。
        /// </summary>
        [Obsolete("Use InterceptorOption(Type, int) constructor instead.")]
        public InterceptorOption()
        {
            Type = null!;
        }

        /// <summary>
        /// 创建拦截器配置
        /// </summary>
        /// <param name="type">拦截器类型</param>
        /// <param name="order">拦截器执行优先级，数值越小优先级越高</param>
        /// <exception cref="ArgumentNullException">当 type 为 null 时抛出</exception>
        public InterceptorOption(Type type, int order = 0)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Order = order;
        }

        /// <summary>
        /// 拦截器类型
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// 拦截器执行优先级
        /// </summary>
        public int Order { get; set; }
    }
}
