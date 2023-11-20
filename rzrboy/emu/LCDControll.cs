namespace rzr
{
	public class LCDC
	{
		// 7 LCD & PPU enable: 0 = Off; 1 = On
		// 6 Window tile map area: 0 = 9800–9BFF; 1 = 9C00–9FFF
		// 5 Window enable: 0 = Off; 1 = On
		// 4 BG & Window tile data area: 0 = 8800–97FF; 1 = 8000–8FFF
		// 3 BG tile map area: 0 = 9800–9BFF; 1 = 9C00–9FFF
		// 2 OBJ size: 0 = 8×8; 1 = 8×16
		// 1 OBJ enable: 0 = Off; 1 = On
		// 0 BG & Window enable / priority [Different meaning in CGB Mode]: 0 = Off; 1 = On
		public enum TileDataArea : byte
		{
			Adr8800 = 0, // 8800–97FF
			Adr8000 = 1 // 8000–8FFF
		}
		public enum TileMapArea : byte
		{
			Adr9800 = 0, // 9800–9BFF
			Adr9C00 = 1 // 9C00–9FFF
		}
		public enum ObjectSize : byte
		{
			Tile8x8 = 0,
			Tile8x16 = 1
		}

		public const TileDataArea Adr8000 = TileDataArea.Adr8000;
		public const TileDataArea Adr8800 = TileDataArea.Adr8800;
		public const TileMapArea Adr9800 = TileMapArea.Adr9800;
		public const TileMapArea Adr9C00 = TileMapArea.Adr9C00;

		public byte Value = 0;

		public static implicit operator byte(LCDC lcdc) => lcdc.Value;

		public bool LCDOn { get => Value.IsBitSet(7); set => Binutil.SetBit(ref Value, 7, value); }
		public TileMapArea WinTileMap { get => Value.IsBitSet(6) ? TileMapArea.Adr9800 : TileMapArea.Adr9C00; set => Binutil.SetBit(ref Value, 6, value == TileMapArea.Adr9C00); }
		public bool WindowOn { get => Value.IsBitSet(5); set => Binutil.SetBit(ref Value, 5, value); }
		public TileDataArea TileData { get => Value.IsBitSet(4) ? TileDataArea.Adr8800 : TileDataArea.Adr8000; set => Binutil.SetBit(ref Value, 4, value == TileDataArea.Adr8000); }
		public TileMapArea BGTileMap { get => Value.IsBitSet(3) ? TileMapArea.Adr9800 : TileMapArea.Adr9C00; set => Binutil.SetBit(ref Value, 3, value == TileMapArea.Adr9C00); }
		public ObjectSize ObjSize { get => Value.IsBitSet(2) ? ObjectSize.Tile8x16 : ObjectSize.Tile8x8; set => Binutil.SetBit(ref Value, 2, value == ObjectSize.Tile8x16); }
		public bool ObjOn { get => Value.IsBitSet(1); set => Binutil.SetBit(ref Value, 1, value); }
		public bool BGWindow { get => Value.IsBitSet(0); set => Binutil.SetBit(ref Value, 0, value); }
	}
}