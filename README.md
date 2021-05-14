Test async void troubleshooting by looking at `AsyncVoidTroubleshooting/Async-Void-Troubleshooting/src/AsyncVoidTroubleshooting.Droid/Views/Main/MainContainerActivity.cs` and uncommenting each method call one at a time. 

For more information read this blog post https://docs.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming

Demonstration of problematic usages of async void: 
```
        void Main()
        {
                // Note: problems are occurring because the async/void method is not awaited, so its context is forgotten
                // Exceptions would normally be added to the returning Task, but async void doesn't return a task, so it throws exceptions on sync context
                ThrowExceptionAsync_VoidNoAwait();
                ThrowExceptionAsync_VoidWithAwait();
        }

        private async void ThrowExceptionAsync_VoidWithAwait()
        {
            try
            {
                await Task.CompletedTask;

                throw new Exception("ThrowExceptionAsync_VoidWithAwait");
            }
            catch (Exception ex)
            {
                LogException(ex);

                throw;
            }
        }

        private async void ThrowExceptionAsync_VoidNoAwait()
        {
            try
            {
                throw new Exception("ThrowExceptionAsync_VoidNoAwait");
            }
            catch (Exception ex)
            {
                LogException(ex);

                throw;
            }
        }
```

This means that calls to e.g. `void Service.Track()`, an async void method which may also call other async void methods, does not always handle exceptions in the intuitive and predictable way. Issues which arise from this are easy to miss due to not containing correct call stack information (as async void loses access to the upper call stack), and because by wrapping all async/void code in a try/catch we can suppress exceptions altogether, but doing this increases nesting and decreases readability, and does not actually guarantee that exceptions are handled as desired. Also, try/catch does not protect you from calling *another* async void method in which exceptions may not be properly handled - meaning that each async/void method must be treated as standalone when it comes to handling exceptions - not ideal.

Another solution is to always await async/void methods when calling them - it is specifically when an async/void method is being let to run *without* awaiting that there is problematic exception handling occurring. Unfortunately, this invalidates the common use case of async/void for fire and forget methods. Fortunately, there exist better workaround for firing and forgetting Task.Run(), I recommend this blog post: https://www.meziantou.net/fire-and-forget-a-task-in-dotnet.htm

The best solution is clear, which is to avoid `async void` when it is not strictly necessary (e.g. event handlers), for lambdas avoid using `Action` when instead `Func<Task>` can be used to pass the Task to the caller, and for method signatures always use `async Task` over `async void`. Exceptions are then instead stored on the Task, allowing the calling thread to ignore or manage exceptions on the Task, as intended. Exceptions will never be thrown on the Sync Context so long as `async Task` is used instead of `async void`.
