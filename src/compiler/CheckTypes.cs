using System;

namespace FTG.Studios.MCC
{

	public static partial class SemanticAnalzyer
	{
		static void CheckTypesInProgram(SymbolTable symbol_table, ParseNode.Program program)
		{
			foreach (var function in program.FunctionDeclarations)
				CheckTypesInFunctionDeclaration(symbol_table, function);
		}

		static void CheckTypesInFunctionDeclaration(SymbolTable symbol_table, ParseNode.FunctionDeclaration declaration)
		{
			bool is_already_defined = false;
			if (symbol_table.TryGetSymbol(declaration.Identifier.Value, out SymbolTableEntry old_entry))
			{
				// Check if this identifier has previously been defined as a variable
				if (old_entry.SymbolClass != SymbolTable.SymbolClass.Function) throw new SemanticAnalzyerException($"Function \"{declaration.Identifier.Value}\" has already been defined as a variable.", declaration.Identifier.Value);

				// Check that both definitions have the same parameters
				if (old_entry.ParamaterCount != declaration.Parameters.Count) throw new SemanticAnalzyerException($"Function \"{declaration.Identifier.Value}\" has already been defined with {old_entry.ParamaterCount} parameters, attemping to define again with {declaration.Parameters.Count} parameters.", declaration.Identifier.Value);
				
				// A function body can only be defined once
				is_already_defined = old_entry.IsDefined;
				if (is_already_defined && declaration.Body != null) throw new SemanticAnalzyerException($"Function \"{declaration.Identifier.Value}\" can only have one body.", declaration.Identifier.Value);
			}

			// TODO: This should count parameters
			int parameter_count = declaration.Parameters.Count;
			symbol_table.AddFunction(declaration.Identifier.Value, SymbolTable.Type.Integer, is_already_defined || declaration.Body != null, parameter_count);

			if (declaration.Body == null) return;
			foreach (var parameter in declaration.Parameters)
				symbol_table.AddVariable(parameter.Value, SymbolTable.Type.Integer, true);

			// TODO: Do I need to copy the symbol table for each scope?
			CheckTypesInBlock(symbol_table, declaration.Body);
		}

		static void CheckTypesInVariableDeclaration(SymbolTable symbol_table, ParseNode.VariableDeclaration declaration)
		{
			symbol_table.AddVariable(declaration.Identifier.Value, SymbolTable.Type.Integer, declaration.Source != null);
			if (declaration.Source == null) return;
			CheckTypesInExpression(symbol_table, declaration.Source);
		}

		static void CheckTypesInBlock(SymbolTable symbol_table, ParseNode.Block block)
		{
			foreach (var item in block.Items)
			{
				if (item is ParseNode.FunctionDeclaration function_declaration)
				{
					// Nested function definitions are not allowed
					if (function_declaration.Body != null) throw new SemanticAnalzyerException($"Nested definition for function \"{function_declaration.Identifier.Value}\" is not allowed.", function_declaration.Identifier.Value);
					CheckTypesInFunctionDeclaration(symbol_table, function_declaration);
				}
				if (item is ParseNode.VariableDeclaration variable_declaration) CheckTypesInVariableDeclaration(symbol_table, variable_declaration);
				if (item is ParseNode.Statement statement) CheckTypesInStatement(symbol_table, statement);
			}
		}

