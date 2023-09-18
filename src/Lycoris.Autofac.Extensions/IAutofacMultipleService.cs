namespace Lycoris.Autofac.Extensions
{
    /// <summary>
    /// 接口多实现服务类
    /// </summary>
    public interface IAutofacMultipleService
    {
        /// <summary>
        /// 获取多实现接口服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">多实现服务别名</param>
        /// <returns></returns>
        T GetService<T>(string name) where T : notnull;

        /// <summary>
        /// 尝试获取服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">多实现服务别名</param>
        /// <returns></returns>
        T? TryGetService<T>(string name) where T : notnull;
    }
}
