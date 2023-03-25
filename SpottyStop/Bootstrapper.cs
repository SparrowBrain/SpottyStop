using SpottyStop.Pages;
using SpottyStop.Services;
using Stylet;
using StyletIoC;

namespace SpottyStop
{
    public class Bootstrapper : Bootstrapper<ShellViewModel>
    {
        protected override void ConfigureIoC(IStyletIoCBuilder builder)
        {
            builder.Bind<ISpotify>().To<Spotify>().InSingletonScope();
            builder.Bind<IComputer>().To<Computer>();
            builder.Bind<IActionDelayRetriever>().To<ActionDelayRetriever>();
            builder.Bind<IDelayedActionFactory>().To<DelayedActionFactory>();
            builder.Bind<IMainAppService>().To<MainAppService>();
            builder.Bind<IGenericDelayedActionRunner>().To<GenericDelayedActionRunner>();
        }

        protected override void Configure()
        {
            // Perform any other configuration before the application starts
        }
    }
}