using Lycoris.Autofac.Extensions;

namespace AutofacModule
{
    [AutofacRegister(ServiceLifeTime.Scoped, Interface = typeof(ITestAService))]
    public class TestService : ITestBService, ITestAService
    {
        public void Test() => Console.WriteLine("ITestAService => Test");
    }
}
