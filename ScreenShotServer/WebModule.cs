using Nancy;
using Nancy.Responses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ScreenShotServer
{
    public class WebModule : Nancy.NancyModule
    {
        public WebModule()
        {
            Get("/smon", GetPage);
            Get("/smon/list", GetList);
            Get("/smon/image/{machine}", GetImage);
            Get("/smon/thumbnail/{machine}", GetThumbnail);
            Get("/smon/live/{machine}", GetLivePage);
            Get("/smon/fast/{machine}", GetFastPage);
            Get("/smon/large/{machine}", GetLargePage);
        }

        public object GetPage(dynamic args)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<HTML><HEAD/><BODY>");
            sb.AppendLine("Hello world!");
            sb.AppendLine("</BODY></HTML>");
            var body = Encoding.UTF8.GetBytes(sb.ToString());
            return new HtmlResponse(HttpStatusCode.OK, a => a.Write(body, 0, body.Length));
        }

        public object GetList(dynamic args)
        {
            try
            {
                var sb = new StringBuilder();
                var ms = Machine.GetMachines();
                sb.AppendLine("<html><head><meta http-equiv=\"refresh\" content=\"5\"/></head><body><table>");
                foreach (var m in ms)
                {
                    sb.AppendLine($"<tr><td colspan=2><b>{m}</b></td></tr>");
                    sb.AppendLine($"<tr><td colspan=2><a href=\"/smon/large/{m}\"><img src=\"/smon/thumbnail/{m}\"/></a></td></tr>");
                    string lu = Machine.GetLastUpdate(m);
                    sb.AppendLine($"<tr><td align=\"center\"><a href=\"/smon/fast/{m}\"><b>FAST</b></a></td><td align=\"center\"><a href=\"/smon/live/{m}\"><b>LIVE</b></a></td></tr>");
                    sb.AppendLine($"<tr><td colspan=2>Last update: {lu}</td></tr>");
                    sb.AppendLine("<tr><td colspan=2><hr/></td></tr>");
                }
                sb.AppendLine("</table></body></html>");

                var body = Encoding.UTF8.GetBytes(sb.ToString());
                return new HtmlResponse(HttpStatusCode.OK, a => a.Write(body, 0, body.Length));
            }
            catch { }

            return 500;
        }

        public object GetImage(dynamic args)
        {
            bool refresh = false;
            try
            {
                if (Request.Query["cd"] != null)
                    refresh = true;
            }
            catch { }

            return GetLastImage(args, false, refresh);
        }

        public object GetThumbnail(dynamic args)
        {
            return GetLastImage(args, true, false);
        }

        public object GetLivePage(dynamic args)
        {
            return GetScreenPage(args, Machine.CaptureSpeed.Live);
        }

        public object GetFastPage(dynamic args)
        {
            return GetScreenPage(args, Machine.CaptureSpeed.Fast);
        }

        public object GetLargePage(dynamic args)
        {
            return GetScreenPage(args, Machine.CaptureSpeed.Regular);
        }

        public object GetScreenPage(dynamic args, Machine.CaptureSpeed speed)
        {
            try
            {
                string m = args.machine;
                Machine.RequestSpeed(m, speed);

                var sb = new StringBuilder();
                sb.AppendLine("<html><head><script type=\"text/javascript\">");
                sb.AppendFormat(@"var img=new Image();
var r=1;
function ol(){{ document.images[0].src=img.src; img=new Image(); img.onload=ol; img.src='/smon/image/{0}?cd='+r; r=r+1; }}
img.onload=ol;
img.src='/smon/image/{0}';
", m);
                sb.AppendLine();
                sb.AppendLine("</script></head><body>");
                sb.AppendLine($"<a href=\"/smon/image/{m}\"><img src=\"/smon/image/{m}\"/></a>");
                sb.AppendLine("</body></html>");

                var body = Encoding.UTF8.GetBytes(sb.ToString());
                return new HtmlResponse(HttpStatusCode.OK, a => a.Write(body, 0, body.Length));
            }
            catch { }

            return 500;
        }

        private object GetLastImage(dynamic args, bool thumbnail, bool refresh)
        {
            try
            {
                string m = args.machine;
                if (refresh)
                    Machine.RefreshSpeed(m);

                var img = Machine.GetLastPicture(m, thumbnail);
                var ms = new MemoryStream(img, false);
                return new StreamResponse(() => ms, "image/png");
            }
            catch { }

            return 500;
        }
    }
}
