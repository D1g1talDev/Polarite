
public static partial class UnitySAM
{
	static int wait1 = 7;
	static int wait2 = 6;

	static int[] pitches = new int[256]; // tab43008

	static int[] frequency1 = new int[256];
	static int[] frequency2 = new int[256];
	static int[] frequency3 = new int[256];

	static int[] amplitude1 = new int[256];
	static int[] amplitude2 = new int[256];
	static int[] amplitude3 = new int[256];

	static int[] sampledConsonantFlag = new int[256]; // tab44800

	//timetable for more accurate c64 simulation
	readonly static int[][] timetable = new int[][]
	{
		new int[] {162, 167, 167, 127, 128},
		new int[] {226, 60, 60, 0, 0},
		new int[] {225, 60, 59, 0, 0},
		new int[] {200, 0, 0, 54, 55},
		new int[] {199, 0, 0, 54, 54}
	};

	static int oldtimetableindex = 0;

	static	Buffer	buf;

	static void Output8BitAry(int index, int[] ary5)
	{
	    int k;
	    bufferpos += timetable[oldtimetableindex][index];
	    oldtimetableindex = index;
	    // write a little bit in advance
	    for(k=0; k<5; k++)
			buf.Set( bufferpos/50 + k, ary5[k]);
	}
	static void Output8Bit(int index, int A)
	{
	    int[] ary = {A,A,A,A,A};
	    Output8BitAry(index, ary);
	}


	//written by me because of different table positions.
	// mem[47] = ...
	// 168=pitches
	// 169=frequency1
	// 170=frequency2
	// 171=frequency3
	// 172=amplitude1
	// 173=amplitude2
	// 174=amplitude3
	static int Read(int p, int Y)
	{
		switch (p)
		{
		case 168: return pitches[Y];
		case 169: return frequency1[Y];
		case 170: return frequency2[Y];
		case 171: return frequency3[Y];
		case 172: return amplitude1[Y];
		case 173: return amplitude2[Y];
		case 174: return amplitude3[Y];
		}
		printf("Error reading from tables: p was" + p.ToString());
		return 0;
	}

	static void Write(int p, int Y, int value)
	{

		switch (p)
		{
		case 168: pitches[Y] = value; return;
		case 169: frequency1[Y] = value; return;
		case 170: frequency2[Y] = value; return;
		case 171: frequency3[Y] = value; return;
		case 172: amplitude1[Y] = value; return;
		case 173: amplitude2[Y] = value; return;
		case 174: amplitude3[Y] = value; return;
		}
		printf("Error writing to tables: p was" + p.ToString());
	}



	// -------------------------------------------------------------------------
	//Code48227
	// Render a sampled sound from the sampleTable.
	//
	//   Phoneme   Sample Start   Sample End
	//   32: S*    15             255
	//   33: SH    257            511
	//   34: F*    559            767
	//   35: TH    583            767
	//   36: /H    903            1023
	//   37: /X    1135           1279
	//   38: Z*    84             119
	//   39: ZH    340            375
	//   40: V*    596            639
	//   41: DH    596            631
	//
	//   42: CH
	//   43: **    399            511
	//
	//   44: J*
	//   45: **    257            276
	//   46: **
	//
	//   66: P*
	//   67: **    743            767
	//   68: **
	//
	//   69: T*
	//   70: **    231            255
	//   71: **
	//
	// The SampledPhonemesTable[] holds flags indicating if a phoneme is
	// voiced or not. If the upper 5 bits are zero, the sample is voiced.
	//
	// Samples in the sampleTable are compressed, with bits being converted to
	// bytes from high bit to low, as follows:
	//
	//   unvoiced 0 bit   -> X
	//   unvoiced 1 bit   -> 5
	//
	//   voiced 0 bit     -> 6
	//   voiced 1 bit     -> 24
	//
	// Where X is a value from the table:
	//
	//   { 0x18, 0x1A, 0x17, 0x17, 0x17 };
	//
	// The index into this table is determined by masking off the lower
	// 3 bits from the SampledPhonemesTable:
	//
	//        index = (SampledPhonemesTable[i] & 7) - 1;
	//
	// For voices samples, samples are interleaved between voiced output.


