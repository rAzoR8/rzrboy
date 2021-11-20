namespace emu
{
    public enum MBCType
    {
        v1
    }
    internal class cartridge : ListSection
    {
 
        public cartridge(byte[] data)
        {
            ushort offset = 0;
            int size = data.Length;

            while (size > 0)
            {
                RSection bank = new(offset, (ushort)(offset + BankSize));
                offset += BankSize;

                var len = Math.Min(bank.End, data.Length - offset);
                Array.Copy(data, bank.mem, len);

                if(rom0 != null) 
                {
                    rom0 = bank;
                }
                else
                {
                    rombanks.Add(bank);
                }

                size -= BankSize;
            }

            Add(rom0);
            Add(romX);
            Add(ram);
        }
    }
}
