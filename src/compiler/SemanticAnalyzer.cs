namespace FTG.Studios.MCC
{

	public static partial class SemanticAnalzyer
	{
		public static void LabelLoops(ParseTree tree)
		{
			next_loop_label_index = 0;
			LabelLoopsInFunction(tree.Program.Function);
		}
		
		public static void ResolveVariables(ParseTree tree)
		{
			VariableMap variable_map = new VariableMap();
			VariableMap.Reset();
			ResolveVariablesInFunction(variable_map, tree.Program.Function);
		}
	}
}