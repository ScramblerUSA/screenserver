using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ScreenShotServer
{
    public class Machine
    {
        private static Dictionary<string, Machine> Machines = new Dictionary<string, Machine>(StringComparer.OrdinalIgnoreCase);
        static Machine()
        {
            ThreadPool.QueueUserWorkItem(TPItem);
        }

        public enum CaptureSpeed
        {
            Regular,
            Fast,
            Live
        }



        public string Name;
        public DateTime LastUpdate = DateTime.MinValue;
        public CaptureSpeed UpdateSpeed = CaptureSpeed.Regular;
        public DateTime LastLiveRequest = DateTime.MinValue;
        public byte[] LastPicture = null;
        public byte[] LastThumbnail = null;


        public static List<string> GetMachines()
        {
            lock (Machines)
            {
                return Machines.Keys.ToList();
            }
        }

        public static string GetLastUpdate(string machine)
        {
            string r = "N/A";
            if (Machines.ContainsKey(machine))
            {
                var lu = Machines[machine].LastUpdate;
                var delay = DateTime.Now - lu;
                r = string.Format("{0} ({1:00}:{2:00} ago)", lu.ToString("HH:mm:ss on MM/dd/yyyy"), delay.TotalMinutes, delay.Seconds);
            }
            return r;
        }

        public static void RestoreMachines()
        {
            lock (Machines)
            {
                // read from FS
            }
        }

        public static CaptureSpeed UpdateScreen(string machine, byte[] picture)
        {
            try
            {
                if (!Machines.ContainsKey(machine))
                {
                    lock (Machines)
                    {
                        Machines[machine] = new Machine() { Name = machine };
                    }
                }

                var m = Machines[machine];

                MemoryStream ms = new MemoryStream(picture, false);
                using (Bitmap bmp = new Bitmap(ms))
                {
                    int w = bmp.Width;
                    int h = bmp.Height;
                    int max = Math.Max(w, h);
                    int tw = (w * 300) / max;
                    int th = (h * 300) / max;
                    using (var thm = bmp.GetThumbnailImage(tw, th, null, IntPtr.Zero))
                    {
                        using (ms = new MemoryStream())
                        {
                            thm.Save(ms, ImageFormat.Png);
                            m.LastPicture = picture;
                            m.LastThumbnail = ms.ToArray();
                        }
                    }
                }


                if (m.LastUpdate.AddSeconds(9) < DateTime.Now)
                {
                    // write a file
                }
                m.LastUpdate = DateTime.Now;

                return m.UpdateSpeed;
            }
            catch { }

            return CaptureSpeed.Regular;
        }

        public static void RequestLive(string machine)
        {
            RequestSpeed(machine, CaptureSpeed.Live);
        }

        public static void RequestFast(string machine)
        {
            RequestSpeed(machine, CaptureSpeed.Fast);
        }

        public static void RequestSpeed(string machine, CaptureSpeed speed)
        {
            if (!Machines.ContainsKey(machine))
                return;

            var m = Machines[machine];

            if (speed > m.UpdateSpeed)
                m.UpdateSpeed = speed;

            if (speed == CaptureSpeed.Regular)
                return;

            m.LastLiveRequest = DateTime.Now;
        }

        public static void RefreshSpeed(string machine)
        {
            if (!Machines.ContainsKey(machine))
                return;

            var m = Machines[machine];
            m.LastLiveRequest = DateTime.Now;
        }

        public static byte[] GetLastPicture(string machine, bool thumbnail)
        {
            if (Machines.ContainsKey(machine))
            {
                var m = Machines[machine];
                return thumbnail ? m.LastThumbnail : m.LastPicture;
            }

            return null;
        }

        public static void HandleTimer()
        {
            DateTime deadline = DateTime.Now.AddSeconds(-2);
            lock (Machines)
            {
                foreach (var m in Machines.Values)
                {
                    if (m.LastLiveRequest < deadline)
                        m.UpdateSpeed = CaptureSpeed.Regular;
                }
            }
        }

        public static void TPItem(object state)
        {
            HandleTimer();
            Thread.Sleep(2000);
            ThreadPool.QueueUserWorkItem(TPItem);
        }
    }
}
