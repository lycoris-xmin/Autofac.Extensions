using Lycoris.Autofac.Extensions;
using Lycoris.Autofac.Extensions.Impl;
using Microsoft.Extensions.DependencyInjection;

namespace AutofacModule
{
    public class ApplicationModule : AutofacRegisterModule
    {
        public override void ModuleRegister(LycorisModuleBuilder builder)
        {

            builder.RegisterScoped(typeof(ITestGencService<>), typeof(TestGencService<>), "123");
            builder.RegisterScoped(typeof(ITestGencService<>), typeof(TestsGencService<>), "456");
        }

        public override void SerivceRegister(IServiceCollection services)
        {
            services.AddScoped<ISerService, SerService>();
        }
    }
}