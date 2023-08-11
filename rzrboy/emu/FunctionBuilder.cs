using System.Reflection;

namespace rzr
{
	public class FunctionBuilder : ModuleBuilder
	{
		public IDictionary<MethodBase, (ushort pc, byte bank)> Functions => m_functions;
		private Dictionary<System.Reflection.MethodBase, (ushort pc, byte bank)> m_functions = new();

		public enum Linkage
		{
			Inline,
			Call
		}

		[AttributeUsageAttribute( AttributeTargets.Method )]
		public class LinkageAttribute : System.Attribute
		{
			public Linkage Linkage { get; }
			public LinkageAttribute( Linkage linkage )
			{
				Linkage = linkage;
			}
		}

		public class InlineAttribute : LinkageAttribute { public InlineAttribute() : base( Linkage.Inline ) { } }
		public class CallAttribute : LinkageAttribute { public CallAttribute() : base( Linkage.Call ) { } }

		[AttributeUsageAttribute( AttributeTargets.Parameter )]
		public class ParamStorageAttribute : System.Attribute
		{
			public rzr.OperandType Target { get; }
			public ParamStorageAttribute( rzr.OperandType target )
			{
				Target = target;
			}
		}

		public class BCAttribute : ParamStorageAttribute { public BCAttribute() : base( rzr.OperandType.BC ) { } }
		public class DEAttribute : ParamStorageAttribute { public DEAttribute() : base( rzr.OperandType.DE ) { } }
		public class HLAttribute : ParamStorageAttribute { public HLAttribute() : base( rzr.OperandType.HL ) { } }

		private void LoadParameters( System.Reflection.MethodInfo? method, params dynamic[] dynParams )
		{
			for( int i = 0; i < dynParams.Length; i++ )
			{
				var info = method?.GetParameters()[i];
				ParamStorageAttribute? storage = info?.GetCustomAttribute<ParamStorageAttribute>();
				if( storage != null && dynParams[i] != null )
				{
					Instr( rzr.InstrType.Ld, storage.Target, new( dynParams[i] ) );
				}
			}
		}

		public delegate void F3<T1, T2, T3>( T1 t1, T2 t2, T3 t3 );
		public F3<T1, T2, T3> Function<T1, T2, T3>( F3<T1, T2, T3> f )
		{
			MethodInfo type = f.GetMethodInfo();
			LinkageAttribute? linkAttrib = type.GetCustomAttribute<LinkageAttribute>();
			Linkage linkage = linkAttrib != null ? linkAttrib.Linkage : Linkage.Call;

			void Impl( T1 t1, T2 t2, T3 t3 )
			{
				LoadParameters( type, t1, t2, t3 );

				if( linkage == Linkage.Call )
				{
					if( Functions.TryGetValue( type, out var label ) )
					{
						if( BankIdx != label.bank ) // far procedure call
							this.SwitchBank( label.bank );

						Call( label.pc );

						if( BankIdx != label.bank )
							this.SwitchBank( BankIdx );
					}
					else
					{
						ushort pc = PC;
						f( t1, t2, t3 );
						Ret();
						Functions.Add( type, (pc, BankIdx) );
					}
				}
				else if( linkage == Linkage.Inline )
				{
					f( t1, t2, t3 );
				}
			}

			return Impl;
		}
	}
}
