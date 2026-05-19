using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Lycoris.Autofac.Extensions.TaskExecutor.Impl
{
    /// <summary>
    /// 异步任务执行器
    /// </summary>
    public class AsyncTaskExecutor : IAsyncTaskExecutor
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        /// <summary>
        ///
        /// </summary>
        /// <param name="serviceScopeFactory"></param>
        public AsyncTaskExecutor(IServiceScopeFactory serviceScopeFactory) => _serviceScopeFactory = serviceScopeFactory;

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
        /// 解析指定类型的任务处理器。
        /// 默认从 <see cref="AutofacRegisterAttribute.MultipleNamed"/> 获取名称，
        /// 通过 <see cref="IAutofacMultipleService.TryGetService{T}"/> 解析具名实例。
        /// 子类可重写以实现自定义解析逻辑（如基于 Keyed 服务解析）。
        /// </summary>
        protected virtual IAsyncTaskExecutorHandler ResolveHandler(IAutofacMultipleService multipleService, Type handlerType)
        {
            var named = handlerType.GetCustomAttribute<AutofacRegisterAttribute>(false)?.MultipleNamed;

            if (string.IsNullOrWhiteSpace(named))
                throw new InvalidOperationException($"Type '{handlerType.Name}' does not define a valid MultipleNamed value in AutofacRegisterAttribute. Override {nameof(ResolveHandler)} to provide custom resolution logic.");

            var service = multipleService.TryGetService<IAsyncTaskExecutorHandler>(named);

            if (service == null)
                throw new InvalidOperationException($"No matching IAsyncTaskExecutorHandler implementation found for '{handlerType.Name}' with named '{named}'.");

            return service;
        }

        /// <summary>
        /// 核心执行逻辑（内部复用）
        /// </summary>
        private async Task ExecuteInternalAsync<T>(object? arg, int delaySeconds) where T : AsyncTaskExecutorHandler
        {
            if (delaySeconds > 0)
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds)).ConfigureAwait(false);

            await using var scope = _serviceScopeFactory.CreateAsyncScope();

            var multipleService = scope.ServiceProvider.GetRequiredService<IAutofacMultipleService>();

            var service = ResolveHandler(multipleService, typeof(T));

            if (arg is null)
                await service.ExecuteAsync().ConfigureAwait(false);
            else
                await service.ExecuteAsync(arg).ConfigureAwait(false);
        }
    }
}
