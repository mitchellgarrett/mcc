using FTG.Studios.MCC.Assembly;
using FTG.Studios.MCC.SemanticAnalysis;

namespace FTG.Studios.MCC.CodeGeneration;

public static class CodeOptimizer {
	
	public static AssemblySymbolTable GenerateAssemblySymbolTable(SymbolTable symbol_table)
	{
		return AssemblySymbolTableGenerator.GenerateAssemblySymbolTable(symbol_table);
	}
	
	public static void AssignVariables(AssemblyTree tree, AssemblySymbolTable symbol_table)
	{
		VariableAssigner.AssignVariables(tree, symbol_table);
	}
	
	public static void FixVariableAccesses(AssemblyTree tree)
	{
		VariableAccessFixer.FixVariableAccesses(tree);
	}
}