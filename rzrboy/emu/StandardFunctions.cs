using static rzr.FunctionBuilder;
using static rzr.AsmOperandTypes;

namespace rzr
{
	public static class StandardFunctions
	{
		public static void memcopy( this FunctionBuilder self, ushort dst, ushort src, ushort len ) =>
		self.Function( [Inline] ( [HL] ushort dst, [DE] ushort src, [BC] ushort len ) =>
		{
			//Ld( DE, TileDataStart );
			//Ld( HL, 0x9000 );
			//Ld( BC, (ushort)TileData.Length );

			ushort copy = self.Ld( A, adrDE );
			self.Ld( adrHLi, A );
			self.Inc( DE );
			self.Dec( BC );
			self.Ld( A, B );
			self.Or( C );
			self.Jp( isNZ, copy );
		} )( dst, src, len );

		public static void waitvsync(this FunctionBuilder self) =>
		self.Function([Inline] () =>
		{
			ushort WaitVBlank = self.Ldh(A, 0x44);
			self.Cp(144);
			self.Jp(isC, WaitVBlank);
		})();

		public delegate void IfBlock( AsmRecorder self );
		static void If(this FunctionBuilder self, rzr.AsmOperandTypes.Condtype cond, IfBlock ifBlock, IfBlock elseBlock )
		{
			AsmRecorder temp = new();
			temp.IP = self.IP;

			// JP cond, if_block
			// [else_block]
			// Jp merge
			// [if_block]
			// merge

			var jpCond = Asm.Jp(cond.Type, Asm.A16(0)); // placeholder
			temp.Consume(jpCond);
			elseBlock(temp);
			var jpMerge = Asm.Jp( Asm.A16(0)); // placeholder
			temp.Consume(jpMerge);
			ushort if_block = temp.PC;
			ifBlock(temp);
			ushort merge_block = temp.PC;

			// fixup
			jpCond.R.d16 = if_block;
			jpMerge.L.d16 = merge_block;

			// assemble
			foreach (AsmInstr instr in temp)
			{
				self.Consume(instr);
			}
		}
	}
}
