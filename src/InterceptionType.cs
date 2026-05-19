namespace Lycoris.Autofac.Extensions
{
    /// <summary>
    /// AOP 拦截类型
    /// </summary>
    public enum InterceptionType
    {
        /// <summary>
        /// 接口拦截（默认），通过接口代理实现
        /// </summary>
        Interface = 0,

        /// <summary>
        /// 类拦截，通过虚方法代理实现，适用于未绑定接口的注册
        /// </summary>
        VirtualClass = 1
    }
}
