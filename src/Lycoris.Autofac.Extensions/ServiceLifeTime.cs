namespace Lycoris.Autofac.Extensions
{
    /// <summary>
    /// 服务注册的生命周期
    /// </summary>
    public enum ServiceLifeTime
    {
        /// <summary>
        /// 瞬态模式,每次调用,都会重新实例化对象
        /// </summary>
        Transient = 0,
        /// <summary>
        /// 同一个作用域生成的对象是同一个实例（webapi、mvc 就是一个请求,请求会穿透的各层享用同一个实例）
        /// </summary>
        Scoped = 1,
        /// <summary>
        /// 单例模式,每次调用,都会使用同一个实例化的对象
        /// </summary>
        Singleton = 2
    }
}
