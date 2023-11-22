namespace rzr
{
	public interface ILogger
	{
		public void Log(string msg, Action? action = null);
		public void Log(System.Exception exception);
	}
}
