using System.Text;

namespace emu
{
    public class Ref<T> where T: struct
    {
        public Ref( T val )
        {
            Value = val;
        }

        public T Value = default;
        public static implicit operator T( Ref<T> val ) { return val.Value; }

        public override string ToString()
        {
            return $"{Value}";
        }
    }

    /// <summary>
    /// Each op takes one m-cycle.
    /// </summary>
    /// <param name="reg"></param>
    /// <param name="mem"></param>
    public delegate void op(Reg reg, ISection mem );

    /// <summary>
    /// Mnemonic and operand name for this op
    /// </summary>
    /// <param name="pc"></param>
    /// <param name="mem"></param>
    /// <returns></returns>
    public delegate string dis( ref ushort pc, ISection mem);

    public interface IInstruction
    {
        /// <summary>
        /// Returns true while there are more 1 M-cycle ops
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="mem"></param>
        /// <returns></returns>
        bool Eval( Reg reg, ISection mem );
        IEnumerable<string> Disassemble( Ref<ushort> pc, ISection mem );
    }

    public class Instruction : IInstruction
    {
        private IEnumerable<op> ops;
        private IEnumerable<dis> dis;

        private IEnumerator<op> cur_op;
        private IEnumerator<dis> cur_dis;


        public Instruction(IEnumerable<op> ops, IEnumerable<dis> dis)
        {
            this.ops = ops;
            this.dis = dis;
            this.cur_op = ops.GetEnumerator();
            this.cur_dis = dis.GetEnumerator();
        }

        public bool Eval(Reg reg, ISection mem )
        {
            if (cur_op.MoveNext() == false)
            {
                cur_op = ops.GetEnumerator();
                return false;
            }

            cur_op.Current(reg, mem);
            return true;
        }

        public IEnumerable<string> Disassemble( Ref<ushort> pc, ISection mem )
        {
            while ( cur_dis.MoveNext() )
            {
                yield return cur_dis.Current( ref pc.Value, mem );
            }

            //cur_dis = dis.GetEnumerator();
            //yield break;
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

        public static Builder Get( this IEnumerable<op> op, string mnemonic ) => new Builder( op, mnemonic );
        public static Builder Get( this IEnumerable<op> op, dis? dis = null ) => new Builder( op, dis );
        public static Builder Add( this IEnumerable<op> op, op other ) { return new Builder( op ) + other; }
        public static Builder Add( this IEnumerable<op> op, dis other ) { return new Builder( op ) + other; }
    }

    public static class InstructionExtensions
    {
        public static string ToString(this IInstruction instr, ref ushort pc, ISection mem)
        {
            Ref<ushort> ref_pc = new(pc);
            string[] seps = { " ", ", ", "" };
            string[] ops = instr.Disassemble( ref_pc, mem ).ToArray();
          
            StringBuilder sb = new();

            int i = 0;
            foreach ( string op in ops )
            {
                sb.Append( op );
                if(i + 1 < ops .Length)
                {
                    sb.Append(seps[i++]);                
                }
            }
            pc = ref_pc;

            return sb.ToString();
        }
    }
}
