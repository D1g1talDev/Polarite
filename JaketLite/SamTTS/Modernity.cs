using UnityEngine;

public static partial class UnitySAM
{
	public static int[] IntArray(string s)
	{
		s = s.ToUpper();

		int x = s.Length;

		var bytes = System.Text.Encoding.UTF8.GetBytes(s);

		int[] intarray = new int[256];
		for (int i = 0; i < bytes.Length; i++)
		{
			intarray[i] = bytes[i];
		}
		return intarray;
	}

	public static string TextToPhonemes(string s, out int[] ints)
	{
		var ia = IntArray( s);

		bool success = TextToPhonemes(ref ia);

		Debug.Log( "success = " + success);

		var what = ia;

		ints = what;
		var bytes = new byte[ what.Length];
		for (int i = 0; i < what.Length; i++)
		{
			if (what[i] == 0)
			{
				var copy = new int[i];
				System.Array.Copy( what, copy, i);
				what = copy;
				break;
			}
			bytes[i] = (byte)what[i];
		}

		string result = System.Text.Encoding.UTF8.GetString(bytes);

		return result;
	}
}
