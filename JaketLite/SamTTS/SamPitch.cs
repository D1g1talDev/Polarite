using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polarite.SamTTS
{
    public class Sam
    {
        public int speed;
        public int pitch;
        public int mouth;
        public int throat;

        public Sam()
        {
            speed = SamPitch.BASE_SPEED;
            pitch = SamPitch.BASE_PITCH;
            mouth = SamPitch.BASE_MOUTH;
            throat = SamPitch.BASE_THROAT;
        }
        public Sam(int _speed, int _pitch, int _mouth, int _throat)
        {
            speed = _speed;
            pitch = _pitch;
            mouth = _mouth;
            throat = _throat;
        }
    }
    public static class SamPitch
    {
        public const int BASE_SPEED = 72;
        public const int BASE_PITCH = 64;
        public const int BASE_MOUTH = 128;
        public const int BASE_THROAT = 128;

        public static Sam configSam = new Sam(ItePlugin.ttsSpeed.value, ItePlugin.ttsPitch.value, ItePlugin.ttsMouth.value, ItePlugin.ttsThroat.value);

        public static void Set(Sam sam)
        {
            UnitySAM.SetSpeed(sam.speed);
            UnitySAM.SetPitch(sam.pitch);
            UnitySAM.SetMouth(sam.mouth);
            UnitySAM.SetThroat(sam.throat);
        }
        public static void Set() => Set(configSam);
        public static void Reset() => Set(new Sam());
        public static void ReUpdateConfigSam() => configSam = new Sam(ItePlugin.ttsSpeed.value, ItePlugin.ttsPitch.value, ItePlugin.ttsMouth.value, ItePlugin.ttsThroat.value);
    }
}
