using System.Text;

namespace emu
{
    /// <summary>
    /// Each op takes one m-cycle.
    /// </summary>
    /// <param name="reg"></param>
    /// <param name="mem"></param>
    public delegate void op(Reg reg, Mem mem);

    /// <summary>
    /// Mnemonic and operand name for this op
    /// </summary>
    /// <param name="pc"></param>
    /// <param name="mem"></param>
    /// <returns></returns>
    public delegate string dis(ushort pc, Mem mem);

    public interface IInstruction
    {
        bool Eval(Reg reg, Mem mem);
        IEnumerable<string> Disassemble(ushort pc, Mem mem);
    }

    public class Instruction : IInstruction
    {
        private IEnumerable<op> ops;
        private IEnumerable<dis> dis;

        private IEnumerator<op> cur_op;

        public Instruction(IEnumerable<op> ops, IEnumerable<dis> dis)
        {
            this.ops = ops;
            this.dis = dis;
            this.cur_op = ops.GetEnumerator();
        }

        public bool Eval(Reg reg, Mem mem)
        {
            if (cur_op.MoveNext() == false)
            {
                cur_op = ops.GetEnumerator();
                return false;
            }

            cur_op.Current(reg, mem);
            return true;
        }

        public IEnumerable<string> Disassemble(ushort pc, Mem mem)
        {
            var cur = dis.GetEnumerator();
            while (cur.MoveNext())
            {
                yield return cur.Current(pc, mem);
            }
        }
    }

    public interface IBuilder
    {
        IInstruction Build();
    }

    /// <summary>
    /// Instruction op does not include instruction fetch cycle
    /// </summary>
    public class Builder : IBuilder
    {
        private List<op> m_ops = new();
        private List<dis> m_dis = new();

        public Builder(op op, dis? dis = null)
        {
            m_ops.Add(op);
            if (dis != null)
            {
                m_dis.Add(dis);
            }
        }

        public Builder(IEnumerable<op> ops, dis? dis = null)
        {
            m_ops.AddRange(ops);
            if (dis != null)
            {
                m_dis.Add(dis);
            }
        }

        public Builder(op op, string mnemonic)
            : this(op, Isa.Ops.mnemonic(mnemonic))
        {
        }

        public Builder(IEnumerable<op> ops, string mnemonic)
            : this(ops, Isa.Ops.mnemonic(mnemonic))
        {
        }

        public static Builder operator +(Builder b, op op) { b.m_ops.Add(op); return b; }
        public static Builder operator +(Builder b, IEnumerable<op> ops) { b.m_ops.AddRange(ops); return b; }
        public static Builder operator +(Builder b, dis op) { b.m_dis.Add(op); return b; }
        public static Builder operator +(Builder b, IEnumerable<dis> dis) { b.m_dis.AddRange(dis); return b; }
        public static Builder operator +(Builder b, string str) { b.m_dis.Add(Isa.Ops.mnemonic(str)); return b; }
        public static Builder operator +(Builder b, IEnumerable<string> str) { b.m_dis.AddRange(str.Select(s => Isa.Ops.mnemonic(s))); return b; }

        public IInstruction Build() { return new Instruction(m_ops, m_dis); }
    }

    public static class BuilderExtensions
    {
        public static Builder Get(this op op, string mnemonic) => new Builder(op, mnemonic);
        public static Builder Get(this op op, dis? dis = null) => new Builder(op, dis);
        public static Builder Add(this op op, op other) { return new Builder(op) + other; }
        public static Builder Add(this op op, dis other) { return new Builder(op) + other; }

        public static Builder Get(this op[] op, string mnemonic) => new Builder(op, mnemonic);
        public static Builder Get(this op[] op, dis? dis = null) => new Builder(op, dis);
        public static Builder Add(this op[] op, op other) { return new Builder(op) + other; }
        public static Builder Add(this op[] op, dis other) { return new Builder(op) + other; }
    }

    public static class InstructionExtensions
    {
        public static string ToString(this IInstruction instr, Reg reg, Mem mem)
        {
            StringBuilder sb = new();
            string[] seps = { " ", ", " };
            int i = 0;

            var elems = instr.Disassemble(reg.PC, mem);
            foreach (string str in elems)
            {
                sb.Append(str);
                if(i + 1 < elems.Count())
                {
                    sb.Append(seps[i++]);                
                }
            }

            return sb.ToString();
        }
    }
}
