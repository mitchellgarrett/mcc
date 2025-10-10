using System;
using System.Collections.Generic;
using FTG.Studios.MCC.Parser;

namespace FTG.Studios.MCC.SemanticAnalysis;

public static class IdentifierResolver
{
	public static void ResolveIdentifiers(ParseTree tree)
	{
		IdentifierMap.Reset();
		IdentifierMap identifier_map = new();
		ResolveIdentifiersInProgram(identifier_map, tree.Program);
	}

	static void ResolveIdentifiersInProgram(IdentifierMap identifier_map, ParseNode.Program program)
	{
		// TODO: Make each function replace itself so the returning of a new value isn't necessary
		for (int i = 0; i < program.Declarations.Count; i++)
		{
			if (program.Declarations[i] is ParseNode.VariableDeclaration variable_declaration) program.Declarations[i] = ResolveIdentifiersInFileScopeVariableDeclaration(identifier_map, variable_declaration);
			if (program.Declarations[i] is ParseNode.FunctionDeclaration function_declaration) program.Declarations[i] = ResolveIdentifiersInFunctionDeclaration(identifier_map, function_declaration, true);
		}
	}

	static ParseNode.FunctionDeclaration ResolveIdentifiersInFunctionDeclaration(IdentifierMap identifier_map, ParseNode.FunctionDeclaration function_declaration, bool has_linkage)
	{
		string unique_identifier = identifier_map.InsertUniqueIdentifier(function_declaration.Identifier.Value, has_linkage, SymbolTable.SymbolClass.Function);

		identifier_map = identifier_map.Copy();

		List<ParseNode.Identifier> unique_parameters = [];
		foreach (var parameter in function_declaration.Parameters)
		{
			ParseNode.Identifier unique_parameter = new(identifier_map.InsertUniqueIdentifier(parameter.Value, false, SymbolTable.SymbolClass.Variable));
			unique_parameters.Add(unique_parameter);
		}

		ParseNode.Block unique_body = null;
		if (function_declaration.Body != null) unique_body = ResolveIdentifiersInBlock(identifier_map, function_declaration.Body);

		return new ParseNode.FunctionDeclaration(new ParseNode.Identifier(unique_identifier), function_declaration.ReturnType, function_declaration.StorageClass, unique_parameters, unique_body);
	}

	static ParseNode.VariableDeclaration ResolveIdentifiersInFileScopeVariableDeclaration(IdentifierMap identifier_map, ParseNode.VariableDeclaration variable_declaration)
	{
		// A file scope variable should be either unitialized or a constant, so its expression does not need to be checked
		identifier_map.InsertUniqueIdentifier(variable_declaration.Identifier.Value, true, SymbolTable.SymbolClass.Variable);
		return variable_declaration;
	}
	
	static ParseNode.VariableDeclaration ResolveIdentifiersInBlockScopeVariableDeclaration(IdentifierMap identifier_map, ParseNode.VariableDeclaration variable_declaration)
	{
		// A variable cannot be defined in the same scope with different linkage
		if (identifier_map.TryGetUniqueIdentifier(variable_declaration.Identifier.Value, out IdentifierMapEntry old_entry))
		{
			if (old_entry.FromCurrentScope)
			{
				if (!old_entry.HasLinkage || variable_declaration.StorageClass != ParseNode.StorageClass.Extern)
					throw new SemanticAnalzyerException($"Conflicting declarations for variable \"{variable_declaration.Identifier.Value}\".", variable_declaration.Identifier.Value);
			}
		}
		
		// If a variable is extern, its identifier is not renamed
		if (variable_declaration.StorageClass == ParseNode.StorageClass.Extern)
		{
			identifier_map.InsertUniqueIdentifier(variable_declaration.Identifier.Value, true, SymbolTable.SymbolClass.Variable);
			return variable_declaration;
		}
		
		// If a variable is not extern, its identifier is renamed
		string unique_identifier = identifier_map.InsertUniqueIdentifier(variable_declaration.Identifier.Value, false, SymbolTable.SymbolClass.Variable);

		ParseNode.Expression resolved_initialization = null;
		if (variable_declaration.Source != null) resolved_initialization = ResolveIdentifiersInExpression(identifier_map, variable_declaration.Source);

		return new ParseNode.VariableDeclaration(new ParseNode.Identifier(unique_identifier), variable_declaration.Type, variable_declaration.StorageClass, resolved_initialization);
	}
	
