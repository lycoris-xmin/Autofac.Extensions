using Lycoris.Autofac.Extensions.TaskExecutor.Impl;

namespace Lycoris.Autofac.Extensions.TaskExecutor
{
    /// <summary>
    /// 
    /// </summary>
    public interface IAsyncTaskExecutor
    {
        /// <summary>
        /// 
        /// </summary>
        void Execute<T>() where T : AsyncTaskExecutorHandler;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="seconds"></param>
        void DelayExecute<T>(int seconds) where T : AsyncTaskExecutorHandler;

        /// <summary>
        /// 
        /// </summary>
        void Execute<T>(object? args) where T : AsyncTaskExecutorHandler;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arg"></param>
        /// <param name="seconds"></param>
        void DelayExecute<T>(object? arg, int seconds) where T : AsyncTaskExecutorHandler;
    }
}
