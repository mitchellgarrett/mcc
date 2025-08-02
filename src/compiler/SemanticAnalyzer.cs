using System;
using System.Collections.Generic;

namespace FTG.Studios.MCC
{

	public static class SemanticAnalzyer
	{

		static int next_temporary_variable_index;

		static readonly Dictionary<string, string> variable_names = new Dictionary<string, string>();

		static string InsertUniqueIdentifier(string original_identifier)
		{
			if (variable_names.ContainsKey(original_identifier)) throw new Exception($"Variable \"{original_identifier}\" is already defined.");
			string unique_identifier = $"{original_identifier}.{next_temporary_variable_index++}";
			variable_names[original_identifier] = unique_identifier;
			return unique_identifier;
		}

		static string GetUniqueIdentifier(string original_identifier)
		{
			if (variable_names.TryGetValue(original_identifier, out string unique_identifier)) return unique_identifier;
			throw new Exception($"Variable \"{original_identifier}\" is not defined.");
		}

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
			string unique_identifier = InsertUniqueIdentifier(declaration.Identifier.Value);

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

			throw new Exception($"Unhandled statement type \"{statement}\"");
		}

		static ParseNode.Expression ResolveVariablesInExpression(ParseNode.Expression expression)
		{
			if (expression is ParseNode.Assignment assignment)
			{
				// Assignment definition must be a variable to be resolved
				if (!(assignment.Destination is ParseNode.Variable)) throw new Exception($"Assignment destination is not a variable ({assignment}).");
				return new ParseNode.Assignment(ResolveVariablesInExpression(assignment.Destination), ResolveVariablesInExpression(assignment.Source));
			}

			if (expression is ParseNode.BinaryExpression binary) return new ParseNode.BinaryExpression(binary.Operator, ResolveVariablesInExpression(binary.LeftExpression), ResolveVariablesInExpression(binary.RightExpression));
			if (expression is ParseNode.Factor factor) return ResolveVariablesInFactor(factor);

			throw new Exception($"Unhandled expression type \"{expression}\"");
		}

		static ParseNode.Factor ResolveVariablesInFactor(ParseNode.Factor factor)
		{
			if (factor is ParseNode.Variable variable)
			{
				// Variable must be defined in order to be resolved
				string unique_identifier = GetUniqueIdentifier(variable.Identifier.Value);
				Console.WriteLine(unique_identifier);
				return new ParseNode.Variable(new ParseNode.Identifier(unique_identifier));
			}

			if (factor is ParseNode.UnaryExpression unary) return new ParseNode.UnaryExpression(unary.Operator, ResolveVariablesInExpression(unary.Expression));
			if (factor is ParseNode.Constant constant) return constant;

			throw new Exception($"Unhandled factor type \"{factor}\"");
		}
	}
}