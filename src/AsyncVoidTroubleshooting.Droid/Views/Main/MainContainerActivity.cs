using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
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

                // What if you called it without awaiting the resulting Task?
                // OnCreate() will continue to run and could cause a race condition, the calling thread is also unaware of the async method's status which was in the Task.
                // Exceptions are thrown on the Task when awaited, so you must not discard it if you want to handle exceptions with async Task..
                // This is better than async/void, but it does not track exceptions in Task:

                // ThrowExceptionAsync_TaskWithAwait();

                // Using `await Task` is ideal for handling code which could throw exceptions
                // Exceptions are returned to the caller with the following 2 options:

                // await ThrowExceptionAsync_TaskWithAwait();
                // await ThrowExceptionAsync_TaskWithoutAwait();

                FireAndForgetJob();
                FireAndForgetJob();

                // For firing and forgetting, you can use the below Task extensions for fire & forget.
                void FireAndForgetJob()
                {
                    Task.Run(() =>
                    {
                            // do work here
                            LogMessage("fire&forget");
                    }).Forget();
                }

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
        // ^^^ PROBLEMATIC USAGES


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

public static class TaskExtensions
{
    public static void Forget(this Task task)
    {
        // note: this code is inspired by a tweet from Ben Adams: https://twitter.com/ben_a_adams/status/1045060828700037125
        // Only care about tasks that may fault (not completed) or are faulted,
        // so fast-path for SuccessfullyCompleted and Canceled tasks.
        if (!task.IsCompleted || task.IsFaulted)
        {
            // use "_" (Discard operation) to remove the warning IDE0058: Because this call is not awaited, execution of the current method continues before the call is completed
            // https://docs.microsoft.com/en-us/dotnet/csharp/discards#a-standalone-discard
            _ = ForgetAwaited(task);
        }

        // Allocate the async/await state machine only when needed for performance reason.
        // More info about the state machine: https://blogs.msdn.microsoft.com/seteplia/2017/11/30/dissecting-the-async-methods-in-c/?WT.mc_id=DT-MVP-5003978
        async static Task ForgetAwaited(Task task)
        {
            try
            {
                // No need to resume on the original SynchronizationContext, so use ConfigureAwait(false)
                await task.ConfigureAwait(false);
            }
            catch
            {
                // Nothing to do here
            }
        }
    }
}
