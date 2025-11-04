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

    public static void SayString(string s, Transform parent = null)
    {
        if (parent == null)
            parent = MonoSingleton<CameraController>.Instance.transform;
        if (string.IsNullOrEmpty(s)) return;

        s = s.Trim().ToUpper();

        s = System.Text.RegularExpressions.Regex.Replace(s, @"[^\u0000-\u007F]+", "");
        s = s.Replace("'", "");
        s = s.Replace("\"", "");
        s = s.Replace("\n", " ");

        if (!s.EndsWith(".") && !s.EndsWith("!") && !s.EndsWith("?"))
            s += ".";

        PlaySAMChunk(s, parent);
    }

    private static void PlaySAMChunk(string chunk, Transform parent)
    {
        if (string.IsNullOrEmpty(chunk)) return;

        int[] ints = null;
        try
        {
            string output = UnitySAM.TextToPhonemes(chunk, out ints);

            if (ints == null || ints.Length == 0)
            {
                output = UnitySAM.TextToPhonemes("AH", out ints);
                if (ints == null || ints.Length == 0) return;
            }

            UnitySAM.SetInput(ints);
            var buf = UnitySAM.SAMMain();
            if (buf == null || buf.GetSize() <= 0) return;

            AudioClip ac = AudioClip.Create("SAMAudio", buf.GetSize(), 1, 22050, false);
            ac.SetData(buf.GetFloats(), 0);
            ItePlugin.SpawnSound(ac, 1, parent, 2f);
        }
        catch (System.Exception e)
        {
            Debug.LogError("SAM chunk failed: " + e.Message);
        }
    }
}