	// Code48227()
	static void RenderSample(ref int mem66)
	{
		int tempA = 0;
		// current phoneme's index
		mem49 = Y;

		// mask low three bits and subtract 1 get value to
		// convert 0 bits on unvoiced samples.
		A = mem39 & 7;
		X = A - 1;

		// store the result
		mem56 = X;

		// determine which offset to use from table { 0x18, 0x1A, 0x17, 0x17, 0x17 }
		// T, S, Z                0          0x18
		// CH, J, SH, ZH          1          0x1A
		// P, F*, V, TH, DH       2          0x17
		// /H                     3          0x17
		// /X                     4          0x17

		// get value from the table
		mem53 = tab48426[X];
		mem47 = X;      //46016+mem[56]*256

		// voiced sample?
		A = mem39 & 248;
		if (A == 0)
		{
			// voiced phoneme: Z*, ZH, V*, DH
			Y = mem49;
			A = pitches[mem49] >> 4;

			// jump to voiced portion
			goto pos48315;
		}

		Y = A ^ 255;
		pos48274:

		// step through the 8 bits in the sample
		mem56 = 8;

		// get the next sample from the table
		// mem47*256 = offset to start of samples

		{
			int offset = mem47 * 256 + Y;
			// offset %= sampleTable.Length;
			A = sampleTable[offset];
		}
		pos48280:

		// left shift to get the high bit
		tempA = A;
		A = A << 1;
		//48281: BCC 48290

		// bit not set?
		if ((tempA & 128) == 0)
		{
			// convert the bit to value from table
			X = mem53;
			//mem[54296] = X;
			// output the byte
			Output8Bit(1, (X & 0x0f) * 16);
			// if X != 0, exit loop
			if (X != 0) goto pos48296;
		}

		// output a 5 for the on bit
		Output8Bit(2, 5 * 16);

		//48295: NOP
		pos48296:

		X = 0;

		// decrement counter
		DEC8( ref mem56);

		// if not done, jump to top of loop
		if (mem56 != 0) goto pos48280;

		// increment position
		INC8( ref Y);
		if (Y != 0) goto pos48274;

		// restore values and return
		mem44 = 1;
		Y = mem49;
		return;


//		int phase1;

		pos48315:
		// handle voiced samples here

		// number of samples?
		int phase1 = A ^ 255;

		Y = mem66;
		do
		{
			//pos48321:

			// shift through all 8 bits
			mem56 = 8;
			//A = Read(mem47, Y);

			// fetch value from table
			A = sampleTable[mem47 * 256 + Y];

			// loop 8 times
			//pos48327:
			do
			{
				//48327: ASL A
				//48328: BCC 48337

				// left shift and check high bit
				tempA = A;
				A = A << 1;
				if ((tempA & 128) != 0)
				{
					// if bit set, output 26
					X = 26;
					Output8Bit(3, (X & 0xf) * 16);
				}
				else
				{
					//timetable 4
					// bit is not set, output a 6
					X = 6;
					Output8Bit(4, (X & 0xf) * 16);
				}

				DEC8( ref mem56);
			} while (mem56 != 0);

			// move ahead in the table
			INC8( ref Y);

			// continue until counter done
			INC8( ref phase1);

		} while (phase1 != 0);
		//  if (phase1 != 0) goto pos48321;

		// restore values and return
		A = 1;
		mem44 = 1;
		mem66 = Y;
		Y = mem49;
		return;
	}


	static int abs( int x)
	{
		if (x < 0) return -x;
		return x;
	}


	// RENDER THE PHONEMES IN THE LIST
	//
	// The phoneme list is converted into sound through the steps:
	//
	// 1. Copy each phoneme <length> number of times into the frames list,
	//    where each frame represents 10 milliseconds of sound.
	//
	// 2. Determine the transitions lengths between phonemes, and linearly
	//    interpolate the values across the frames.
	//
	// 3. Offset the pitches by the fundamental frequency.
	//
	// 4. Render the each frame.



