using UnityEngine;

public static partial class UnitySAM
{
    static int A, X, Y;
    
    static int[] inputtemp;   // secure copy of input tab36096

    static void Code37055(int mem59)
    {
        X = mem59;
        X--;
        A = inputtemp[X];
        Y = A;
        A = tab36376[Y];
        return;
    }

    static void Code37066(int mem58)
    {
        X = mem58;
        INC8( ref X);
        A = inputtemp[X];
        Y = A;
        A = tab36376[Y];
    }

    static int GetRuleByte(int mem62, int Y)
    {
        int address = mem62;

        if (mem62 >= 37541)
        {
            address -= 37541;
            return rules2[address + Y];
        }
        address -= 32000;
        return rules[address + Y];
    }

	static void INC8( ref int xxx)
	{
		xxx++;
		xxx &= 255;
	}

	static void DEC8( ref int xxx)
	{
		xxx--;
		xxx &= 255;
	}

    static bool TextToPhonemes( ref int[] input) // Code36484
    {
        //unsigned char *tab39445 = &mem[39445];   //input and output
        //unsigned char mem29;
        int mem56 = 0;      //output position for phonemes
        int mem57 = 0;
        int mem58 = 0;
        int mem59 = 0;
        int mem60 = 0;
        int mem61 = 0;

        int mem62 = 0;     // memory position of current rule

        int mem64 = 0;      // position of '=' or current character
        int mem65 = 0;     // position of ')'
        int mem66 = 0;     // position of '('
        int mem36653 = 0;

		inputtemp = new int[256];

        inputtemp[0] = 32;

        // secure copy of input
        // because input will be overwritten by phonemes
        X = 1;
        Y = 0;
        do
        {
            //pos36499:
            A = input[Y] & 127;
            if (A >= 112) A = A & 95;
            else if (A >= 96) A = A & 79;

            inputtemp[X] = A;
			INC8(ref X);
			INC8(ref Y);
        } while (Y != 255);

		printf( inputtemp);

        X = 255;
        inputtemp[X] = 27;
        mem61 = 255;


    pos36550:
        A = 255;
        mem56 = 255;


    pos36554:
        while (true)
        {
			INC8( ref mem61);
            X = mem61;

			if (X >= inputtemp.Length) break;

            A = inputtemp[X];
            mem64 = A;
            if (A == '[')
            {
				INC8(ref mem56);
                X = mem56;

                A = 155;
                input[X] = 155;
				INC8( ref X);

                //goto pos36542;
                //          Code39771();    //Code39777();


				var copy = new int[X];
				System.Array.Copy( input, copy, X);
				input = copy;

                return true;
            }

            //pos36579:
            if (A != '.') break;
			INC8(ref X);
            Y = inputtemp[X];
            A = tab36376[Y] & 1;
            if (A != 0) break;
			INC8(ref mem56);
            X = mem56;
            A = '.';
            input[X] = '.';
        } //while


        //pos36607:
        A = mem64;
        Y = A;
        A = tab36376[A];
        mem57 = A;
        if ((A & 2) != 0)
        {
            mem62 = 37541;
            goto pos36700;
        }

        //pos36630:
        A = mem57;
        if (A != 0) goto pos36677;
        A = 32;

		if (X >= inputtemp.Length) return true;

        inputtemp[X] = ' ';
		INC8(ref mem56);
        X = mem56;
        if (X > 120) goto pos36654;
        input[X] = A;
        goto pos36554;

    // -----

    //36653 is unknown. Contains position

    pos36654:
        input[X] = 155;
        A = mem61;
        mem36653 = A;
        //  mem29 = A; // not used
        //  Code36538(); das ist eigentlich
        return true;
        //Code39771();
        //go on if there is more input ???

        mem61 = mem36653;
        goto pos36550;

    pos36677:
        A = mem57 & 128;
        if (A == 0)
        {
            //36683: BRK
			return false;
        }

        // go to the right rules for this character.
        X = mem64 - 'A';
        mem62 = tab37489[X] | (tab37515[X] << 8);

    // -------------------------------------
    // go to next rule
    // -------------------------------------

    pos36700:

        // find next rule
        Y = 0;
        do
        {
			mem62++;
            A = GetRuleByte(mem62, Y);
        } while ((A & 128) == 0);
        INC8( ref Y);

        //pos36720:
        // find '('
        while (true)
        {
            A = GetRuleByte(mem62, Y);
            if (A == '(') break;
			INC8( ref Y);
        }
        mem66 = Y;

        //pos36732:
        // find ')'
        do
        {
			INC8( ref Y);
            A = GetRuleByte(mem62, Y);
        } while (A != ')');
        mem65 = Y;

        //pos36741:
        // find '='
        do
        {
            INC8( ref Y);
            A = GetRuleByte(mem62, Y);
            A = A & 127;
        } while (A != '=');
        mem64 = Y;

        X = mem61;
        mem60 = X;

        // compare the string within the bracket
        Y = mem66;
        INC8( ref Y);
        //pos36759:
        while (true)
        {
            mem57 = inputtemp[X];
            A = GetRuleByte(mem62, Y);
            if (A != mem57) goto pos36700;
            INC8( ref Y);
            if (Y == mem65) break;
            INC8( ref X);
            mem60 = X;
        }

        // the string in the bracket is correct

        //pos36787:
        A = mem61;
        mem59 = mem61;

    pos36791:
        while (true)
        {
			DEC8( ref mem66);
            Y = mem66;
            A = GetRuleByte(mem62, Y);
            mem57 = A;
            //36800: BPL 36805
            if ((A & 128) != 0) goto pos37180;
            X = A & 127;
            A = tab36376[X] & 128;
            if (A == 0) break;
            X = mem59 - 1;
            A = inputtemp[X];
            if (A != mem57) goto pos36700;
            mem59 = X;
        }

        //pos36833:
        A = mem57;
        if (A == ' ') goto pos36895;
        if (A == '#') goto pos36910;
        if (A == '.') goto pos36920;
        if (A == '&') goto pos36935;
        if (A == '@') goto pos36967;
        if (A == '^') goto pos37004;
        if (A == '+') goto pos37019;
        if (A == ':') goto pos37040;
        //  Code42041();    //Error
        //36894: BRK
		return false;

    // --------------

    pos36895:
        Code37055(mem59);
        A = A & 128;
        if (A != 0) goto pos36700;
        pos36905:
        mem59 = X;
        goto pos36791;

    // --------------

    pos36910:
        Code37055(mem59);
        A = A & 64;
        if (A != 0) goto pos36905;
        goto pos36700;

    // --------------


    pos36920:
        Code37055(mem59);
        A = A & 8;
        if (A == 0) goto pos36700;
        pos36930:
        mem59 = X;
        goto pos36791;

    // --------------

    pos36935:
        Code37055(mem59);
        A = A & 16;
        if (A != 0) goto pos36930;
        A = inputtemp[X];
        if (A != 72) goto pos36700;
		DEC8( ref X);
        A = inputtemp[X];
        if ((A == 67) || (A == 83)) goto pos36930;
        goto pos36700;

    // --------------

    pos36967:
        Code37055(mem59);
        A = A & 4;
        if (A != 0) goto pos36930;
        A = inputtemp[X];
        if (A != 72) goto pos36700;
        if ((A != 84) && (A != 67) && (A != 83)) goto pos36700;
        mem59 = X;
        goto pos36791;

    // --------------


    pos37004:
        Code37055(mem59);
        A = A & 32;
        if (A == 0) goto pos36700;

        pos37014:
        mem59 = X;
        goto pos36791;

    // --------------

    pos37019:
        X = mem59;
		DEC8( ref X);
        A = inputtemp[X];
        if ((A == 'E') || (A == 'I') || (A == 'Y')) goto pos37014;
        goto pos36700;
    // --------------

    pos37040:
        Code37055(mem59);
        A = A & 32;
        if (A == 0) goto pos36791;
        mem59 = X;
        goto pos37040;

    //---------------------------------------


    pos37077:
        X = mem58 + 1;
        A = inputtemp[X];
        if (A != 'E') goto pos37157;
        INC8( ref X);
        Y = inputtemp[X];
		DEC8( ref X);
        A = tab36376[Y] & 128;
        if (A == 0) goto pos37108;
        INC8( ref X);
        A = inputtemp[X];
        if (A != 'R') goto pos37113;
        pos37108:
        mem58 = X;
        goto pos37184;

    pos37113:
        if ((A == 83) || (A == 68)) goto pos37108;  // 'S' 'D'
        if (A != 76) goto pos37135; // 'L'
        INC8( ref X);
        A = inputtemp[X];
        if (A != 89) goto pos36700;
        goto pos37108;

    pos37135:
        if (A != 70) goto pos36700;
        INC8( ref X);
        A = inputtemp[X];
        if (A != 85) goto pos36700;
        INC8( ref X);
        A = inputtemp[X];
        if (A == 76) goto pos37108;
        goto pos36700;

    pos37157:
        if (A != 73) goto pos36700;
        INC8( ref X);
        A = inputtemp[X];
        if (A != 78) goto pos36700;
        INC8( ref X);
        A = inputtemp[X];
        if (A == 71) goto pos37108;
        //pos37177:
        goto pos36700;

    // -----------------------------------------

    pos37180:

        A = mem60;
        mem58 = A;

    pos37184:
        Y = mem65 + 1;

        //37187: CPY 64
        //  if(? != 0) goto pos37194;
        if (Y == mem64) goto pos37455;
        mem65 = Y;
        //37196: LDA (62),y
        A = GetRuleByte(mem62, Y);
        mem57 = A;
        X = A;
        A = tab36376[X] & 128;
        if (A == 0) goto pos37226;
        X = mem58 + 1;
        A = inputtemp[X];
        if (A != mem57) goto pos36700;
        mem58 = X;
        goto pos37184;

    pos37226:
        A = mem57;
        if (A == 32) goto pos37295;   // ' '
        if (A == 35) goto pos37310;   // '#'
        if (A == 46) goto pos37320;   // '.'
        if (A == 38) goto pos37335;   // '&'
        if (A == 64) goto pos37367;   // ''
        if (A == 94) goto pos37404;   // ''
        if (A == 43) goto pos37419;   // '+'
        if (A == 58) goto pos37440;   // ':'
        if (A == 37) goto pos37077;   // '%'
        if (A == 37) goto pos37077;   // '%'
    //pos37291:
        //  Code42041(); //Error
        //37294: BRK
		return false;

    // --------------
    pos37295:
        Code37066(mem58);
        A = A & 128;
        if (A != 0) goto pos36700;
    
    pos37305:
        mem58 = X;
        goto pos37184;

    // --------------

    pos37310:
        Code37066(mem58);
        A = A & 64;
        if (A != 0) goto pos37305;
        goto pos36700;

    // --------------


    pos37320:
        Code37066(mem58);
        A = A & 8;
        if (A == 0) goto pos36700;

    pos37330:
        mem58 = X;
        goto pos37184;

    // --------------

    pos37335:
        Code37066(mem58);
        A = A & 16;
        if (A != 0) goto pos37330;
        A = inputtemp[X];
        if (A != 72) goto pos36700;
        INC8( ref X);
        A = inputtemp[X];
        if ((A == 67) || (A == 83)) goto pos37330;
        goto pos36700;

    // --------------


    pos37367:
        Code37066(mem58);
        A = A & 4;
        if (A != 0) goto pos37330;
        A = inputtemp[X];
        if (A != 72) goto pos36700;
        if ((A != 84) && (A != 67) && (A != 83)) goto pos36700;
        mem58 = X;
        goto pos37184;

    // --------------

    pos37404:
        Code37066(mem58);
        A = A & 32;
        if (A == 0) goto pos36700;
        pos37414:
        mem58 = X;
        goto pos37184;

    // --------------

    pos37419:
        X = mem58;
        INC8( ref X);
        A = inputtemp[X];
        if ((A == 69) || (A == 73) || (A == 89)) goto pos37414;
        goto pos36700;

    // ----------------------

    pos37440:

        Code37066(mem58);
        A = A & 32;
        if (A == 0) goto pos37184;
        mem58 = X;
        goto pos37440;
    pos37455:
        Y = mem64;
        mem61 = mem60;

        if (debug)
            PrintRule(mem62);

    pos37461:
        //37461: LDA (62),y
        A = GetRuleByte(mem62, Y);
        mem57 = A;
        A = A & 127;
        if (A != '=')
        {
			INC8( ref mem56);
            X = mem56;
            input[X] = A;
        }

        //37478: BIT 57
        //37480: BPL 37485  //not negative flag
        if ((mem57 & 128) == 0) goto pos37485; //???
        goto pos36554;
    pos37485:
        INC8( ref Y);
        goto pos37461;
    }
}
