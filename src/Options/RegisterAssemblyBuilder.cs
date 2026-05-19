namespace Lycoris.Autofac.Extensions.Options
{
    /// <summary>
    /// 
    /// </summary>
    public class RegisterAssemblyBuilder
    {
        /// <summary>
        /// 
        /// </summary>
        public ServiceLifeTime ServiceLifeTime { get; set; } = ServiceLifeTime.Transient;

        /// <summary>
        /// 
        /// </summary>
        public bool Self { get; set; } = false;

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
        /// 
        /// </summary>
        /// <returns></returns>
        internal Action<AutofacSingleBuilder> BuildAutofacSingleBuilder()
        {
            return x =>
            {
                x.PropertiesAutowired = this.PropertiesAutowired;
                x.EnableInterceptor = this.EnableInterceptor;
                x.InterceptionType = this.InterceptionType;
                x.ExcludeInterceptor = this.ExcludeInterceptor;
                x.Interceptors = this.Interceptors;
            };
        }
    }
}
