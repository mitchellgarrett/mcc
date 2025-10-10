namespace FTG.Studios.MCC.Intermediate;

public class IntermediateTree(IntermediateNode.Program program)
{
	public readonly IntermediateNode.Program Program = program;

	public override string ToString() {
		return Program.ToString();
	}
}