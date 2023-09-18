namespace AutofacModule
{
    //[AutofacRegister(ServiceLifeTime.Scoped)]
    public class TestGencService<T> : ITestGencService<T>
    {
        public T? Test(T input)
        {
            Console.WriteLine(input);
            return input;
        }
    }
}
