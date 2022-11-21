using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rzr
{
	public class OperandSelector : IEnumerable<InstrType>
	{
		// Lhs operand -> list of viable Rhs operands
		public class LhsToRhs : Dictionary<OperandType, List<OperandType>>
		{
			private List<OperandType>? m_lhs = null;
			// TODO: sort by length of Name: A -> SP -> IoC -> adrBC
			public List<OperandType> Lhs { get { return m_lhs != null ? m_lhs : ( m_lhs = Keys.ToList() ); } }

			public LhsToRhs() { }

			public LhsToRhs( OperandType key )
			{
				Add( key, NoRhs );
			}

			public static implicit operator LhsToRhs( OperandType key ) => new LhsToRhs( key );
			public static implicit operator LhsToRhs( OperandType[] keys ) => new LhsToRhs( keys );

			public LhsToRhs( OperandType key, IEnumerable<OperandType> values )
			{
				Add( key, values );
			}

			public LhsToRhs( IEnumerable<OperandType> keys, params OperandType[] values )
			{
				Add( keys, values );
			}

			public LhsToRhs Add( OperandType key, IEnumerable<OperandType> values )
			{
				List<OperandType>? entries;
				if( base.TryGetValue( key, out entries ) == false ) 
				{
					entries = new( values );
					base.Add( key, entries );
				}
				else
				{
					entries.AddRange( values );
				}
				return this;
			}

			public LhsToRhs Add( OperandType key, params OperandType[] values )
			{
				return Add( key, (IEnumerable<OperandType>)values );
			}

			public LhsToRhs Add( IEnumerable<OperandType> keys, IEnumerable<OperandType> values )
			{
				foreach( OperandType key in keys )
				{
					Add( key, values );				
				}
				return this;
			}

			public LhsToRhs Add( IEnumerable<OperandType> keys, params OperandType[] values )
			{
				foreach( OperandType key in keys )
				{
					Add( key, values );
				}
				return this;
			}
		}

		private static readonly Dictionary<InstrType, LhsToRhs> m_instrToOps = new();

		private static readonly LhsToRhs NoOperands = new();
		private static readonly OperandType[] NoRhs = { };

		private const OperandType D8 = OperandType.d8;
		private const OperandType R8 = OperandType.r8;
		private const OperandType D16 = OperandType.d16;
		private const OperandType Io8 = OperandType.io8;
		private const OperandType SPr8 = OperandType.SPr8;

		public LhsToRhs this[InstrType i]
		{
			get => m_instrToOps[i];
			private set
			{
				m_instrToOps[i] = value;
			}
		}

		public OperandSelector() 
		{
			// TODO: reorder by most-used-first
			this[InstrType.Db] = D8;
			this[InstrType.Nop] = NoOperands;
			this[InstrType.Stop] = D8;
			this[InstrType.Halt] = NoOperands;
			this[InstrType.Di] = NoOperands;
			this[InstrType.Ei] = NoOperands;
			this[InstrType.Ld] = new
				LhsToRhs( Asm.BcDeHlSp, D8 )
				.Add( Asm.adrBcDeHlID, Asm.A )
				.Add( Asm.BDHAdrHl, D8 )
				.Add( D16, Asm.SP )
				.Add( Asm.A, Asm.adrBcDeHlID )
				.Add( Asm.CELA, D8 )
				.Add( Asm.BDHAdrHl, Asm.BCDEHLAdrHlA.Take( 6 ) ) // up to (HL)
				.Add( new[] { Asm.B, Asm.D, Asm.H }, Asm.adrHL ) // colum upto HALT
				.Add( Asm.BDHAdrHl, Asm.A )
				.Add( Asm.CELA, Asm.BCDEHLAdrHlA )
				.Add( Io8, Asm.A )
				.Add( Asm.A, Io8 )
				.Add( Asm.IoC, Asm.A )
				.Add( Asm.A, Asm.IoC )
				.Add( Asm.HL, SPr8 )
				.Add( Asm.SP, Asm.HL )
				.Add( D16, Asm.A )
				.Add( Asm.A, D16 );
			this[InstrType.Inc] = new
				LhsToRhs( Asm.BcDeHlSp, NoRhs )
				.Add( Asm.BDHAdrHl, NoRhs )
				.Add( Asm.CELA, NoRhs );
			this[InstrType.Dec] = new
				LhsToRhs( Asm.BcDeHlSp, NoRhs )
				.Add( Asm.BDHAdrHl, NoRhs )
				.Add( Asm.CELA, NoRhs );
			this[InstrType.Add] = new
				LhsToRhs( Asm.A, Asm.BCDEHLAdrHlA )
				.Add( Asm.A, D8 )
				.Add( Asm.SP, R8 );
			this[InstrType.Adc] =
			this[InstrType.Sub] =
			this[InstrType.Sbc] =
			this[InstrType.And] =
			this[InstrType.Xor] =
			this[InstrType.Or] =
			this[InstrType.Cp] = new
				LhsToRhs( Asm.BCDEHLAdrHlA, NoRhs )
				.Add( D8, NoRhs );
			this[InstrType.Jp] = new
				LhsToRhs( Asm.condZCnZnC, D16 )
				.Add(D16, NoRhs)
				.Add(Asm.HL, NoRhs);
			this[InstrType.Jr] = new
				LhsToRhs( Asm.condZCnZnC, R8 )
				.Add( R8, NoRhs );
			this[InstrType.Ret] = new
				LhsToRhs( OperandType.none, NoRhs ) // RET
				.Add( Asm.condZCnZnC, NoRhs ); // RET C ...
			this[InstrType.Reti] = NoOperands;
			this[InstrType.Call] = new
				LhsToRhs( D16, NoRhs )
				.Add( Asm.condZCnZnC, D16 );
			this[InstrType.Rst] = OperandType.RstAddr;
			this[InstrType.Push] =
			this[InstrType.Pop] = Asm.BcDeHlAf;
			this[InstrType.Rla] = NoOperands;
			this[InstrType.Rlca] = NoOperands;
			this[InstrType.Rra] = NoOperands;
			this[InstrType.Rrca] = NoOperands;
			this[InstrType.Daa] = NoOperands;
			this[InstrType.Scf] = NoOperands;
			this[InstrType.Cpl] = NoOperands;
			this[InstrType.Ccf] = NoOperands;
			this[InstrType.Rlc] =
			this[InstrType.Rrc] =
			this[InstrType.Rl] =
			this[InstrType.Rr] =
			this[InstrType.Sla] =
			this[InstrType.Sra] =
			this[InstrType.Swap] =
			this[InstrType.Srl] = Asm.BCDEHLAdrHlA;
			this[InstrType.Bit] =
			this[InstrType.Res] =
			this[InstrType.Bit] = new LhsToRhs( OperandType.BitIdx, Asm.BCDEHLAdrHlA );
		}

		public IEnumerator<InstrType> GetEnumerator()
		{
			return m_instrToOps.Keys.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return m_instrToOps.Keys.GetEnumerator();
		}
	}
}
