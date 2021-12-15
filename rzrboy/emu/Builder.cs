using System.Text;

namespace rzr
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
    public delegate void op( Reg reg, ISection mem );

    /// <summary>
    /// Mnemonic and operand name for this op
    /// </summary>
    /// <param name="pc"></param>
    /// <param name="mem"></param>
    /// <returns></returns>
    public delegate string dis( ref ushort pc, ISection mem);

    public delegate IEnumerable<op> ProduceInstruction( );

    /// <summary>
    /// Instruction op does not include instruction fetch cycle
    /// </summary>
    public class Builder
    {
        public ProduceInstruction Instr { get; }
        private List<dis> m_dis = new();

        public Builder( ProduceInstruction ops, dis? dis = null )
        {
            Instr = ops;
            if ( dis != null )
            {
                m_dis.Add( dis );
            }
        }

        public Builder( op op, dis? dis = null )
            : this (() => Enumerable.Repeat(op, 1), dis)
        {
        }

        public Builder( ProduceInstruction ops, string mnemonic )
        : this( ops, Isa.Ops.mnemonic( mnemonic ) )
        {
        }

        public static implicit operator Builder( op op ) { return new Builder( op ); }

        public static implicit operator Builder( ProduceInstruction ops ) { return new Builder( ops ); }

        public virtual IEnumerable<string> Operands( Ref<ushort> pc, ISection mem )
        {
            return m_dis.Select( dis => dis( ref pc.Value, mem ) );
        }

        public string ToString( ref ushort pc, ISection mem )
        {
            Ref<ushort> ref_pc = new( pc );
            string[] seps = { " ", ", ", "" };
            string[] ops = Operands( ref_pc, mem ).ToArray();

            StringBuilder sb = new();

            int i = 0;
            foreach ( string op in ops )
            {
                sb.Append( op );
                if ( i + 1 < ops.Length )
                {
                    sb.Append( seps[i++] );
                }
            }
            pc = ref_pc;

            return sb.ToString();
        }

        public static Builder operator +(Builder b, dis op) { b.m_dis.Add(op); return b; }
        public static Builder operator +(Builder b, IEnumerable<dis> dis) { b.m_dis.AddRange(dis); return b; }
        public static Builder operator +(Builder b, string str) { b.m_dis.Add(Isa.Ops.mnemonic(str)); return b; }
    }

    public static class BuilderExtensions
    {
        public static Builder Get( this ProduceInstruction op ) => new Builder( op );
        public static Builder Get( this ProduceInstruction op, string mnemonic) => new Builder(op) + mnemonic;
        public static Builder Get( this ProduceInstruction op, dis dis) => new Builder( op ) + dis;

        public static Builder Get( this op op ) => new Builder( op );
        public static Builder Get( this op op, string mnemonic ) => new Builder( op ) + mnemonic;
        public static Builder Get( this op op, dis dis ) => new Builder( op ) + dis;

        // Debug name
        public static string ToString( this ProduceInstruction ops )
        {
            return ops.Method.Name;
        }
    }
}
