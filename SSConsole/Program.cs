using Mono.Unix;
using Mono.Unix.Native;
using Nancy.Hosting.Self;
using System;

namespace SSConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var uri = "http://localhost:1234/";
            Console.WriteLine("Starting Nancy on {0}...", uri);

            using (var nancyHost = new NancyHost(new Uri(uri)))
            {
                nancyHost.Start();
                Console.WriteLine("Nancy now listening.");

                if (Type.GetType("Mono.Runtime") != null)
                {
                    // on mono, processes will usually run as daemons - this allows you to listen
                    // for termination signals (ctrl+c, shutdown, etc) and finalize correctly
                    UnixSignal.WaitAny(new[] {
                        new UnixSignal(Signum.SIGINT),
                        new UnixSignal(Signum.SIGTERM),
                        new UnixSignal(Signum.SIGQUIT),
                        new UnixSignal(Signum.SIGHUP)
                    });
                }
                else
                {
                    Console.ReadLine();
                }

                Console.WriteLine("Stopping Nancy");
                nancyHost.Stop();  // stop hosting
            }
        }
    }
}
