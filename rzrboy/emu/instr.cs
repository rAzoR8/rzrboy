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
        public static op ld_imm(byte? target, int next = 0) => (reg reg, mem mem) => { target = mem[reg.PC++]; return next; };
        public static op ld_imm(Reg8 target, int next = 0) => (reg reg, mem mem) => { reg[target] = mem[reg.PC++]; return next; };
        public static op[] ld_imm(Reg8 t1, Reg8 t2, int next = 0) => new op[] { ld_imm(t1, next+1), ld_imm(t2, next)};
        // reg to reg
        public static op ld_reg(Reg8 dst, Reg8 src) => (reg, mem) => { reg[dst] = reg[src]; return 0; };
        public static op ld_reg_addr(Reg8 dst, Reg16 src_addr) => (reg, mem) => { reg[dst] = mem[reg[src_addr]]; return 0; };
        public static op ld_reg_addr(Reg16 dst_addr, Reg8 src) => (reg, mem) => { mem[reg[dst_addr]] = reg[src]; return 0; };

        public instr(params op[] ops) : base(ops) { }
    }

    public struct instrdesc 
    {
    }
}
