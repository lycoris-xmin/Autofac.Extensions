namespace AutofacModule
{
    public interface ITestGencService<T>
    {
        T? Test(T input);
    }
}
