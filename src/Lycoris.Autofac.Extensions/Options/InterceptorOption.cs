namespace Lycoris.Autofac.Extensions.Options
{
    /// <summary>
    /// 拦截器配置
    /// </summary>
    public class InterceptorOption
    {
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        /// <summary>
        /// 拦截器类型
        /// </summary>
        public Type Type { get; set; }
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。

        /// <summary>
        /// 拦截器执行优先级
        /// </summary>
        public int Order { get; set; }
    }
}
