using FTG.Studios.MCC.Parser;

namespace FTG.Studios.MCC.SemanticAnalysis;

public static class SemanticAnalzyer
{
	public static void ResolveIdentifiers(ParseTree tree)
	{
		IdentifierResolver.ResolveIdentifiers(tree);
	}

	public static SymbolTable CheckTypes(ParseTree tree)
	{
		return TypeChecker.CheckTypes(tree);
	}
	
	public static void LabelLoops(ParseTree tree)
	{
		LoopLabeler.LabelLoops(tree);
	}
}