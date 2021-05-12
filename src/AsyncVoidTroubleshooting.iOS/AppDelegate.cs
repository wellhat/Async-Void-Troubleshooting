using Foundation;
using MvvmCross.Platforms.Ios.Core;
using AsyncVoidTroubleshooting.Core;

namespace AsyncVoidTroubleshooting.iOS
{
    [Register(nameof(AppDelegate))]
    public class AppDelegate : MvxApplicationDelegate<Setup, App>
    {
    }
}
