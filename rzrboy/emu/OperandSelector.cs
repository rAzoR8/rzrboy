using System.Collections;

namespace rzr
{
	public class OperandSelector : IEnumerable<InstrType>
	{
		public interface ILhsToRhs : IReadOnlyDictionary<OperandType, List<OperandType>>
		{
			public IReadOnlyList<OperandType> Lhs { get; }
		}

		// Lhs operand -> list of viable Rhs operands
		private class LhsToRhs : Dictionary<OperandType, List<OperandType>>, ILhsToRhs
		{
			private List<OperandType>? m_lhs = null;

			// TODO: sort by length of Name: A -> SP -> IoC -> adrBC
			public IReadOnlyList<OperandType> Lhs { get { return m_lhs != null ? m_lhs : ( m_lhs = Keys.ToList() ); } }

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

		public ILhsToRhs this[InstrType i] => m_instrToOps[i];

		public OperandSelector() 
		{
			m_instrToOps[InstrType.Db] = D8;
			m_instrToOps[InstrType.Nop] = NoOperands;
			m_instrToOps[InstrType.Stop] = D8;
			m_instrToOps[InstrType.Halt] = NoOperands;
			m_instrToOps[InstrType.Di] = NoOperands;
			m_instrToOps[InstrType.Ei] = NoOperands;
			m_instrToOps[InstrType.Ld] = new
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
				.Add( Asm.ioC, Asm.A )
				.Add( Asm.A, Asm.ioC )
				.Add( Asm.HL, SPr8 )
				.Add( Asm.SP, Asm.HL )
				.Add( D16, Asm.A )
				.Add( Asm.A, D16 );
			m_instrToOps[InstrType.Inc] = 
			m_instrToOps[InstrType.Dec] = new
				LhsToRhs( Asm.BcDeHlSp, NoRhs )
				.Add( Asm.BDHAdrHl, NoRhs )
				.Add( Asm.CELA, NoRhs );
			m_instrToOps[InstrType.Add] = new
				LhsToRhs( Asm.A, Asm.BCDEHLAdrHlA )
				.Add( Asm.A, D8 )
				.Add( Asm.SP, R8 );
			m_instrToOps[InstrType.Adc] =
			m_instrToOps[InstrType.Sub] =
			m_instrToOps[InstrType.Sbc] =
			m_instrToOps[InstrType.And] =
			m_instrToOps[InstrType.Or] =
			m_instrToOps[InstrType.Xor] =
			m_instrToOps[InstrType.Cp] = new
				LhsToRhs( Asm.BCDEHLAdrHlA, NoRhs )
				.Add( D8, NoRhs );
			m_instrToOps[InstrType.Jp] = new
				LhsToRhs( Asm.condZCnZnC, D16 )
				.Add(D16, NoRhs)
				.Add(Asm.HL, NoRhs);
			m_instrToOps[InstrType.Jr] = new
				LhsToRhs( Asm.condZCnZnC, R8 )
				.Add( R8, NoRhs );
			m_instrToOps[InstrType.Ret] = new
				LhsToRhs( OperandType.none, NoRhs ) // RET
				.Add( Asm.condZCnZnC, NoRhs ); // RET C ...
			m_instrToOps[InstrType.Reti] = NoOperands;
			m_instrToOps[InstrType.Call] = new
				LhsToRhs( D16, NoRhs )
				.Add( Asm.condZCnZnC, D16 );
			m_instrToOps[InstrType.Rst] = OperandType.RstAddr;
			m_instrToOps[InstrType.Push] =
			m_instrToOps[InstrType.Pop] = Asm.BcDeHlAf;
			m_instrToOps[InstrType.Rla] = NoOperands;
			m_instrToOps[InstrType.Rlca] = NoOperands;
			m_instrToOps[InstrType.Rra] = NoOperands;
			m_instrToOps[InstrType.Rrca] = NoOperands;
			m_instrToOps[InstrType.Daa] = NoOperands;
			m_instrToOps[InstrType.Scf] = NoOperands;
			m_instrToOps[InstrType.Cpl] = NoOperands;
			m_instrToOps[InstrType.Ccf] = NoOperands;
			m_instrToOps[InstrType.Rlc] =
			m_instrToOps[InstrType.Rrc] =
			m_instrToOps[InstrType.Rl] =
			m_instrToOps[InstrType.Rr] =
			m_instrToOps[InstrType.Sla] =
			m_instrToOps[InstrType.Sra] =
			m_instrToOps[InstrType.Swap] =
			m_instrToOps[InstrType.Srl] = Asm.BCDEHLAdrHlA;
			m_instrToOps[InstrType.Bit] =
			m_instrToOps[InstrType.Res] =
			m_instrToOps[InstrType.Bit] = 
			m_instrToOps[InstrType.Set] = new LhsToRhs( OperandType.BitIdx, Asm.BCDEHLAdrHlA );
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
