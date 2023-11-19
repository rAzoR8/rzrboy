using System.Diagnostics;

namespace rzr
{
	public enum CartridgeType : byte
    {
        ROM_ONLY = 0x00,

        MBC1 = 0x01,
        MBC1_RAM = 0x02,
        MBC1_RAM_BATTERY = 0x03,

        MBC2 = 0x05,
        MBC2_BATTERY = 0x06,

        ROM_RAM = 0x08,
        ROM_RAM_BATTERY = 0x09,

        MMM01 = 0x0B,
        MMM01_RAM = 0x0C,
        MMM01_RAM_BATTERY = 0x0D,

        MBC3_TIMER_BATTERY = 0x0F,
        MBC3_TIMER_RAM_BATTERY = 0x10,
        MBC3 = 0x11,
        MBC3_RAM = 0x12,
        MBC3_RAM_BATTERY = 0x13,

        MBC5 = 0x19,
        MBC5_RAM = 0x1A,
        MBC5_RAM_BATTERY = 0x1B,
        MBC5_RUMBLE = 0x1C,
        MBC5_RUMBLE_RAM = 0x1D,
        MBC5_RUMBLE_RAM_BATTERY = 0x1E,

        MBC6 = 0x20,
        MBC7_SENSOR_RUMBLE_RAM_BATTERY = 0x22,
        POCKET_CAMERA = 0xFC,
        BANDAI_TAMA5 = 0xFD,
        HuC3 = 0xFE,
        HuC1_RAM_BATTERY = 0xFF
    }

	public static class HeaderExtensions
	{
		public static bool HasRam( this CartridgeType type )
		{
			switch( type )
			{
				case CartridgeType.MBC1_RAM:
				case CartridgeType.MBC1_RAM_BATTERY:
				case CartridgeType.ROM_RAM:
				case CartridgeType.ROM_RAM_BATTERY:
				case CartridgeType.MMM01_RAM:
				case CartridgeType.MMM01_RAM_BATTERY:
				case CartridgeType.MBC3_TIMER_RAM_BATTERY:
				case CartridgeType.MBC3_RAM:
				case CartridgeType.MBC3_RAM_BATTERY:
				case CartridgeType.MBC5_RAM:
				case CartridgeType.MBC5_RAM_BATTERY:
				case CartridgeType.MBC5_RUMBLE_RAM:
				case CartridgeType.MBC5_RUMBLE_RAM_BATTERY:
				case CartridgeType.MBC7_SENSOR_RUMBLE_RAM_BATTERY:
				case CartridgeType.HuC1_RAM_BATTERY:
					return true;
				default:
					return false;
			}
		}

		public static bool HasBattery( this CartridgeType type )
		{
			switch( type )
			{
				case CartridgeType.MBC1_RAM_BATTERY:
				case CartridgeType.MBC2_BATTERY:
				case CartridgeType.ROM_RAM_BATTERY:
				case CartridgeType.MMM01_RAM_BATTERY:
				case CartridgeType.MBC3_TIMER_BATTERY:
				case CartridgeType.MBC3_TIMER_RAM_BATTERY:
				case CartridgeType.MBC3_RAM_BATTERY:
				case CartridgeType.MBC5_RAM_BATTERY:
				case CartridgeType.MBC5_RUMBLE_RAM_BATTERY:
				case CartridgeType.MBC7_SENSOR_RUMBLE_RAM_BATTERY:
				case CartridgeType.HuC1_RAM_BATTERY:
					return true;
				default:
					return false;
			}
		}
	}

    public static class Cartridge
    {
		public static string GetFileName( this Mbc mbc, string extension = ".gb" ) => $"{mbc.Header.Title.ToLower().Replace( ' ', '_' )}_v{mbc.Header.Version}{extension}";

		public static Mbc CreateMbc( byte[] cart ) => CreateMbc( (CartridgeType)cart[(ushort)HeaderOffsets.Type], cart );
		
		public static void SaveRom( this Mbc mbc, string path ) 
		{
			mbc.FinalizeRom();

			System.IO.File.WriteAllBytes( path, mbc.Rom() );
		}

		public static void SaveRam( this Mbc mbc, string path )
		{
			System.IO.File.WriteAllBytes( path, mbc.Ram() );
		}

		public static Mbc CreateMbc( CartridgeType type, byte[] cart )
		{
			Mbc? mbc = null;

			switch( type )
			{
				case CartridgeType.ROM_ONLY:
				case CartridgeType.ROM_RAM:
				case CartridgeType.ROM_RAM_BATTERY:
					mbc = new Mbc( cart );
					break;
				case CartridgeType.MBC1:
				case CartridgeType.MBC1_RAM:
				case CartridgeType.MBC1_RAM_BATTERY:
					mbc = new Mbc1( cart );
					break;
				case CartridgeType.MBC2:
				case CartridgeType.MBC2_BATTERY:
					break;
				case CartridgeType.MMM01:
				case CartridgeType.MMM01_RAM:
				case CartridgeType.MMM01_RAM_BATTERY:
					break;
				case CartridgeType.MBC3:
				case CartridgeType.MBC3_RAM:
				case CartridgeType.MBC3_RAM_BATTERY:
				case CartridgeType.MBC3_TIMER_BATTERY:
				case CartridgeType.MBC3_TIMER_RAM_BATTERY:
					break;
				case CartridgeType.MBC5:
				case CartridgeType.MBC5_RAM:
				case CartridgeType.MBC5_RAM_BATTERY:
				case CartridgeType.MBC5_RUMBLE:
				case CartridgeType.MBC5_RUMBLE_RAM:
				case CartridgeType.MBC5_RUMBLE_RAM_BATTERY:
					break;
				case CartridgeType.MBC6:
					break;
				case CartridgeType.MBC7_SENSOR_RUMBLE_RAM_BATTERY:
					break;
				case CartridgeType.POCKET_CAMERA:
					break;
				case CartridgeType.BANDAI_TAMA5:
					break;
				case CartridgeType.HuC3:
					break;
				case CartridgeType.HuC1_RAM_BATTERY:
					break;
				default:
					break;
			}

			if( mbc == null )
			{
				mbc = new Mbc( cart ); // unknown cart type			
			}

			Debug.Assert( mbc.Header.Type == type );

			// TODO: restore ram
			mbc.Header.Valid();

			return mbc;
		}
    }
}
