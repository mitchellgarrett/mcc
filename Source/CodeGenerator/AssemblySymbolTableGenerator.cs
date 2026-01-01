using FTG.Studios.MCC.Assembly;
using FTG.Studios.MCC.SemanticAnalysis;

namespace FTG.Studios.MCC.CodeGeneration;

public static class AssemblySymbolTableGenerator
{
	public static AssemblySymbolTable GenerateAssemblySymbolTable(SymbolTable symbol_table)
	{
		AssemblySymbolTable assembly_symbol_table = new();
		foreach ((string identifier, var entry) in symbol_table)
		{
			if (entry is VariableEntry variable) ConvertVariableEntry(assembly_symbol_table, identifier, variable);
			if (entry is SemanticAnalysis.FunctionEntry function) ConvertFunctionEntry(assembly_symbol_table, identifier, function);
		}
		return assembly_symbol_table;
	}
	
	static void ConvertVariableEntry(AssemblySymbolTable assembly_symbol_table, string identifier, VariableEntry entry)
	{
		AssemblyType type = entry.ReturnType.ToAssemblyType();
		bool is_static = entry.Attributes is IdentifierAttributes.Static;
		assembly_symbol_table.AddObject(identifier, type, is_static, false);
	}
	
	static void ConvertFunctionEntry(AssemblySymbolTable assembly_symbol_table, string identifier, SemanticAnalysis.FunctionEntry entry)
	{
		bool is_defined = (entry.Attributes as IdentifierAttributes.Function).IsDefined;
		assembly_symbol_table.AddFunction(identifier, is_defined);
	}
}