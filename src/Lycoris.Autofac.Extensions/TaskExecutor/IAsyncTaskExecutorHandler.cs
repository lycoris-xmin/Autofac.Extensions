namespace Lycoris.Autofac.Extensions.TaskExecutor
{
    /// <summary>
    /// 
    /// </summary>
    public interface IAsyncTaskExecutorHandler
    {
        /// <summary>
        /// 
        /// </summary>
        Task ExecuteAsync();

        /// <summary>
        /// 
        /// </summary>
        Task ExecuteAsync(object? args);
    }
}
