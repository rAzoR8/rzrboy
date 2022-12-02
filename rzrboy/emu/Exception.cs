namespace rzr
{
	public abstract class Exception : System.Exception
	{
		public Exception( string message ) : base( message ) { }
	}
	public class ExecException : Exception
	{
		public Reg? State { get; }
		public ExecException( string message, Reg? state = null ) : base( message ) { this.State = state; }
	}
	public class AsmException : Exception { public AsmException( string message ) : base( message ) { } }
}
