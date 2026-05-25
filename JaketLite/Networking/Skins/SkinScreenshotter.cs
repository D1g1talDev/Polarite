using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Polarite.Networking.Skins
{
    public static class SkinScreenshotter
    {
        public static string Path = Application.persistentDataPath + "/Skins/Screenshots/";
        public static void Init()
        {
            if(!Directory.Exists(Path))
            {
                Directory.CreateDirectory(Path);
            }
        }
        public static void Screenshot()
        {
            ScreenCapture.CaptureScreenshot(Path + SkinSaver.CurrentSkin.name + ".png");
        }
    }
}