	static ParseNode.Block ResolveIdentifiersInBlock(IdentifierMap identifier_map, ParseNode.Block block)
	{
		for (int index = 0; index < block.Items.Count; index++)
		{
			if (block.Items[index] is ParseNode.FunctionDeclaration function_declaration)
			{
				// Nested function definitions are not allowed
				if (function_declaration.Body != null) throw new SemanticAnalzyerException($"Nested definition for function \"{function_declaration.Identifier.Value}\" is not allowed.", function_declaration.Identifier.Value);
				block.Items[index] = ResolveIdentifiersInFunctionDeclaration(identifier_map, function_declaration, true);
			}
			if (block.Items[index] is ParseNode.VariableDeclaration variable_declaration) block.Items[index] = ResolveIdentifiersInBlockScopeVariableDeclaration(identifier_map, variable_declaration);
			if (block.Items[index] is ParseNode.Statement statement) block.Items[index] = ResolveIdentifiersInStatement(identifier_map, statement);
		}
		return block;
	}

	static ParseNode.Statement ResolveIdentifiersInStatement(IdentifierMap identifier_map, ParseNode.Statement statement)
	{
		if (statement is ParseNode.Return return_statement)
		{
			return new ParseNode.Return(ResolveIdentifiersInExpression(identifier_map, return_statement.Expression));
		}

		if (statement is ParseNode.If @if)
		{
			ParseNode.Expression condition = ResolveIdentifiersInExpression(identifier_map, @if.Condition);
			ParseNode.Statement then = @if.Then != null ? ResolveIdentifiersInStatement(identifier_map, @if.Then) : null;
			ParseNode.Statement @else = @if.Else != null ? ResolveIdentifiersInStatement(identifier_map, @if.Else) : null;

			return new ParseNode.If(condition, then, @else);
		}

		if (statement is ParseNode.Block block) return ResolveIdentifiersInBlock(identifier_map.Copy(), block);
		if (statement is ParseNode.While @while) return ResolveIdentifiersInWhile(identifier_map, @while);
		if (statement is ParseNode.DoWhile do_while) return ResolveIdentifiersInDoWhile(identifier_map, do_while);
		if (statement is ParseNode.For @for) return ResolveIdentifiersInFor(identifier_map.Copy(), @for);

		if (statement is ParseNode.Break || statement is ParseNode.Continue) return statement;
		
		if (statement is ParseNode.Expression expression)
		{
			return ResolveIdentifiersInExpression(identifier_map, expression);
		}

		throw new SemanticAnalzyerException($"Unhandled statement type \"{statement}\"", statement.ToString());
	}

	static ParseNode.While ResolveIdentifiersInWhile(IdentifierMap identifier_map, ParseNode.While @while)
	{
		ParseNode.Expression condition = ResolveIdentifiersInExpression(identifier_map, @while.Condition);
		ParseNode.Statement body = ResolveIdentifiersInStatement(identifier_map, @while.Body);

		return new ParseNode.While(condition, body);
	}
	
	static ParseNode.DoWhile ResolveIdentifiersInDoWhile(IdentifierMap identifier_map, ParseNode.DoWhile do_while)
	{
		ParseNode.Expression condition = ResolveIdentifiersInExpression(identifier_map, do_while.Condition);
		ParseNode.Statement body = do_while.Body != null ? ResolveIdentifiersInStatement(identifier_map, do_while.Body) : null;

		return new ParseNode.DoWhile(condition, body);
	}
	
