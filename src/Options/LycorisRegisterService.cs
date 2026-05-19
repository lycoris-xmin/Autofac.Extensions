namespace Lycoris.Autofac.Extensions.Options
{
    /// <summary>
    /// 
    /// </summary>
    internal class LycorisRegisterService
    {
        /// <summary>
        /// 注册相关配置
        /// </summary>
        public AutofacRegisterAttribute? Option { get; set; }

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        /// <summary>
        /// 实现类
        /// </summary>
        public Type Type { get; set; }
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。

        /// <summary>
        /// 接口
        /// </summary>
        public Type? AsType { get; set; }

        /// <summary>
        /// 拦截器列表
        /// </summary>
        public List<InterceptorOption>? Interceptors = null;
    }
}
