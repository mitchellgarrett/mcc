using System.Collections.Generic;

namespace FTG.Studios.MCC {

	public static class SemanticAnalzyer
	{

		static int next_temporary_variable_index;
		static string NextTemporaryVariable
		{
			get { return $"unique.{next_temporary_variable_index++}"; }
		}

		static readonly Dictionary<string, string> variable_names = new Dictionary<string, string>();

		public static void ResolveVariables(ParseTree tree)
		{
			ParseNode.Program program = tree.Program;
			ResolveVariablesInFunction(program.Function);
		}

		static void ResolveVariablesInFunction(ParseNode.Function function)
		{
			variable_names.Clear();
			for (int index = 0; index < function.Body.Count; index++)
			{
				if (function.Body[index] is ParseNode.Declaration declaration) function.Body[index] = ResolveVariablesInDeclaration(declaration);
				if (function.Body[index] is ParseNode.Statement statement) function.Body[index] = ResolveVariablesInStatement(statement);
			}
		}

		static ParseNode.Declaration ResolveVariablesInDeclaration(ParseNode.Declaration declaration)
		{
			if (variable_names.ContainsKey(declaration.Identifier.Value)) System.Environment.Exit(1);
			string unique_identifier = NextTemporaryVariable;
			variable_names.Add(declaration.Identifier.Value, unique_identifier);

			ParseNode.Expression resolved_initialization = null;
			if (declaration.Source != null) resolved_initialization = ResolveVariablesInExpression(declaration.Source);

			return new ParseNode.Declaration(new ParseNode.Identifier(unique_identifier), resolved_initialization);
		}

		static ParseNode.Statement ResolveVariablesInStatement(ParseNode.Statement statement)
		{
			if (statement is ParseNode.ReturnStatement returnStatement)
			{
				return new ParseNode.ReturnStatement(ResolveVariablesInExpression(returnStatement.Expression));
			}

			if (statement is ParseNode.Expression expression)
			{
				return ResolveVariablesInExpression(expression);
			}
			
			System.Environment.Exit(1);
			return null;
		}

		static ParseNode.Expression ResolveVariablesInExpression(ParseNode.Expression expression)
		{
			if (expression is ParseNode.Assignment assignment)
			{
				// Assignment definition must be a variable to be resolved
				if (!(assignment.Destination is ParseNode.Variable)) System.Environment.Exit(1);
				return new ParseNode.Assignment(ResolveVariablesInExpression(assignment.Destination), ResolveVariablesInExpression(assignment.Source));
			}

			if (expression is ParseNode.Variable variable)
			{
				// Variable must be defined in order to be resolved
				if (!variable_names.ContainsKey(variable.Identifier.Value)) System.Environment.Exit(1);
				return new ParseNode.Variable(new ParseNode.Identifier(variable_names[variable.Identifier.Value]));
			}
			
			System.Environment.Exit(1);
			return null;
		}
	}
}