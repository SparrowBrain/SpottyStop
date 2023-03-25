using System.Diagnostics;

namespace SpottyStop.Services
{
    internal class Computer : IComputer
    {
        public void Shutdown()
        {
            Process.Start("shutdown", "/s /t 10");
        }
    }
}