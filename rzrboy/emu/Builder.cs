namespace emu
{
    /// <summary>
    /// Each op takes one m-cycle.
    /// </summary>
    /// <param name="reg"></param>
    /// <param name="mem"></param>
    /// <returns>return true if there are more operands, false to skip</returns>
    public delegate bool op(Reg reg, Mem mem);

    public interface IInstruction 
    {
        bool Eval(Reg reg, Mem mem);
    }

    public class Instruction : IInstruction
    {
        private IEnumerator<op> cur;

        public Instruction(IEnumerator<op> cur) {  this.cur = cur; }

        public bool Eval(Reg reg, Mem mem) 
        {
            bool cont = cur.Current(reg, mem) && cur.MoveNext();
            return cont;
        }
    }

    public interface IBuilder
    {
        IInstruction Build();
    }

    /// <summary>
    /// Instruction op does not include instruction fetch cycle
    /// </summary>
    public class Builder : List<op>, IBuilder
    {
        public Builder(params op[] ops) : base(ops) {}

        public static Builder operator +(Builder b, op op) { b.Add(op); return b; } 

        public IInstruction Build() { return new Instruction(GetEnumerator()); }
    }
}
