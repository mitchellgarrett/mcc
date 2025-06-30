using System.Collections.Generic;

namespace FTG.Studios.MCC {
	
	public static class SemanticAnalzyer {
		
		static int next_temporary_variable_index;
		static string NextTemporaryVariable {
			get { return $"unique.{next_temporary_variable_index++}"; }
		}
		
		static Dictionary<string, string> variable_names = new Dictionary<string, string>();
		
		public static void ResolveVariables(ParseTree tree) {
			ParseNode.Program program = tree.Program;
			ResolveVariablesInFunction(program.Function);
		}
		
		static void ResolveVariablesInFunction(ParseNode.Function function) {
			variable_names.Clear();
			for (int index = 0; index < function.Body.Count; index++) {
				if (function.Body[index] is ParseNode.Declaration declaration) function.Body[index] = ResolveVariablesInDeclaration(declaration);
				if (function.Body[index] is ParseNode.Statement statement) function.Body[index] = ResolveVariablesInStatement(statement);
			}
		}
		
		static ParseNode.Declaration ResolveVariablesInDeclaration(ParseNode.Declaration declaration) {
			if (variable_names.ContainsKey(declaration.Identifier)) System.Environment.Exit(1);
			string unique_identifier = NextTemporaryVariable;
			variable_names.Add(declaration.Identifier, unique_identifier);
			
			ParseNode.Expression resolved_initialization = null;
			if (declaration.Source != null) resolved_initialization = ResolveVariablesInExpression(declaration.Source);
			
			return new ParseNode.Declaration(unique_identifier, resolved_initialization);
		}
	}
}