namespace FTG.Studios.MCC
{

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
}