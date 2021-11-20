namespace emu
{
    // returns indext to next op, each op takes one m-cycle
    public delegate uint op(reg reg, mem mem);
    public class instr : List<op>
    {
        public instr(params op[] ops) : base(ops) { }
    }

    public struct instrdesc 
    {
    }

    public class nop : instr 
    {
        public nop() : base( (reg reg, mem  mem) => 0 ) { }
    }
}
