using System.Diagnostics;

namespace rzr
{
	public static class Interrupt
	{
        public const ushort IFRegister = 0xFF0F;
        public const ushort IERegister = 0xFFFF;
        public enum Type : byte
        {
            VBlank = 0,
            Lcdc,
            Timer,
            Serial,
            Joypad
        }

        public readonly record struct Entry( ushort addr, Type type ) 
        {
			public byte bit => (byte)( 1 << (byte)type );
		}

		public static readonly Entry[] Interrupts = new Entry[]{
			new Entry(0x0040, Type.VBlank),
			new Entry(0x0048, Type.Lcdc),
			new Entry(0x0050, Type.Timer),
			new Entry(0x0058, Type.Serial),
			new Entry(0x0060, Type.Joypad),
		};

		/// <summary>
		/// Handle one pending interrupt, 5 cycles
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<Op> HandlePending()
        {
            byte IF = 0; byte IE = 0;
            yield return ( reg, mem ) => IF = mem[0xFF0F];
            yield return ( reg, mem ) => IE = mem[0xFFFF];

            foreach( Entry Int in Interrupts )
			{
				int mask = IF & IE & Int.bit;
				if( mask != 0 )
                {
                    Debug.WriteLine( $"INT 0x{Int.addr:X2}:{Int.type}" );
                    yield return ( reg, mem ) =>
                    {
                        // clear the interrupt being handled now
                        mem[0xFF0F] &= (byte)~Int.bit;
                        reg.IME = IMEState.Disabled; // disable interrupts
                        mem[--reg.SP] = reg.PC.GetMsb();
                    };

                    yield return ( reg, mem ) => mem[--reg.SP] = reg.PC.GetLsb();
                    yield return ( reg, mem ) => reg.PC = Int.addr; // jump
                    
                    break; // done
                }
			}
        }
    }
}
