using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Buffer
{
	List<int> Data;

	public Buffer()
	{
		Data = new List<int>();
	}

	public int[] Get()
	{
		return Data.ToArray();
	}

	public int GetSize()
	{
		return Data.Count;
	}

	public void Set( int position, int data)
	{
		while( position >= Data.Count)
		{
			Data.Add( 0);
		}
		Data[position] = data;
	}

	public float[] GetFloats()
	{
		float[] floats = new float[GetSize()];

		for (int i = 0; i < Data.Count; i++)
		{
			floats[i] = (Data[i] - 127) / 255.0f;
		}

		return floats;
	}
}
