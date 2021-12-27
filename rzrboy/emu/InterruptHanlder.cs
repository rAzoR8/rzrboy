using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rzr
{
	public class InterruptHanlder
	{
        public enum Type : byte
        {
            VBlank = 0,
            Lcdc,
            Timer,
            Serial,
            Joypad
        }

        public readonly record struct Interrupt( ushort addr, Type type ) 
        {
			public byte bit => (byte)( 1 << (byte)type );
		}

		public static readonly Interrupt[] Interrupts = new Interrupt[]{
			new Interrupt(0x0040, Type.VBlank),
			new Interrupt(0x0048, Type.Lcdc),
			new Interrupt(0x0050, Type.Timer),
			new Interrupt(0x0058, Type.Serial),
			new Interrupt(0x0060, Type.Joypad),
		};

		// Handle interrupt
		public static IEnumerable<op> HandleInterrupts()
        {
            byte IF = 0; byte IE = 0;
            yield return ( reg, mem ) => IF = mem[0xFF0F];
            yield return ( reg, mem ) => IE = mem[0xFFFF];

            foreach( Interrupt Int in Interrupts )
			{
				int mask = IF & IE & Int.bit;
				if( mask != 0 )
                {
                    yield return ( reg, mem ) =>
                    {
                        // clear the interrupt being handled now
                        mem[0xFF0F] &= (byte)~Int.bit;
                        reg.IME = false; // disable interrupts
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
