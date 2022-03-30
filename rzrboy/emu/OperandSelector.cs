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
			private static readonly OperandType[] Empty = { };

			public LhsToRhs() { }

			public LhsToRhs( OperandType key )
			{
				Add( key, Empty );
			}

			public static implicit operator LhsToRhs( OperandType key ) => new LhsToRhs(key);

			public LhsToRhs( OperandType key, params OperandType[] values )
			{
				Add( key, values );
			}

			public LhsToRhs( IEnumerable<OperandType> keys, IEnumerable<OperandType> values )
			{
				Add( keys, values );
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
				return Add( key, (IEnumerable<OperandType>) values );
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

		private static readonly LhsToRhs Empty = new();

		private const OperandType D8 = OperandType.d8;
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

			this[InstrType.Db] = D8;
			this[InstrType.Nop] = Empty;
			this[InstrType.Stop] = D8;
			this[InstrType.Halt] = Empty;
			this[InstrType.Di] = Empty;
			this[InstrType.Ei] = Empty;
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
