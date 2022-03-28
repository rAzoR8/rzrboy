using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rzr
{
	public class OperandSelector
	{
		private static readonly Dictionary<InstrType, IList<OperandType>> m_firstOperand = new();

		private static readonly OperandType[] Empty = { };
		private static readonly OperandType[] D8 = { OperandType.d8 };
		private static readonly OperandType[] D16 = { OperandType.d16 };

		public IList<OperandType> this[InstrType i]
		{
			get => m_firstOperand[i];
			private set
			{
				Debug.Assert( m_firstOperand[i] == null );
				m_firstOperand[i] = value;
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
			this[InstrType.Ld] = new OperandType[]
			{
				OperandType.BC, OperandType.DE, OperandType.HL, OperandType.SP,
				OperandType.AdrBC, OperandType.AdrDE, OperandType.AdrHLI, OperandType.AdrHLD,
				OperandType.B, OperandType.D, OperandType.H, OperandType.AdrHL,
				OperandType.d16,
				OperandType.C, OperandType.E, OperandType.L, OperandType.A,
				OperandType.io8,
				OperandType.ioC
			};
			this[InstrType.Inc] = this[InstrType.Dec] = new OperandType[]
			{
				OperandType.BC, OperandType.DE, OperandType.HL, OperandType.SP,
				OperandType.B, OperandType.D, OperandType.H, OperandType.AdrHL,
				OperandType.C, OperandType.E, OperandType.L, OperandType.A,
			};
			this[InstrType.Add] = new OperandType[]	{OperandType.A, OperandType.SP, OperandType.HL};
		}
	}
}
