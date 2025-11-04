using UnityEngine;

public static partial class UnitySAM
{
	static void printf(string s)
	{
		Debug.Log("Print:'" + s + "'");
	}

	static void printf( int i)
	{
		int[] ia = new int[1] { i};
		printf(ia);
	}

	static void printf(int[] ia)
	{
		byte[] bytes = new byte[ia.Length];
		for (int i = 0; i < ia.Length; i++)
		{
			if (ia[i] == 0)
			{
				var copy = new byte[i];
				System.Array.Copy( bytes, copy, i);
				bytes = copy;
				break;
			}
			bytes[i] = (byte)ia[i];
		}

		string s = System.Text.Encoding.UTF8.GetString(bytes);

		printf( s);
	}

	static void PrintRule(int offset)
	{
		int i = 1;
		int A = 0;
		string s = "Applying rule: ";
		do
		{
			A = GetRuleByte(offset, i);
			if ((A&127) == '=')
			{
				s = s + " -> ";
			}
			else
			{
				s = s + System.String.Format( "{0}", (char) (A & 127));
			}
			i++;
		} while ((A&128)==0);
		printf(s);
	}

	static void PrintPhonemes( int[] phonemeindex, int[] phonemeLength, int[] stress)
	{
	}
}
