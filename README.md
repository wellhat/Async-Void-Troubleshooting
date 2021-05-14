Test async void troubleshooting by looking at `AsyncVoidTroubleshooting/Async-Void-Troubleshooting/src/AsyncVoidTroubleshooting.Droid/Views/Main/MainContainerActivity.cs` and uncommenting each method call one at a time. 

For more information read this blog post https://docs.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming

Demonstration of problematic usages of async void: 
```
        void OnCreate()
        {
                // Note: problems are occurring because the async/void method is not awaited, so its context is forgotten
                // Exceptions would normally be added to the returning Task, but async void doesn't return a task, so it throws exceptions on sync context
                
                try
                {
                        ThrowExceptionAsync_VoidNoAwait();
                        ThrowExceptionAsync_VoidWithAwait();
                }
                catch (Exception)
                {
                        // Exceptions **will not** be caught here. They are thrown onto the Sync Context. In Xamarin this usually results in unhandled exceptions.
                }
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

This means that calls to e.g. `void Service.Track()`, an async void method which may also call other async void methods, does not handle exceptions in the intuitive and predictable way. Issues which arise from this are easy to miss due to not containing correct call stack information (as async loses access to the upper call stack), and because by wrapping all async/void code in a try/catch we can suppress exceptions altogether, but doing this increases nesting and decreases readability.

Unfortunately, this somewhat invalidates 1 common use case of async/void, fire and forget async methods. Fortunately, there exists better ways for firing and forgetting using Task.Run(), I recommend checking out this blog post regarding this: https://www.meziantou.net/fire-and-forget-a-task-in-dotnet.htm

Furthermore, try/catch cannot protect you from calling *another async void method* in which exceptions may not be suppressed. Try/catch/suppress will only save you if you are certain that every async method called is suppressing exceptions in the same way. If you use external libraries or work on a large team, it is not usually realistic to provide such guarantees, it's better to use fail-safe exception handling with `async Task`.

Most of the time, the best solution is clear:
1. which is to avoid `async void` when it is not strictly necessary (e.g. event handlers)
2.  for lambdas, avoid using `Action` when instead `Func<Task>` can be used - async `Action`s are `async void`, so bear that in mind! 
3.  For method signatures, always use `async Task` over `async void` when it is possible. Exceptions will be stored on the returned Task which are then thrown when awaited, so the calling thread handles the exception when it is consuming the returned result. 
4.  When you need to fire and forget a Task, use the `Task.Forget()` extension from the above paragraph's blog post link. 

In the rare cases where you **must** use async void, for example in library `void foo()` overrides or in event handlers, it pays to use extreme precaution with exception handling: wrap all code in try/catch and handle all exceptions explicitly inside any async/void code. The #1 conclusion I take from this is: if you want the caller to handle the exception from an `async` function, you need to return a `Task` to it which contains the exception. 
