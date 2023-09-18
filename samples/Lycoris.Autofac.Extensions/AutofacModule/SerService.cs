namespace AutofacModule
{
    public class SerService : ISerService
    {
        public void Test(string a)
        {
            Console.WriteLine($"SerService --> {a}");
        }
    }
}
