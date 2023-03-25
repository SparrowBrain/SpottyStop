using System;
using Stylet;
using StyletIoC;
using SpottyStop.Pages;
using SpottyStop.Services;
using SpotifyAPI.Web;

namespace SpottyStop
{
    public class Bootstrapper : Bootstrapper<ShellViewModel>
    {
        protected override void ConfigureIoC(IStyletIoCBuilder builder)
        {
            builder.Bind<ISpotify>().To<Spotify>().InSingletonScope();
        }

        protected override void Configure()
        {
            // Perform any other configuration before the application starts
        }
    }
}
