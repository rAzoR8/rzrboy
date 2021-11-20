namespace emu
{
    /// <summary>
    /// Each op takes one m-cycle.
    /// Returning the index to the next op of the instruction allows easy implementation of conditional instructions like jumps
    /// </summary>
    /// <param name="reg"></param>
    /// <param name="mem"></param>
    /// <returns>index to NEXT op</returns>
    public delegate int op(reg reg, mem mem);

    /// <summary>
    /// Instruction op does not include instruction fetch cycle
    /// </summary>
    public class instr : List<op>
    {
        public readonly static op NOP = (reg reg, mem mem) => 0;

        // read next byte from mem[pc++]
        //
        public static op imm(byte? target, int next = 0) => (reg reg, mem mem) => { target = mem[reg.PC++]; return next; };
        public static op imm(Reg8 target, int next = 0) => (reg reg, mem mem) => { reg[target] = mem[reg.PC++]; return next; };
        public static op[] imm(Reg8 t1, Reg8 t2, int next = 0) => new op[] { imm(t1, next+1), imm(t2, next)};

        public instr(params op[] ops) : base(ops) { }
    }

    public struct instrdesc 
    {
    }
}
