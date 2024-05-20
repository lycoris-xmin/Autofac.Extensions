namespace AutofacModule
{
    //[AutofacRegister(ServiceLifeTime.Scoped, MultipleNamed = "456")]
    public class TestsGencService<T> : ITestGencService<T>
    {
        private readonly ISerService serService;

        public TestsGencService(ISerService serService)
        {
            this.serService = serService;
        }

        public T? Test(T input)
        {
            serService.Test(input!.ToString()!);
            return input;
        }
    }
}