	static ParseNode.For ResolveIdentifiersInFor(IdentifierMap identifier_map, ParseNode.For @for)
	{
		ParseNode.ForInitialization initialization;
		if (@for.Initialization is ParseNode.VariableDeclaration init_declaration) initialization = ResolveIdentifiersInBlockScopeVariableDeclaration(identifier_map, init_declaration);
		else if (@for.Initialization is ParseNode.Expression init_expression) initialization = ResolveIdentifiersInExpression(identifier_map, init_expression);
		else if (@for.Initialization == null) initialization = null;
		else throw new SemanticAnalzyerException($"Unhandled for initialization type \"{@for.Initialization}\"", @for.Initialization.ToString());
		
		ParseNode.Expression condition = @for.Condition != null ? ResolveIdentifiersInExpression(identifier_map, @for.Condition) : null;
		ParseNode.Expression post = @for.Post != null ? ResolveIdentifiersInExpression(identifier_map, @for.Post) : null;
		ParseNode.Statement body = ResolveIdentifiersInStatement(identifier_map, @for.Body);

		return new ParseNode.For(initialization, condition, post, body);
	}

	static ParseNode.Expression ResolveIdentifiersInExpression(IdentifierMap identifier_map, ParseNode.Expression expression)
	{
		if (expression is ParseNode.Assignment assignment)
		{
			// Assignment definition must be a variable to be resolved
			if (!(assignment.Destination is ParseNode.Variable)) throw new SemanticAnalzyerException($"Assignment destination is not a variable ({assignment}).", assignment.ToString());
			return new ParseNode.Assignment(ResolveIdentifiersInExpression(identifier_map, assignment.Destination), ResolveIdentifiersInExpression(identifier_map, assignment.Source));
		}

		if (expression is ParseNode.Conditional conditional)
		{
			ParseNode.Expression condition = ResolveIdentifiersInExpression(identifier_map, conditional.Condition);
			ParseNode.Expression then = ResolveIdentifiersInExpression(identifier_map, conditional.Then);
			ParseNode.Expression @else = ResolveIdentifiersInExpression(identifier_map, conditional.Else);

			return new ParseNode.Conditional(condition, then, @else);
		}

		if (expression is ParseNode.FunctionCall function_call)
		{
			string unique_identifier = identifier_map.GetUniqueIdentifier(function_call.Identifier.Value, SymbolTable.SymbolClass.Function);
			List<ParseNode.Expression> unique_arguments = new List<ParseNode.Expression>();
			foreach (var argument in function_call.Arguments)
				unique_arguments.Add(ResolveIdentifiersInExpression(identifier_map, argument));
			return new ParseNode.FunctionCall(new ParseNode.Identifier(unique_identifier), unique_arguments);
		}

		if (expression is ParseNode.BinaryExpression binary) return new ParseNode.BinaryExpression(binary.Operator, ResolveIdentifiersInExpression(identifier_map, binary.LeftExpression), ResolveIdentifiersInExpression(identifier_map, binary.RightExpression));
		if (expression is ParseNode.Factor factor) return ResolveIdentifiersInFactor(identifier_map, factor);

		throw new SemanticAnalzyerException($"Unhandled expression type \"{expression}\"", expression.ToString());
	}

	static ParseNode.Factor ResolveIdentifiersInFactor(IdentifierMap identifier_map, ParseNode.Factor factor)
	{
		if (factor is ParseNode.Variable variable)
		{
			// Variable must be defined in order to be resolved
			string unique_identifier = identifier_map.GetUniqueIdentifier(variable.Identifier.Value, SymbolTable.SymbolClass.Variable);
			return new ParseNode.Variable(new ParseNode.Identifier(unique_identifier));
		}

		if (factor is ParseNode.UnaryExpression unary) return new ParseNode.UnaryExpression(unary.Operator, ResolveIdentifiersInExpression(identifier_map, unary.Expression));
		if (factor is ParseNode.Constant constant) return constant;

		throw new SemanticAnalzyerException($"Unhandled factor type \"{factor}\"", factor.ToString());
	}
}