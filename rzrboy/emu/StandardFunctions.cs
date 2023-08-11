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
	}
}
