namespace FTG.Studios.MCC
{

	public static partial class SemanticAnalzyer
	{
		public static void ResolveIdentifiers(ParseTree tree)
		{
			IdentifierMap.Reset();
			IdentifierMap identifier_map = new IdentifierMap();
			ResolveIdentifiersInProgram(identifier_map, tree.Program);
		}

		public static SymbolTable CheckTypes(ParseTree tree)
		{
			SymbolTable symbol_table = new SymbolTable();
			CheckTypesInProgram(symbol_table, tree.Program);
			return symbol_table;
		}
		
		public static void LabelLoops(ParseTree tree)
		{
			next_loop_label_index = 0;
			LabelLoopsInProgram(tree.Program);
		}
	}
}