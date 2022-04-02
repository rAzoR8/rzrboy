using Microsoft.Maui.Controls;
using rzr;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace rzrboy
{
	public class AssemblyView : ListView
	{
		public ObservableCollection<rzr.AsmInstr> Assembly { get; }

		public AssemblyView( ObservableCollection<rzr.AsmInstr> assembly )
		{
			Assembly = assembly;
			base.ItemsSource = Assembly;
			base.ItemTemplate = new( () => new InstructionPicker( Asm.Ld( Asm.A, Asm.D ) )  );
		}
    }
}
