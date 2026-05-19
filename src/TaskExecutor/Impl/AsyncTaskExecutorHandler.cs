namespace Lycoris.Autofac.Extensions.TaskExecutor.Impl
{
    /// <summary>
    /// 
    /// </summary>
    public class AsyncTaskExecutorHandler : IAsyncTaskExecutorHandler
    {
        /// <summary>
        /// 
        /// </summary>
        public virtual Task ExecuteAsync() => Task.CompletedTask;

        /// <summary>
        /// 
        /// </summary>
        public virtual Task ExecuteAsync(object? args) => Task.CompletedTask;
    }
}
