using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AsyncVoidTroubleshooting.Core.ViewModels.Main;
using Java.Lang;
using Exception = System.Exception;

namespace AsyncVoidTroubleshooting.Droid.Views.Main
{
    [Activity(
        Theme = "@style/AppTheme",
        WindowSoftInputMode = SoftInput.AdjustResize | SoftInput.StateHidden)]
    public class MainContainerActivity : BaseActivity<MainContainerViewModel>
    {
        protected override int ActivityLayoutId => Resource.Layout.activity_main_container;

        private void LogMessage(string msg) => System.Diagnostics.Debug.WriteLine(msg);

        protected override async void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            try
            {
                // The following two implementations both run regardless of order
                // Both will throw exceptions, but not in this try/catch, but in the SyncContext!
                // Unlike when using await as below, exceptions in async/void risk crashing the app.
                // If the methods below were suppressing top level exceptions instead of re-throwing, but they called *another* async void method (see AnalitixService), then that too could risk crashing the app!

                // ThrowExceptionAsync_VoidNoAwait();
                // ThrowExceptionAsync_VoidWithAwait();

                // For those reasons I discussed above, using async/void is unadvisable except when unavoidable and for event handlers.
                // In these cases, extra care needs to be taken to make sure that exceptions are suppressed in ALL async/void calls in the stack, or else it may again just be thrown on the Sync Context.



                // What if you called it without awaiting the resulting Task? Ignoring the obvious issue that OnCreate() will consider executing which may cause race conditions, the calling thread is also unaware of the async method's status.
                // Exceptions are thrown into the Task which is discarded.
                // This is better than async/void but may still result in unpredictable exception handling, especially if this pattern is being overused.

                // ThrowExceptionAsync_TaskWithAwait();

                // Using await in conjunction with Task irons out these issues:
                // Exceptions are returned to the caller with the following 2 options:

                // await ThrowExceptionAsync_TaskWithAwait();
                // await ThrowExceptionAsync_TaskWithoutAwait();

                // You should avoid CPU bound (or otherwise thread-blocking) code at the start of async methods. Until the first `await` is reached, the calling thread will not have access to the task. eg:
                async Task<bool> BadAsyncFunction()
                {
                    Thread.Sleep(2500);

                    // If an exception is thrown before reaching 'await', the method may throw exceptions on the Sync Context and cause crashes as with async/void.
                    // You should be trying to do Syncronous work in a separate execution context than your Asyncronous work. This will avoid all such issues.
                    return await Task.FromResult(true).ConfigureAwait(false);
                }

                // Instead of the above, consider this pattern which supports fire/forget + async
                void GoodAsync_SyncWork()
                {
                    Thread.Sleep(2500);

                    throw new Exception("GoodAsync_SyncWork");
                }

                // Try to keep async/void contexts as short lived as possible, ideally they should be only a function which suppresses exceptions and which calls awaits a different function that uses async Task instead.
                // Use `result = await Task.Run(...)` to perform heavy syncronous work on a bg thread whenever possible and await the result. Task.Run(...) can also be helpful for fire and forget behaviours.
                async Task<bool> GoodAsync_AsyncWork()
                {
                    await Task.Delay(20).ConfigureAwait(false);

                    throw new Exception("GoodAsync_AsyncWork");

                    return await Task.FromResult(true).ConfigureAwait(false);
                }

                await Task.Run(GoodAsync_SyncWork);
                bool result = await GoodAsync_AsyncWork();

            }
            catch (Exception ex)
            {
                LogMessage("Suppressed exception successfully on caller");
                LogMessage("======================");
                LogException(ex);
            }
        }


        // PROBLEMATIC ASYNC VOID USE
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
        // END PROBLEMATIC USAGES


        // Other usages

        private async Task ThrowExceptionAsync_TaskWithAwait()
        {
            try
            {
                await Task.CompletedTask;

                throw new Exception("ThrowExceptionAsync_TaskWithAwait");
            }
            catch (Exception ex)
            {
                LogException(ex);

                throw;
            }
        }

        private async Task ThrowExceptionAsync_TaskWithoutAwait()
        {
            try
            {
                throw new Exception("ThrowExceptionAsync_TaskWithAwait");
            }
            catch (Exception ex)
            {
                LogException(ex);

                throw;
            }
        }

        private void LogException(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
            System.Diagnostics.Debug.WriteLine(ex.StackTrace);
        }
    }
}
