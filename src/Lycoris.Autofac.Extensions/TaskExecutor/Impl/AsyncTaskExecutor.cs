using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Lycoris.Autofac.Extensions.TaskExecutor.Impl
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class AsyncTaskExecutor : IAsyncTaskExecutor
    {
        /// <summary>
        /// 异步任务延迟秒数
        /// </summary>
        const int DELAY_SECOND = 1000;

        /// <summary>
        /// 
        /// </summary>
        private readonly IServiceScopeFactory _serviceScopeFactory;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceScopeFactory"></param>
        public AsyncTaskExecutor(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Execute<T>() where T : AsyncTaskExecutorHandler
        {
            Task.Run(async () =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var multiplie = scope.ServiceProvider.GetRequiredService<IAutofacMultipleService>();

                var type = typeof(T);

                var service = multiplie.TryGetService<IAsyncTaskExecutorHandler>(type.Name!);

                if (service == null)
                {
                    var named = typeof(T).GetCustomAttribute<AutofacRegisterAttribute>(false)?.MultipleNamed;
                    if (string.IsNullOrEmpty(named))
                        throw new ArgumentNullException(nameof(AutofacRegisterAttribute.MultipleNamed));

                    service = multiplie.TryGetService<IAsyncTaskExecutorHandler>(named!);
                }

                if (service == null)
                    throw new ArgumentNullException(nameof(service));

                await service.ExecuteAsync();
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="seconds"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void DelayExecute<T>(int seconds) where T : AsyncTaskExecutorHandler
        {
            seconds = seconds <= 0 ? 0 : seconds;

            Task.Run(async () =>
            {
                await Task.Delay(seconds);

                using var scope = _serviceScopeFactory.CreateScope();
                var multiplie = scope.ServiceProvider.GetRequiredService<IAutofacMultipleService>();

                var type = typeof(T);

                var service = multiplie.TryGetService<IAsyncTaskExecutorHandler>(type.Name!);

                if (service == null)
                {
                    var named = typeof(T).GetCustomAttribute<AutofacRegisterAttribute>(false)?.MultipleNamed;
                    if (string.IsNullOrEmpty(named))
                        throw new ArgumentNullException(nameof(AutofacRegisterAttribute.MultipleNamed));

                    service = multiplie.TryGetService<IAsyncTaskExecutorHandler>(named!);
                }

                if (service == null)
                    throw new ArgumentNullException(nameof(service));

                await service.ExecuteAsync();
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arg"></param>
        public void Execute<T>(object? arg) where T : AsyncTaskExecutorHandler
        {
            Task.Run(async () =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var multiplie = scope.ServiceProvider.GetRequiredService<IAutofacMultipleService>();

                var type = typeof(T);

                var service = multiplie.TryGetService<IAsyncTaskExecutorHandler>(type.Name!);

                if (service == null)
                {
                    var named = typeof(T).GetCustomAttribute<AutofacRegisterAttribute>(false)?.MultipleNamed;
                    if (string.IsNullOrEmpty(named))
                        throw new ArgumentNullException(nameof(AutofacRegisterAttribute.MultipleNamed));

                    service = multiplie.TryGetService<IAsyncTaskExecutorHandler>(named!);
                }

                if (service == null)
                    throw new ArgumentNullException(nameof(service));

                await service.ExecuteAsync(arg);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arg"></param>
        /// <param name="seconds"></param>
        public void DelayExecute<T>(object? arg, int seconds) where T : AsyncTaskExecutorHandler
        {
            seconds = seconds <= 0 ? 0 : seconds;

            Task.Run(async () =>
            {
                await Task.Delay(seconds);

                using var scope = _serviceScopeFactory.CreateScope();
                var multiplie = scope.ServiceProvider.GetRequiredService<IAutofacMultipleService>();

                var type = typeof(T);

                var service = multiplie.TryGetService<IAsyncTaskExecutorHandler>(type.Name!);

                if (service == null)
                {
                    var named = typeof(T).GetCustomAttribute<AutofacRegisterAttribute>(false)?.MultipleNamed;
                    if (string.IsNullOrEmpty(named))
                        throw new ArgumentNullException(nameof(AutofacRegisterAttribute.MultipleNamed));

                    service = multiplie.TryGetService<IAsyncTaskExecutorHandler>(named!);
                }

                if (service == null)
                    throw new ArgumentNullException(nameof(service));

                await service.ExecuteAsync(arg);
            });
        }
    }
}
