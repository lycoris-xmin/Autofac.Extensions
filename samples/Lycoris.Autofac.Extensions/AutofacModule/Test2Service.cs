using Lycoris.Autofac.Extensions;

namespace AutofacModule
{
    [AutofacRegister(ServiceLifeTime.Scoped)]
    public class Test2Service : ITestService, ITestBService
    {
        public void Test() => Console.WriteLine("ITestService => Test2");
    }
}
