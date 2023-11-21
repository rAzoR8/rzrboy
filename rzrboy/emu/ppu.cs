namespace rzr
{
    public class Ppu
    {
        public Ppu()
        {
        }

        public void Tick( State state )
		{
			state.pix.Tick++;
        }
    }
}
