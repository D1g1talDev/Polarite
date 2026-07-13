using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Polarite;

using UnityEngine;

public class TextReader : MonoBehaviour
{
    public static string TextOutput;

    public static void Out(string s)
    {
        if (s == null)
        {
            TextOutput = "";
            return;
        }

        var t = TextOutput;

        t = t + s;
        t = t + "\n";

        TextOutput = t;
    }

    public static AudioSource SayString(string s, Transform parent = null, bool dontRemove = false, bool freeze = false)
    {
        if (parent == null)
            parent = MonoSingleton<CameraController>.Instance.transform;
        if (string.IsNullOrEmpty(s)) return null;

        s = s.Trim().ToUpper();

        s = System.Text.RegularExpressions.Regex.Replace(s, @"[^\u0000-\u007F]+", "");
        s = s.Replace("'", "");
        s = s.Replace("\"", "");
        s = s.Replace("\n", " ");

        if (!s.EndsWith(".") && !s.EndsWith("!") && !s.EndsWith("?"))
            s += ".";

        return PlaySAMChunk(s, parent, dontRemove, freeze);
    }

    private static AudioSource PlaySAMChunk(string chunk, Transform parent, bool dontRemove, bool freeze)
    {
        if (string.IsNullOrEmpty(chunk)) return null;

        int[] ints = null;
        try
        {
            string output = UnitySAM.TextToPhonemes(chunk, out ints);

            if (ints == null || ints.Length == 0)
            {
                output = UnitySAM.TextToPhonemes("AH", out ints);
                if (ints == null || ints.Length == 0) return null;
            }

            UnitySAM.SetInput(ints);
            var buf = UnitySAM.SAMMain();
            if (buf == null || buf.GetSize() <= 0) return null;

            AudioClip ac = AudioClip.Create("SAMAudio", buf.GetSize(), 1, 22050, false);
            ac.SetData(buf.GetFloats(), 0);
            AudioSource final = ItePlugin.SpawnSound(ac, 1, parent, 2f, dontRemove: dontRemove);
            if (freeze)
            {
                final.transform.SetParent(null, true);
                final.transform.position = parent.transform.position;
            }
            return final;
        }
        catch (System.Exception e)
        {
            Debug.LogError("SAM chunk failed: " + e.Message);
            return null;
        }
    }
}