		static void CheckTypesInStatement(SymbolTable symbol_table, ParseNode.Statement statement)
		{
			if (statement is ParseNode.Return @return)
			{
				CheckTypesInExpression(symbol_table, @return.Expression);
				return;
			}

			if (statement is ParseNode.If @if)
			{
				CheckTypesInExpression(symbol_table, @if.Condition);
				if (@if.Then != null) CheckTypesInStatement(symbol_table, @if.Then);
				if (@if.Else != null) CheckTypesInStatement(symbol_table, @if.Else);

				return;
			}

			if (statement is ParseNode.Block block)
			{
				CheckTypesInBlock(symbol_table, block);
				return;
			}

			if (statement is ParseNode.While @while)
			{
				CheckTypesInExpression(symbol_table, @while.Condition);
				CheckTypesInStatement(symbol_table, @while.Body);
				return;
			}

			if (statement is ParseNode.DoWhile do_while)
			{
				CheckTypesInExpression(symbol_table, do_while.Condition);
				if (do_while.Body != null) CheckTypesInStatement(symbol_table, do_while.Body);
				return;
			}

			if (statement is ParseNode.For @for)
			{
				if (@for.Initialization is ParseNode.VariableDeclaration init_declaration) CheckTypesInVariableDeclaration(symbol_table, init_declaration);
				else if (@for.Initialization is ParseNode.Expression init_expression) CheckTypesInExpression(symbol_table, init_expression);
				else if (@for.Initialization != null) throw new SemanticAnalzyerException($"Unhandled for initialization type \"{@for.Initialization}\"", @for.Initialization.ToString());

				if (@for.Condition != null) CheckTypesInExpression(symbol_table, @for.Condition);
				if (@for.Post != null) CheckTypesInExpression(symbol_table, @for.Post);
				CheckTypesInStatement(symbol_table, @for.Body);
				return;
			}

			if (statement is ParseNode.Break || statement is ParseNode.Continue) return;

			if (statement is ParseNode.Expression expression)
			{
				CheckTypesInExpression(symbol_table, expression);
				return;
			}

			throw new SemanticAnalzyerException($"Unhandled statement type \"{statement}\"", statement.ToString());

		}

		static void CheckTypesInExpression(SymbolTable symbol_table, ParseNode.Expression expression)
		{
			if (expression is ParseNode.Variable variable)
			{
				if (!symbol_table.TryGetSymbol(variable.Identifier.Value, out SymbolTableEntry entry))
					throw new SemanticAnalzyerException($"Variable \"{variable.Identifier.Value}\" does not exist.", variable.Identifier.Value);
				if (entry.SymbolClass != SymbolTable.SymbolClass.Variable || entry.ReturnType != SymbolTable.Type.Integer)
					throw new SemanticAnalzyerException($"Variable \"{variable.Identifier.Value}\" is the wrong type.", variable.Identifier.Value);
				
				return;
			}

			if (expression is ParseNode.FunctionCall function_call)
			{
				if (!symbol_table.TryGetSymbol(function_call.Identifier.Value, out SymbolTableEntry entry))
					throw new SemanticAnalzyerException($"Function \"{function_call.Identifier.Value}\" does not exist.", function_call.Identifier.Value);

				if (entry.SymbolClass != SymbolTable.SymbolClass.Function) throw new SemanticAnalzyerException($"Variable \"{function_call.Identifier.Value}\" is being used like a function.", function_call.Identifier.Value);
				if (entry.ReturnType != SymbolTable.Type.Integer) throw new SemanticAnalzyerException($"Function \"{function_call.Identifier.Value}\" has the wrong return type.", function_call.Identifier.Value);

				// TODO: Eventually this will actually check that the arguments match
				if (entry.ParamaterCount != function_call.Arguments.Count) throw new SemanticAnalzyerException($"Function \"{function_call.Identifier.Value}\" expects {entry.ParamaterCount} arguments, got {function_call.Arguments.Count}.", function_call.Identifier.Value);

				foreach (var argument in function_call.Arguments)
					CheckTypesInExpression(symbol_table, argument);
				
				return;
			}

			if (expression is ParseNode.Assignment assignment)
			{
				CheckTypesInExpression(symbol_table, assignment.Destination);
				CheckTypesInExpression(symbol_table, assignment.Source);
				return;
			}

			if (expression is ParseNode.Conditional conditional)
			{
				CheckTypesInExpression(symbol_table, conditional.Condition);
				CheckTypesInExpression(symbol_table, conditional.Then);
				CheckTypesInExpression(symbol_table, conditional.Else);
				return;
			}

			if (expression is ParseNode.BinaryExpression binary)
			{
				CheckTypesInExpression(symbol_table, binary.LeftExpression);
				CheckTypesInExpression(symbol_table, binary.RightExpression);
				return;
			}
			
			if (expression is ParseNode.UnaryExpression unary)
			{
				CheckTypesInExpression(symbol_table, unary.Expression);
				return;
			}
			
			if (expression is ParseNode.Constant) return;
			
			throw new SemanticAnalzyerException($"Unhandled expression type \"{expression}\"", expression.ToString());
		}
	}
}