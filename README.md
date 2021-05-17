Demonstration of problematic usages of async void: 
```
        void OnCreate()
        {
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

Suppose that calls to a made up method e.g. `async void Service.Track()`, a method which also calls more async void methods: exceptions could be raised and will be handled in unpredictable ways as we'll see here. Another problem, the call stack information on the Exception is often incomplete or unhelpful. Because `async void` methods throw exceptions directly on the Sync Context instead of throwing on the calling thread, we usually wrap all the code in `async void` in try/catch blocks to suppress all exceptions, a scattergun approach that comes with several its own drawbacks.

One major use case of `async void` is fire and forget async methods. At first glance they might seem ideal, except you know that if your code could throw exceptions you must suppress them there or crash, as you cannot throw them to the caller. There's a better way: I recommend checking out this blog post regarding this: https://www.meziantou.net/fire-and-forget-a-task-in-dotnet.htm

It gets worse when you call more async void methods: unlike other method types, `async void` methods must suppress exceptions in the code of all `async` methods which don't return `Task`. Logically, it follows that you need to be certain every `async void` method you call is suppressing exceptions as well. If you work on a large codebase, it is not usually realistic to be confident of the handling of every method and library you call, so it's better to use fail-safe exception handling with `async Task`.

Most of the time the best solution is clear:
1. which is to avoid `async void` when it is not strictly necessary (e.g. event handlers)
2.  for lambdas, avoid using `Action` when instead `Func<Task>` can be used - async `Action`s are `async void`, so bear that in mind! 
3.  For method signatures, always use `async Task` over `async void` when it is possible. Exceptions will be stored on the returned Task which are then thrown when awaited, so the calling thread handles the exception when it is consuming the returned result. 
4.  When you need to fire and forget a Task, use the `Task.Forget()` extension from the above paragraph's blog post link. 
5. In the rare cases where you **must** use async void, for example in library `void func()` overrides or in event handlers, be extra careful to suppress and handle exceptions. 

The conclusion I take from this is: `async void` methods are dangerous in that if an unhandled exception occurs in one, it will often be thrown on the Sync Context with unhelpful call stack information, resulting in more app crashes which are difficult to reproduce or resolve. These issues are avoidable: use `async Task` in every possible use case and understand the behaviour of `async void` to prevent further issues in your app.

--

*You can test the behaviour yourself on Xamarin.Android by pulling this solution and opening the .sln file in your .NET IDE.* 

*the relevant code is at `src/AsyncVoidTroubleshooting.Droid/Views/Main/MainContainerActivity.cs`, uncomment each method call one at a time to see the difference in exception handling between the approaches*

For more information read this blog post https://docs.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming

