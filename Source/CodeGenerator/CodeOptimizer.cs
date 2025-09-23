using FTG.Studios.MCC.Assembly;

namespace FTG.Studios.MCC.CodeGeneration;

public static class CodeOptimizer {
	
	public static void AssignVariables(AssemblyTree tree)
	{
		VariableAssigner.AssignVariables(tree);
	}
	
	public static void FixVariableAccesses(AssemblyTree tree)
	{
		VariableAccessFixer.FixVariableAccesses(tree);
	}
}