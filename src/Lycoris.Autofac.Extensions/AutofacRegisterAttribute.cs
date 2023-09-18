using Castle.DynamicProxy;
using Castle.DynamicProxy.Internal;

namespace Lycoris.Autofac.Extensions
{
    /// <summary>
    /// 注入特性(用来处理自动注入)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class AutofacRegisterAttribute : Attribute
    {
        /// <summary>
        /// 注册的生命周期
        /// </summary>
        public ServiceLifeTime ServiceLifeTime { get; set; }

        /// <summary>
        /// 是否启用属性注入(默认：false)
        /// </summary>
        public bool PropertiesAutowired { get; set; } = false;

        /// <summary>
        /// 开启AOP拦截支持(默认：false)
        /// </summary>
        public bool EnableInterceptor { get; set; } = false;

        /// <summary>
        /// 是否仅注册自身(默认：false)
        /// </summary>
        public bool Self { get; set; } = false;

        /// <summary>
        /// 
        /// </summary>
        private Type? _Interface = null;

        /// <summary>
        /// 指定注入的接口名称
        /// </summary>
        public Type? Interface
        {
            get => _Interface;
            set
            {
                if (value == null || !value.IsInterface)
                    throw new ArgumentException("type must be an interface", nameof(value));

                _Interface = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private Type? _Interceptor = null;

        /// <summary>
        /// AOP拦截器
        /// </summary>
        public Type? Interceptor
        {
            get => _Interceptor;
            set
            {
                if (!value.GetAllInterfaces().Any(x => x == typeof(IInterceptor)))
                    throw new ArgumentException("the specified AOP interceptor is not assignable from 'IInterceptor' and cannot be used", nameof(_Interceptor));

                _Interceptor = value;
                EnableInterceptor = true;
            }
        }

        /// <summary>
        /// AOP拦截器执行优先级 数值越小优先级越高,默认优先级最高
        /// </summary>
        public int? InterceptorOrder { get; set; } = null;

        /// <summary>
        /// 是否是AOP拦截器(默认：false)
        /// </summary>
        public bool IsInterceptor { get; set; } = false;

        /// <summary>
        /// 接口多实例实现命名
        /// </summary>
        public string? MultipleNamed { get; set; } = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ServiceLifeTime"></param>
        public AutofacRegisterAttribute(ServiceLifeTime ServiceLifeTime)
        {
            this.ServiceLifeTime = ServiceLifeTime;
        }
    }
}
