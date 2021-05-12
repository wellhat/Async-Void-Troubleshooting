using MvvmCross.IoC;
using MvvmCross.ViewModels;
using AsyncVoidTroubleshooting.Core.ViewModels.Main;

namespace AsyncVoidTroubleshooting.Core
{
    public class App : MvxApplication
    {
        public override void Initialize()
        {
            CreatableTypes()
                .EndingWith("Service")
                .AsInterfaces()
                .RegisterAsLazySingleton();

            RegisterAppStart<MainViewModel>();
        }
    }
}
