using System.Diagnostics;
using System.Text;

namespace emu
{
    public class Cpu
    {
        public Isa isa { get; } = new();
        public Reg reg { get; } = new();
        private Mem mem { get; }

        private byte cur_opcode = 0;
        private IInstruction? cur_instr = null;

        public Cpu( Mem memory ) 
        {
            mem = memory;
        }

        public string Disassemble( ref ushort pc, ISection bin )
        {
            byte opcode = mem[reg.PC]; // fetch
            IBuilder builder = isa[opcode];

            StringBuilder sb = new();

            sb.Append( $"[0x{pc:X4}:0x{opcode:X2}] " );

            if ( builder != null )
            {
                sb.Append( builder.Build().ToString( ++pc, bin ) );
            }
            else
            {
                sb.Append( "not implemented :(" );
            }

            return sb.ToString();
        }

        public IEnumerable<string> Disassemble( ushort from_pc, ushort to_pc, ISection bin ) 
        {
            while ( from_pc < to_pc )
            {
                ushort prev = from_pc;
                yield return Disassemble( ref from_pc, bin );
                if ( prev == from_pc ) 
                    yield break;
            }
        }

        /// <summary>
        /// execute one M-cycle
        /// </summary>
        /// <returns>true if executing the same instruction, falseafter a new one is fetched</returns>
        public bool Tick()
        {
            if ( cur_instr == null || cur_instr.Eval( reg, mem ) == false ) // fetch and exec are interleaved
            {
                cur_opcode = mem[reg.PC]; // fetch
                IBuilder builder = isa[cur_opcode];

                Debug.Write( $"[0x{reg.PC:X4}:0x{cur_opcode:X2}] " );

                // TODO: remove once all instructions are implemented
                if ( builder == null )
                {
                    Debug.WriteLine( "not implemented :(" );
                    return false;
                }

                cur_instr = builder.Build();

                Debug.WriteLine( cur_instr.ToString( reg.PC, mem ) );
                reg.PC++;

                return false;
            }
            return true;
        }
    }
}
