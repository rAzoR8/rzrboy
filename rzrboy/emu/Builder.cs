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

    // immediate results
    public class ImmRes
    {
        public byte Lsb = 0;
        public byte Msb = 0;

        public ushort Addr16 => (ushort)( Lsb | ( Msb << 8 ) );

        public bool IME = false;
    }

    public delegate IEnumerable<op> InstrOps( ImmRes res );

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

    //public class Instruction : IInstruction
    //{
    //    private IEnumerable<op> ops;
    //    private IEnumerable<dis> dis;

    //    private IEnumerator<op> cur_op;
    //    private IEnumerator<dis> cur_dis;

    //    public Instruction( InstrOps prod, IEnumerable<dis> dis)
    //    {
    //        this.ops = prod();
    //        this.dis = dis;
    //        this.cur_op = this.ops.GetEnumerator();
    //        this.cur_dis = dis.GetEnumerator();
    //    }

    //    public bool Eval( Reg reg, ISection mem )
    //    {
    //        if (cur_op.MoveNext() == false)
    //        {
    //            cur_op = ops.GetEnumerator();
    //            return false;
    //        }

    //        cur_op.Current(reg, mem);
    //        return true;
    //    }

    //    public IEnumerable<string> Disassemble( Ref<ushort> pc, ISection mem )
    //    {
    //        while ( cur_dis.MoveNext() )
    //        {
    //            yield return cur_dis.Current( ref pc.Value, mem );
    //        }

    //        cur_dis = dis.GetEnumerator();
    //    }
    //}

    public interface IBuilder
    {
        IInstruction Build();
    }

    /// <summary>
    /// Instruction op does not include instruction fetch cycle
    /// </summary>
    public class Builder
    {
        public InstrOps Instr { get; }
        private List<dis> m_dis = new();
        private IEnumerator<dis> cur_dis;

        public Builder( InstrOps ops, dis? dis = null )
        {
            Instr = ops;
            if ( dis != null )
            {
                m_dis.Add( dis );
            }

            cur_dis = m_dis.GetEnumerator();
        }

        public Builder( OpBuilderNoArg ops, dis? dis = null )
            : this( ( ImmRes _ ) => ops(), dis )
        {
        }

        public Builder( OpBuilderNoArg ops, string mnemonic )
            : this( ( ImmRes _ ) => ops(), Isa.Ops.mnemonic( mnemonic ) )
        {
        }

        public Builder( InstrOps ops, string mnemonic )
            : this( ops, Isa.Ops.mnemonic( mnemonic ) )
        {
        }

        public delegate IEnumerable<op> OpBuilderNoArg();

        public static implicit operator Builder( OpBuilderNoArg ops ) { return new Builder( (ImmRes _) => ops() ); }

        public static implicit operator Builder( InstrOps ops ) { return new Builder( ops ); }

        public IEnumerable<string> Disassemble( Ref<ushort> pc, ISection mem )
        {
            while ( cur_dis.MoveNext() )
            {
                yield return cur_dis.Current( ref pc.Value, mem );
            }

            cur_dis = m_dis.GetEnumerator();
        }

        public string ToString( ref ushort pc, ISection mem )
        {
            Ref<ushort> ref_pc = new( pc );
            string[] seps = { " ", ", ", "" };
            string[] ops = Disassemble( ref_pc, mem ).ToArray();

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
        public static Builder Get( this InstrOps op ) => new Builder( op );
        public static Builder Get( this InstrOps op, string mnemonic) => new Builder(op) + mnemonic;
        public static Builder Get( this InstrOps op, dis dis) => new Builder( op ) + dis;

        public static Builder Get( this op op, string mnemonic )
        {
            IEnumerable<op> ops( ImmRes _ ) { yield return op; }
            return new Builder( ops, mnemonic );
        }

        public static Builder Get( this op op, dis dis )
        {
            IEnumerable<op> ops( ImmRes _ ) { yield return op; }
            return new Builder( ops, dis );
        }

        // Debug name
        public static string ToString( this InstrOps ops )
        {
            return ops.Method.Name;
        }
    }
}
