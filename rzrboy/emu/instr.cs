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
        public instr(params op[] ops) : base(ops) { }
    }

    public struct instrdesc 
    {
    }
}