	//void Code47574()
	public static void Render()
	{
		int phase1 = 0;  //mem43
		int phase2 = 0;
		int phase3 = 0;

		int mem66 = 0;

		int mem38 = 0;
		int mem40 = 0;
		int speedcounter = 0; //mem45
		int mem48 = 0;
		int i;
		if (phonemeIndexOutput[0] == 255) return; //exit if no data

		A = 0;
		X = 0;
		mem44 = 0;


		// CREATE FRAMES
		//
		// The length parameter in the list corresponds to the number of frames
		// to expand the phoneme to. Each frame represents 10 milliseconds of time.
		// So a phoneme with a length of 7 = 7 frames = 70 milliseconds duration.
		//
		// The parameters are copied from the phoneme to the frame verbatim.


		// pos47587:
		do
		{
			// get the index
			Y = mem44;
			// get the phoneme at the index
			A = phonemeIndexOutput[mem44];
			mem56 = A;

			// if terminal phoneme, exit the loop
			if (A == 255) break;

			// period phoneme *.
			if (A == 1)
			{
				// add rising inflection
				A = 1;
				mem48 = 1;
				//goto pos48376;
				AddInflection(mem48, phase1);
			}
			/*
            if (A == 2) goto pos48372;
            */

			// question mark phoneme?
			if (A == 2)
			{
				// create falling inflection
				mem48 = 255;
				AddInflection(mem48, phase1);
			}
			//  pos47615:

			// get the stress amount (more stress = higher pitch)
			phase1 = tab47492[stressOutput[Y] + 1];

			// get number of frames to write
			phase2 = phonemeLengthOutput[Y];
			Y = mem56;

			// copy from the source to the frames list
			do
			{
				frequency1[X] = freq1data[Y];     // F1 frequency
				frequency2[X] = freq2data[Y];     // F2 frequency
				frequency3[X] = freq3data[Y];     // F3 frequency
				amplitude1[X] = ampl1data[Y];     // F1 amplitude
				amplitude2[X] = ampl2data[Y];     // F2 amplitude
				amplitude3[X] = ampl3data[Y];     // F3 amplitude
				sampledConsonantFlag[X] = sampledConsonantFlags[Y];        // phoneme data for sampled consonants
				pitches[X] = pitch + phase1;      // pitch
				INC8( ref X);
				DEC8( ref phase2);
			} while (phase2 != 0);
			INC8( ref mem44);
		} while (mem44 != 0);
		// -------------------
		//pos47694:

		// CREATE TRANSITIONS
		//
		// Linear transitions are now created to smoothly connect the
		// end of one sustained portion of a phoneme to the following
		// phoneme.
		//
		// To do this, three tables are used:
		//
		//  Table         Purpose
		//  =========     ==================================================
		//  blendRank     Determines which phoneme's blend values are used.
		//
		//  blendOut      The number of frames at the end of the phoneme that
		//                will be used to transition to the following phoneme.
		//
		//  blendIn       The number of frames of the following phoneme that
		//                will be used to transition into that phoneme.
		//
		// In creating a transition between two phonemes, the phoneme
		// with the HIGHEST rank is used. Phonemes are ranked on how much
		// their identity is based on their transitions. For example,
		// vowels are and diphthongs are identified by their sustained portion,
		// rather than the transitions, so they are given low values. In contrast,
		// stop consonants (P, B, T, K) and glides (Y, L) are almost entirely
		// defined by their transitions, and are given high rank values.
		//
		// Here are the rankings used by SAM:
		//
		//     Rank    Type                         Phonemes
		//     2       All vowels                   IY, IH, etc.
		//     5       Diphthong endings            YX, WX, ER
		//     8       Terminal liquid consonants   LX, WX, YX, N, NX
		//     9       Liquid consonants            L, RX, W
		//     10      Glide                        R, OH
		//     11      Glide                        WH
		//     18      Voiceless fricatives         S, SH, F, TH
		//     20      Voiced fricatives            Z, ZH, V, DH
		//     23      Plosives, stop consonants    P, T, K, KX, DX, CH
		//     26      Stop consonants              J, GX, B, D, G
		//     27-29   Stop consonants (internal)   **
		//     30      Unvoiced consonants          /H, /X and Q*
		//     160     Nasal                        M
		//
		// To determine how many frames to use, the two phonemes are
		// compared using the blendRank[] table. The phoneme with the
		// higher rank is selected. In case of a tie, a blend of each is used:
		//
		//      if blendRank[phoneme1] ==  blendRank[phomneme2]
		//          // use lengths from each phoneme
		//          outBlendFrames = outBlend[phoneme1]
		//          inBlendFrames = outBlend[phoneme2]
		//      else if blendRank[phoneme1] > blendRank[phoneme2]
		//          // use lengths from first phoneme
		//          outBlendFrames = outBlendLength[phoneme1]
		//          inBlendFrames = inBlendLength[phoneme1]
		//      else
		//          // use lengths from the second phoneme
		//          // note that in and out are SWAPPED!
		//          outBlendFrames = inBlendLength[phoneme2]
		//          inBlendFrames = outBlendLength[phoneme2]
		//
		// Blend lengths can't be less than zero.
		//
		// Transitions are assumed to be symetrical, so if the transition
		// values for the second phoneme are used, the inBlendLength and
		// outBlendLength values are SWAPPED.
		//
		// For most of the parameters, SAM interpolates over the range of the last
		// outBlendFrames-1 and the first inBlendFrames.
		//
		// The exception to this is the Pitch[] parameter, which is interpolates the
		// pitch from the CENTER of the current phoneme to the CENTER of the next
		// phoneme.
		//
		// Here are two examples. First, For example, consider the word "SUN" (S AH N)
		//
		//    Phoneme   Duration    BlendWeight    OutBlendFrames    InBlendFrames
		//    S         2           18             1                 3
		//    AH        8           2              4                 4
		//    N         7           8              1                 2
		//
		// The formant transitions for the output frames are calculated as follows:
		//
		//     flags ampl1 freq1 ampl2 freq2 ampl3 freq3 pitch
		//    ------------------------------------------------
		// S
		//    241     0     6     0    73     0    99    61   Use S (weight 18) for transition instead of AH (weight 2)
		//    241     0     6     0    73     0    99    61   <-- (OutBlendFrames-1) = (1-1) = 0 frames
		// AH
		//      0     2    10     2    66     0    96    59 * <-- InBlendFrames = 3 frames
		//      0     4    14     3    59     0    93    57 *
		//      0     8    18     5    52     0    90    55 *
		//      0    15    22     9    44     1    87    53
		//      0    15    22     9    44     1    87    53
		//      0    15    22     9    44     1    87    53   Use N (weight 8) for transition instead of AH (weight 2).
		//      0    15    22     9    44     1    87    53   Since N is second phoneme, reverse the IN and OUT values.
		//      0    11    17     8    47     1    98    56 * <-- (InBlendFrames-1) = (2-1) = 1 frames
		// N
		//      0     8    12     6    50     1   109    58 * <-- OutBlendFrames = 1
		//      0     5     6     5    54     0   121    61
		//      0     5     6     5    54     0   121    61
		//      0     5     6     5    54     0   121    61
		//      0     5     6     5    54     0   121    61
		//      0     5     6     5    54     0   121    61
		//      0     5     6     5    54     0   121    61
		//
		// Now, consider the reverse "NUS" (N AH S):
		//
		//     flags ampl1 freq1 ampl2 freq2 ampl3 freq3 pitch
		//    ------------------------------------------------
		// N
		//     0     5     6     5    54     0   121    61
		//     0     5     6     5    54     0   121    61
		//     0     5     6     5    54     0   121    61
		//     0     5     6     5    54     0   121    61
		//     0     5     6     5    54     0   121    61
		//     0     5     6     5    54     0   121    61   Use N (weight 8) for transition instead of AH (weight 2)
		//     0     5     6     5    54     0   121    61   <-- (OutBlendFrames-1) = (1-1) = 0 frames
		// AH
		//     0     8    11     6    51     0   110    59 * <-- InBlendFrames = 2
		//     0    11    16     8    48     0    99    56 *
		//     0    15    22     9    44     1    87    53   Use S (weight 18) for transition instead of AH (weight 2)
		//     0    15    22     9    44     1    87    53   Since S is second phoneme, reverse the IN and OUT values.
		//     0     9    18     5    51     1    90    55 * <-- (InBlendFrames-1) = (3-1) = 2
		//     0     4    14     3    58     1    93    57 *
		// S
		//   241     2    10     2    65     1    96    59 * <-- OutBlendFrames = 1
		//   241     0     6     0    73     0    99    61

		A = 0;
		mem44 = 0;
		mem49 = 0; // mem49 starts at as 0
		X = 0;
		while (true) //while No. 1
		{

			// get the current and following phoneme
			Y = phonemeIndexOutput[X];
			A = phonemeIndexOutput[X + 1];
			INC8( ref X);

			// exit loop at end token
			if (A == 255) break;//goto pos47970;


			// get the ranking of each phoneme
			X = A;
			mem56 = blendRank[A];
			A = blendRank[Y];

			// compare the rank - lower rank value is stronger
			if (A == mem56)
			{
				// same rank, so use out blend lengths from each phoneme
				phase1 = outBlendLength[Y];
				phase2 = outBlendLength[X];
			}
			else
				if (A < mem56)
				{
					// first phoneme is stronger, so us it's blend lengths
					phase1 = inBlendLength[X];
					phase2 = outBlendLength[X];
				}
				else
				{
					// second phoneme is stronger, so use it's blend lengths
					// note the out/in are swapped
					phase1 = outBlendLength[Y];
					phase2 = inBlendLength[Y];
				}

			Y = mem44;
			A = mem49 + phonemeLengthOutput[mem44]; // A is mem49 + length
			mem49 = A; // mem49 now holds length + position
			A = A + phase2; //Maybe Problem because of carry flag

			//47776: ADC 42
			speedcounter = A;
			mem47 = 168;
			phase3 = mem49 - phase1; // what is mem49
			A = phase1 + phase2; // total transition?
			mem38 = A;

			X = A;
			X -= 2;
			if ((X & 128) == 0)
				do   //while No. 2
				{
					//pos47810:

					// mem47 is used to index the tables:
					// 168  pitches[]
					// 169  frequency1
					// 170  frequency2
					// 171  frequency3
					// 172  amplitude1
					// 173  amplitude2
					// 174  amplitude3

					mem40 = mem38;

					if (mem47 == 168)     // pitch
					{

						// unlike the other values, the pitches[] interpolates from
						// the middle of the current phoneme to the middle of the
						// next phoneme

						int mem36, mem37;
						// half the width of the current phoneme
						mem36 = phonemeLengthOutput[mem44] >> 1;
						// half the width of the next phoneme
						mem37 = phonemeLengthOutput[mem44 + 1] >> 1;
						// sum the values
						mem40 = mem36 + mem37; // length of both halves
						mem37 += mem49; // center of next phoneme
						mem36 = mem49 - mem36; // center index of current phoneme
						A = Read(mem47, mem37); // value at center of next phoneme - end interpolation value
						//A = mem[address];

						Y = mem36; // start index of interpolation
						mem53 = A - Read(mem47, mem36); // value to center of current phoneme
					}
					else
					{
						// value to interpolate to
						A = Read(mem47, speedcounter);
						// position to start interpolation from
						Y = phase3;
						// value to interpolate from
						mem53 = A - Read(mem47, phase3);
					}

					//Code47503(mem40);
					// ML : Code47503 is division with remainder, and mem50 gets the sign

					// calculate change per frame
					int m53 = mem53;
					mem50 = mem53 & 128;
					int m53abs = abs(m53);
					mem51 = m53abs % mem40; //abs((char)m53) % mem40;
					mem53 = (int)(m53 / mem40);

					// interpolation range
					X = mem40; // number of frames to interpolate over
					Y = phase3; // starting frame


					// linearly interpolate values

					mem56 = 0;
					//47907: CLC
					//pos47908:
					while (true)     //while No. 3
					{
						A = Read(mem47, Y) + mem53; //carry alway cleared

						mem48 = A;
						INC8( ref Y);
						DEC8( ref X);
						if (X == 0) break;

						mem56 += mem51;
						if (mem56 >= mem40)  //???
						{
							mem56 -= mem40; //carry? is set
							//if ((mem56 & 128)==0)
							if ((mem50 & 128) == 0)
							{
								//47935: BIT 50
								//47937: BMI 47943
								if (mem48 != 0) INC8( ref mem48);
							}
							else
							{
								DEC8( ref mem48);
							}
						}
						//pos47945:
						Write(mem47, Y, mem48);
					} //while No. 3

					//pos47952:
					INC8( ref mem47);
					//if (mem47 != 175) goto pos47810;
				} while (mem47 != 175);     //while No. 2
			//pos47963:
			INC8( ref mem44);
			X = mem44;
		}  //while No. 1

		//goto pos47701;
		//pos47970:

		// add the length of this phoneme
		mem48 = mem49 + phonemeLengthOutput[mem44];


		// ASSIGN PITCH CONTOUR
		//
		// This subtracts the F1 frequency from the pitch to create a
		// pitch contour. Without this, the output would be at a single
		// pitch level (monotone).


		// don't adjust pitch if in sing mode
		if (!singmode)
		{
			// iterate through the buffer
			for (i = 0; i < 256; i++)
			{
				// subtract half the frequency of the formant 1.
				// this adds variety to the voice
				pitches[i] -= (frequency1[i] >> 1);
			}
		}

		phase1 = 0;
		phase2 = 0;
		phase3 = 0;
		mem49 = 0;
		speedcounter = 72; //sam standard speed

		// RESCALE AMPLITUDE
		//
		// Rescale volume from a linear scale to decibels.
		//

		//amplitude rescaling
		for (i = 255; i >= 0; i--)
		{
			amplitude1[i] = amplitudeRescale[amplitude1[i]];
			amplitude2[i] = amplitudeRescale[amplitude2[i]];
			amplitude3[i] = amplitudeRescale[amplitude3[i]];
		}

		Y = 0;
		A = pitches[0];
		mem44 = A;
		X = A;
		mem38 = A - (A >> 2);     // 3/4*A ???

		if (debug)
		{
//			PrintOutput(sampledConsonantFlag, frequency1, frequency2, frequency3, amplitude1, amplitude2, amplitude3, pitches);
		}

		// PROCESS THE FRAMES
		//
		// In traditional vocal synthesis, the glottal pulse drives filters, which
		// are attenuated to the frequencies of the formants.
		//
		// SAM generates these formants directly with sin and rectangular waves.
		// To simulate them being driven by the glottal pulse, the waveforms are
		// reset at the beginning of each glottal pulse.

		//finally the loop for sound output
		//pos48078:
		bool pos48159bool = false;

		while (true)
		{
			// get the sampled information on the phoneme
			A = sampledConsonantFlag[Y];
			mem39 = A;

			// unvoiced sampled phoneme?
			A = A & 248;
			if (A != 0)
			{
				// render the sample for the phoneme
				RenderSample(ref mem66);

				// skip ahead two in the phoneme buffer
				Y += 2;
				mem48 -= 2;
			}
			else
			{
				// simulate the glottal pulse and formants
				int[] ary = new int[5];
				int p1 = phase1 * 256; // Fixed point integers because we need to divide later on
				int p2 = phase2 * 256;
				int p3 = phase3 * 256;
				int k;
				for (k = 0; k < 5; k++)
				{
					int sp1 = sinus[0xff & (p1 >> 8)];
					int sp2 = sinus[0xff & (p2 >> 8)];
					int rp3 = rectangle[0xff & (p3 >> 8)];
					int sin1 = sp1 * (amplitude1[Y] & 0x0f);
					int sin2 = sp2 * (amplitude2[Y] & 0x0f);
					int rect = rp3 * (amplitude3[Y] & 0x0f);
					int mux = sin1 + sin2 + rect;
					mux /= 32;
					mux += 128; // Go from signed to unsigned amplitude
					mux &= 255;
					ary[k] = mux;
					p1 += frequency1[Y] * 256 / 4; // Compromise, this becomes a shift and works well
					p2 += frequency2[Y] * 256 / 4;
					p3 += frequency3[Y] * 256 / 4;
				}
				// output the accumulated value
				Output8BitAry(0, ary);
				DEC8( ref speedcounter);
				if (speedcounter != 0) goto pos48155;
				INC8( ref Y); //go to next amplitude

				// decrement the frame count
				DEC8( ref mem48);
			}

			// if the frame count is zero, exit the loop
			if (mem48 == 0) return;
			speedcounter = speed;
			pos48155:

			// decrement the remaining length of the glottal pulse
			DEC8( ref mem44);

			pos48159:
			// finished with a glottal pulse?
			if (mem44 == 0 || pos48159bool)
			{
//				pos48159:			// former location of label here
				pos48159bool = false;

				// fetch the next glottal pulse length
				A = pitches[Y];
				mem44 = A;
				A = A - (A >> 2);
				mem38 = A;

				// reset the formant wave generators to keep them in
				// sync with the glottal pulse
				phase1 = 0;
				phase2 = 0;
				phase3 = 0;
				continue;
			}

			// decrement the count
			DEC8( ref mem38);

			// is the count non-zero and the sampled flag is zero?
			if ((mem38 != 0) || (mem39 == 0))
			{
				// reset the phase of the formants to match the pulse
				phase1 += frequency1[Y];
				phase1 &= 255;
				phase2 += frequency2[Y];
				phase2 &= 255;
				phase3 += frequency3[Y];
				phase3 &= 255;
				continue;
			}

			// voiced sampled phonemes interleave the sample with the
			// glottal pulse. The sample flag is non-zero, so render
			// the sample for the phoneme.
			RenderSample( ref mem66);

			pos48159bool = true;
			goto pos48159;
		} //while


		// The following code is never reached. It's left over from when
		// the voiced sample code was part of this loop, instead of part
		// of RenderSample();

		//pos48315:
		int tempA;
		phase1 = A ^ 255;
		Y = mem66;
		do
		{
			//pos48321:

			mem56 = 8;
			A = Read(mem47, Y);

			//pos48327:
			do
			{
				//48327: ASL A
				//48328: BCC 48337
				tempA = A;
				A = A << 1;
				if ((tempA & 128) != 0)
				{
					X = 26;
					// mem[54296] = X;
					bufferpos += 150;
					buf.Set( bufferpos / 50, (X & 15)*16);
				} else
				{
					//mem[54296] = 6;
					X=6;
					bufferpos += 150;
					buf.Set(bufferpos / 50, (X & 15)*16);
				}

				for(X = wait2; X>0; X--); //wait
				DEC8( ref mem56);
			} while(mem56 != 0);

			INC8( ref Y);
			INC8( ref phase1);

		} while (phase1 != 0);
		//  if (phase1 != 0) goto pos48321;
		A = 1;
		mem44 = 1;
		mem66 = Y;
		Y = mem49;
		return;
	}


