using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ScreenShotServer
{
    public class AgentModule : Nancy.NancyModule
    {
        public AgentModule()
        {
            Post("/smon/screen/{machine}", UpdateScreen);
        }

        public object UpdateScreen(dynamic args)
        {
            try
            {
                MemoryStream ms = new MemoryStream();
                Context.Request.Body.CopyTo(ms);

                string machine = args.machine;
                var speed = Machine.UpdateScreen(machine, ms.ToArray());

                if (speed == Machine.CaptureSpeed.Regular)
                    return 202;
                if (speed == Machine.CaptureSpeed.Fast)
                    return 204;

                return "0";
            }
            catch { }

            return 500;
        }
    }
}
