using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Lycoris.Autofac.Extensions.TaskExecutor.Impl
{
    /// <summary>
    /// 异步任务执行器
    /// </summary>
    public sealed class AsyncTaskExecutor : IAsyncTaskExecutor
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceScopeFactory"></param>
        public AsyncTaskExecutor(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        #region ==== 公共方法 ====

        /// <summary>
        /// 立即执行指定任务处理器
        /// </summary>
        public void Execute<T>() where T : AsyncTaskExecutorHandler => Task.Run(() => ExecuteInternalAsync<T>(null, 0));

        /// <summary>
        /// 延迟执行指定任务处理器
        /// </summary>
        public void DelayExecute<T>(int seconds) where T : AsyncTaskExecutorHandler => Task.Run(() => ExecuteInternalAsync<T>(null, seconds));

        /// <summary>
        /// 立即执行带参数的任务处理器
        /// </summary>
        public void Execute<T>(object? arg) where T : AsyncTaskExecutorHandler => Task.Run(() => ExecuteInternalAsync<T>(arg, 0));

        /// <summary>
        /// 延迟执行带参数的任务处理器
        /// </summary>
        public void DelayExecute<T>(object? arg, int seconds) where T : AsyncTaskExecutorHandler => Task.Run(() => ExecuteInternalAsync<T>(arg, seconds));

        #endregion

        /// <summary>
        /// 核心执行逻辑（内部复用）
        /// </summary>
        private async Task ExecuteInternalAsync<T>(object? arg, int delaySeconds) where T : AsyncTaskExecutorHandler
        {
            // 延迟执行
            if (delaySeconds > 0)
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

            await using var scope = _serviceScopeFactory.CreateAsyncScope();

            var multiplie = scope.ServiceProvider.GetRequiredService<IAutofacMultipleService>();

            var handlerType = typeof(T);

            var named = handlerType.GetCustomAttribute<AutofacRegisterAttribute>(false)?.MultipleNamed;

            if (string.IsNullOrWhiteSpace(named))
                throw new ArgumentNullException(nameof(AutofacRegisterAttribute.MultipleNamed), $"Type '{handlerType.Name}' does not define a valid MultipleNamed value in AutofacRegisterAttribute.");

            var service = multiplie.TryGetService<IAsyncTaskExecutorHandler>(named);

            if (service == null)
                throw new ArgumentNullException(nameof(service), $"No matching IAsyncTaskExecutorHandler implementation found for '{handlerType.Name}'.");

            // 执行任务
            if (arg is null)
                await service.ExecuteAsync();
            else
                await service.ExecuteAsync(arg);
        }
    }
}