	// Create a rising or falling inflection 30 frames prior to
	// index X. A rising inflection is used for questions, and
	// a falling inflection is used for statements.

	static void AddInflection(int mem48, int phase1)
	{
		//pos48372:
		//  mem48 = 255;
		//pos48376:

		// store the location of the punctuation
		mem49 = X;
		A = X;
		int Atemp = A;

		// backup 30 frames
		A = A - 30;
		// if index is before buffer, point to start of buffer
		if (Atemp <= 30) A = 0;
		X = A;

		// FIXME: Explain this fix better, it's not obvious
		// ML : A =, fixes a problem with invalid pitch with '.'
		while ((A = pitches[X]) == 127) INC8( ref X);


		pos48398:
		//48398: CLC
		//48399: ADC 48

		// add the inflection direction
		A += mem48;
		A &= 255;
		phase1 = A;

		// set the inflection
		pitches[X] = A;
		pos48406:

		// increment the position
		INC8( ref X);

		// exit if the punctuation has been reached
		if (X == mem49) return; //goto pos47615;
		if (pitches[X] == 255) goto pos48406;
		A = phase1;
		goto pos48398;
	}

	/*
    SAM's voice can be altered by changing the frequencies of the
    mouth formant (F1) and the throat formant (F2). Only the voiced
    phonemes (5-29 and 48-53) are altered.
	*/
	static void SetMouthThroat(int mouth, int throat)
	{
		int initialFrequency;
		int newFrequency = 0;
		//int mouth; //mem38880
		//int throat; //mem38881

		// mouth formants (F1) 5..29
		int[] mouthFormants5_29 = new int[] {
			0, 0, 0, 0, 0, 10,
			14, 19, 24, 27, 23, 21, 16, 20, 14, 18, 14, 18, 18,
			16, 13, 15, 11, 18, 14, 11, 9, 6, 6, 6};

		// throat formants (F2) 5..29
		int[] throatFormants5_29 = new int [] {
			255, 255,
			255, 255, 255, 84, 73, 67, 63, 40, 44, 31, 37, 45, 73, 49,
			36, 30, 51, 37, 29, 69, 24, 50, 30, 24, 83, 46, 54, 86};

		// there must be no zeros in this 2 tables
		// formant 1 frequencies (mouth) 48..53
		int[] mouthFormants48_53 = new int[] { 19, 27, 21, 27, 18, 13 };

		// formant 2 frequencies (throat) 48..53
		int[] throatFormants48_53 = new int[] { 72, 39, 31, 43, 30, 34 };

		int pos = 5; //mem39216
		//pos38942:
		// recalculate formant frequencies 5..29 for the mouth (F1) and throat (F2)
		while (pos != 30)
		{
			// recalculate mouth frequency
			initialFrequency = mouthFormants5_29[pos];
			if (initialFrequency != 0) newFrequency = trans(mouth, initialFrequency);
			freq1data[pos] = newFrequency;

			// recalculate throat frequency
			initialFrequency = throatFormants5_29[pos];
			if (initialFrequency != 0) newFrequency = trans(throat, initialFrequency);
			freq2data[pos] = newFrequency;
			INC8( ref pos);
		}

		//pos39059:
		// recalculate formant frequencies 48..53
		pos = 48;
		Y = 0;
		while (pos != 54)
		{
			// recalculate F1 (mouth formant)
			initialFrequency = mouthFormants48_53[Y];
			newFrequency = trans(mouth, initialFrequency);
			freq1data[pos] = newFrequency;

			// recalculate F2 (throat formant)
			initialFrequency = throatFormants48_53[Y];
			newFrequency = trans(throat, initialFrequency);
			freq2data[pos] = newFrequency;
			INC8( ref Y);
			INC8( ref pos);
		}
	}


	//return = (mem39212*mem39213) >> 1
	static int trans(int mem39212, int mem39213)
	{
		//pos39008:
		int carry;
		int temp;
		int mem39214, mem39215;
		A = 0;
		mem39215 = 0;
		mem39214 = 0;
		X = 8;
		do
		{
			carry = mem39212 & 1;
			mem39212 = mem39212 >> 1;
			if (carry != 0)
			{
				/*
                        39018: LSR 39212
                        39021: BCC 39033
                        */
				carry = 0;
				A = mem39215;
				temp = (int)A + (int)mem39213;
				A = A + mem39213;
				if (temp > 255) carry = 1;
				mem39215 = A;
			}
			temp = mem39215 & 1;
			mem39215 = (mem39215 >> 1) | ((carry != 0) ? 128 : 0);
			carry = temp;
			//39033: ROR 39215
			DEC8( ref X);
		} while (X != 0);
		temp = mem39214 & 128;
		mem39214 = (mem39214 << 1) | ((carry != 0) ? 1 : 0);
		carry = temp;
		temp = mem39215 & 128;
		mem39215 = (mem39215 << 1) | ((carry != 0) ? 1 : 0);
		carry = temp;

		return mem39215;
	}
}
