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

This means that calls to e.g. `void Service.Track()`, an async void method which may also call other async void methods, does not always handle exceptions in the intuitive and predictable way. Issues which arise from this are easy to miss due to not containing correct call stack information (as async void loses access to the upper call stack), and because by wrapping all async/void code in a try/catch we can suppress exceptions altogether, but doing this increases nesting and decreases readability, and does not actually guarantee that exceptions are handled as desired. 

Furthermore, try/catch does not protect you from calling *another* async void method in which exceptions may not be properly handled. Try/catch will only save you if you are certain that every async/void method in the call stack is suppressing exceptions or being awaited. If you use external libraries or work on a large team, it is not realistic to provide such guarantees.

Another solution is to always await async/void methods when calling them - it is specifically when an async/void method is being let to run *without* awaiting that there is problematic exception handling occurring. Unfortunately, this invalidates 1 common use case of async/void, fire and forget async methods. Fortunately, there exist better workaround for firing and forgetting with Task.Run(), I recommend checking out this blog post regarding this: https://www.meziantou.net/fire-and-forget-a-task-in-dotnet.htm

In all other cases, the best solution is clear, which is to avoid `async void` when it is not strictly necessary (e.g. event handlers). For lambdas, avoid using `Action` when instead `Func<Task>` can be used - async `Action`s are `async void`, so bear that in mind! For method signatures, always use `async Task` over `async void` when possible. Exceptions are stored on the returned Task and thrown when awaited. When you need to fire and forget a Task, use the `.Forget()` extension from the above paragraph's blog post link. In the rare cases where you **must** use async void, for example in `override void` methods or event handlers, it pays to use extreme precaution with exception handling: wrap code in try/catch and handle all exceptions explicitly inside any async/void code. Remember that if you want the calling thread/method to handle the exception, you have to use `async Task`.
