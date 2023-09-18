using Autofac;

namespace Lycoris.Autofac.Extensions.Options
{
    /// <summary>
    /// Autofac默认空模块
    /// </summary>
    internal class NullModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Method intentionally left empty.
        }
    }
}
