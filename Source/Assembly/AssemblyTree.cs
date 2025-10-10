namespace FTG.Studios.MCC.Assembly;

public class AssemblyTree(AssemblyNode.Program program)
{
	
	public readonly AssemblyNode.Program Program = program;

	public override string ToString() {
		return Program.ToString();
	}
}