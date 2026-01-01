using System;
using System.Collections.Generic;
using System.Linq;
using FTG.Studios.MCC.Lexer;
using FTG.Studios.MCC.Parser;
using FTG.Studios.MCC.SemanticAnalysis;

namespace FTG.Studios.MCC.Intermediate;

public static class IntermediateGenerator {
	
	static int next_temporary_variable_index;
	static IntermediateNode.Variable NextTemporaryVariable(SymbolTable symbol_table, PrimitiveType type) {
		IntermediateNode.Variable variable = new($"tmp.{next_temporary_variable_index++}");
		symbol_table.AddVariable(variable.Identifier, new IdentifierAttributes.Local(), type);
		return variable;
	}
	
	static int next_temporary_label_index;
	public static IntermediateNode.Label NextTemporaryLabel {
		get { return new IntermediateNode.Label($"L{next_temporary_label_index++}"); }
	}
	
	public static IntermediateTree Generate(ParseTree tree, SymbolTable symbol_table) {
		next_temporary_variable_index = 0;
		next_temporary_label_index = 0;
		IntermediateNode.Program program = GenerateProgram(tree.Program, symbol_table);		
		return new IntermediateTree(program);
	}
	
	static IntermediateNode.Program GenerateProgram(ParseNode.Program program, SymbolTable symbol_table)
	{
		List<IntermediateNode.TopLevel> declarations = [];
		foreach (var function in program.Declarations)
		{
			if (function is ParseNode.FunctionDeclaration function_declaration)
			{
				IntermediateNode.Function intermediate_function = GenerateFunctionDeclaration(symbol_table, function_declaration);
				if (intermediate_function != null) declarations.Add(intermediate_function);
			}
		}
		
		declarations.AddRange(GenerateStaticVariablesFromSymbolTable(symbol_table));
		
		return new IntermediateNode.Program(declarations);
	}
	
	static List<IntermediateNode.StaticVariable> GenerateStaticVariablesFromSymbolTable(SymbolTable symbol_table)
	{
		List<IntermediateNode.StaticVariable> static_variables = [];
		foreach ((string identifier, SymbolTableEntry entry) in symbol_table)
		{
			if (entry.Attributes is IdentifierAttributes.Static static_attributes)
			{
				if (static_attributes.InitialValue is InitialValue.Constant constant) {
					static_variables.Add(new IntermediateNode.StaticVariable(identifier, static_attributes.IsGlobal, constant));
				} else if (static_attributes.InitialValue is InitialValue.Tentative) {
					static_variables.Add(new IntermediateNode.StaticVariable(identifier, static_attributes.IsGlobal, new InitialValue.IntegerConstant(entry.ReturnType, 0)));
				}
				// Ignore variables with no initializer
			}
		}

		return static_variables;
	}
	
	static IntermediateNode.Function GenerateFunctionDeclaration(SymbolTable symbol_table, ParseNode.FunctionDeclaration function)
	{
		// If the function has no body, there is nothing to generate
		if (function.Body == null) return null;

		string identifier = function.Identifier.Value;

		// Convert list of ParseNode.Identifiers to array of strings
		IntermediateNode.Variable[] parameters = function.ParameterIdentifiers.Select(p => new IntermediateNode.Variable(p.Value)).ToArray();

		// Generate instructions for the body of the function
		List<IntermediateNode.Instruction> instructions = [];
		GenerateBlock(instructions, symbol_table, function.Body);

		// Add Return(0) to the end of every function in case there is no explicit return given
		GenerateReturnStatement(instructions, symbol_table, new ParseNode.Return(new ParseNode.IntegerConstant(PrimitiveType.Integer, 0)));

		// Pull the function's globality from the symbol table since it might have been definied with a different globality earlier
		if (!symbol_table.TryGetSymbol(function.Identifier.Value, out SymbolTableEntry entry)) throw new IntermediateGeneratorException($"Function \"{function.Identifier.Value}\" does not exist in symbol table.", null, null, null);
		bool is_global = (entry.Attributes as IdentifierAttributes.Function).IsGlobal;
		
		return new IntermediateNode.Function(identifier, is_global, parameters, instructions.ToArray());
	}

	static void GenerateBlock(List<IntermediateNode.Instruction> instructions, SymbolTable symbol_table, ParseNode.Block block)
	{
		foreach (ParseNode.BlockItem item in block.Items) GenerateBlockItem(instructions, symbol_table, item);
	}

	static void GenerateBlockItem(List<IntermediateNode.Instruction> instructions, SymbolTable symbol_table, ParseNode.BlockItem item)
	{
		if (item is ParseNode.VariableDeclaration declaration) GenerateVariableDeclaration(instructions, symbol_table, declaration);
		if (item is ParseNode.Statement statement) GenerateStatement(instructions, symbol_table, statement);
	}

	static void GenerateVariableDeclaration(List<IntermediateNode.Instruction> instructions, SymbolTable symbol_table, ParseNode.VariableDeclaration declaration)
	{
		// Do not generate initializers for static variables
		if (declaration.StorageClass == StorageClass.Static) return;
		
		// Generate declaration initializer, if it exists
		IntermediateNode.Operand source = null;
		if (declaration.Source != null) source = GenerateExpression(instructions, symbol_table, declaration.Source);
		
		IntermediateNode.Variable destination = new(declaration.Identifier.Value);

		// Copy initialization value to destination
		if (source != null)
		{
			instructions.Add(new IntermediateNode.Comment($"{destination.ToCommentString()} = {source.ToCommentString()}"));
			instructions.Add(new IntermediateNode.Copy(source, destination));
		}
	}

	static void GenerateStatement(List<IntermediateNode.Instruction> instructions, SymbolTable symbol_table, ParseNode.Statement statement)
	{
		if (statement is null) return;
		if (statement is ParseNode.Return return_statement)
		{
			GenerateReturnStatement(instructions, symbol_table, return_statement);
			return;
		}
		if (statement is ParseNode.If if_statement)
		{
			GenerateIfStatement(instructions, symbol_table, if_statement);
			return;
		}
		if (statement is ParseNode.Block block)
		{
			GenerateBlock(instructions, symbol_table, block);
			return;
		}
		if (statement is ParseNode.Expression expression)
		{
			GenerateExpression(instructions, symbol_table, expression);
			return;
		}
		if (statement is ParseNode.Break @break)
		{
			instructions.Add(new IntermediateNode.Jump(@break.InternalLabel));
			return;
		}
		if (statement is ParseNode.Continue @continue)
		{
			instructions.Add(new IntermediateNode.Jump(@continue.InternalLabel));
			return;
		}
		if (statement is ParseNode.While @while)
		{
			GenerateWhileLoop(instructions, symbol_table, @while);
			return;
		}
		if (statement is ParseNode.DoWhile do_while)
		{
			GenerateDoWhileLoop(instructions, symbol_table, do_while);
			return;
		}
		if (statement is ParseNode.For @for)
		{
			GenerateForLoop(instructions, symbol_table, @for);
			return;
		}
		
		throw new IntermediateGeneratorException("GenerateStatement", statement.GetType(), statement, typeof(Nullable), typeof(ParseNode.Return), typeof(ParseNode.Expression), typeof(ParseNode.Break), typeof(ParseNode.Continue), typeof(ParseNode.While), typeof(ParseNode.DoWhile), typeof(ParseNode.For));
	}
	
	static void GenerateReturnStatement(List<IntermediateNode.Instruction> instructions, SymbolTable symbol_table, ParseNode.Return statement) {
		IntermediateNode.Operand value = GenerateExpression(instructions, symbol_table, statement.Expression);
		instructions.Add(new IntermediateNode.Comment($"return {value.ToCommentString()}"));
		instructions.Add(new IntermediateNode.Return(value));
	}

	static void GenerateIfStatement(List<IntermediateNode.Instruction> instructions, SymbolTable symbol_table, ParseNode.If statement)
	{
		if (statement.Else == null)
		{
			GenerateIfStatementWithoutElse(instructions, symbol_table, statement);
			return;
		}
		
		GenerateIfStatementWithElse(instructions, symbol_table, statement);
	}

	static void GenerateIfStatementWithoutElse(List<IntermediateNode.Instruction> instructions, SymbolTable symbol_table, ParseNode.If statement)
	{
		int comment_index = instructions.Count;
		IntermediateNode.Operand condition = GenerateExpression(instructions, symbol_table, statement.Condition);
		instructions.Insert(comment_index, new IntermediateNode.Comment($"compare {condition.ToCommentString()}"));

		IntermediateNode.Label end_label = NextTemporaryLabel;
		instructions.Add(new IntermediateNode.JumpIfZero(end_label.Identifier, condition));
		
		instructions.Add(new IntermediateNode.Comment("then"));
		GenerateStatement(instructions, symbol_table, statement.Then);
		instructions.Add(end_label);
	}

	static void GenerateIfStatementWithElse(List<IntermediateNode.Instruction> instructions, SymbolTable symbol_table, ParseNode.If statement)
	{
		int comment_index = instructions.Count;
		IntermediateNode.Operand condition = GenerateExpression(instructions, symbol_table, statement.Condition);
		instructions.Insert(comment_index, new IntermediateNode.Comment($"compare {condition.ToCommentString()}"));

		IntermediateNode.Label else_label = NextTemporaryLabel;
		instructions.Add(new IntermediateNode.JumpIfZero(else_label.Identifier, condition));
		
		instructions.Add(new IntermediateNode.Comment("then"));
		GenerateStatement(instructions, symbol_table, statement.Then);
		IntermediateNode.Label end_label = NextTemporaryLabel;
		instructions.Add(new IntermediateNode.Jump(end_label.Identifier));
		
		instructions.Add(new IntermediateNode.Comment("otherwise"));
		instructions.Add(else_label);
		GenerateStatement(instructions, symbol_table, statement.Else);
		instructions.Add(end_label);

	}

	static void GenerateWhileLoop(List<IntermediateNode.Instruction> instructions, SymbolTable symbol_table, ParseNode.While statement)
	{
		instructions.Add(new IntermediateNode.Label($"Continue{statement.InternalLabel}"));
		IntermediateNode.Operand value = GenerateExpression(instructions, symbol_table, statement.Condition);
		instructions.Add(new IntermediateNode.JumpIfZero($"Break{statement.InternalLabel}", value));
		GenerateStatement(instructions, symbol_table, statement.Body);
		instructions.Add(new IntermediateNode.Jump($"Continue{statement.InternalLabel}"));
		instructions.Add(new IntermediateNode.Label($"Break{statement.InternalLabel}"));
	}
	
	static void GenerateDoWhileLoop(List<IntermediateNode.Instruction> instructions, SymbolTable symbol_table, ParseNode.DoWhile statement)
	{
		instructions.Add(new IntermediateNode.Label($"Start{statement.InternalLabel}"));
		GenerateStatement(instructions, symbol_table, statement.Body);
		instructions.Add(new IntermediateNode.Label($"Continue{statement.InternalLabel}"));
		IntermediateNode.Operand value = GenerateExpression(instructions, symbol_table, statement.Condition);
		instructions.Add(new IntermediateNode.JumpIfNotZero($"Start{statement.InternalLabel}", value));
		instructions.Add(new IntermediateNode.Label($"Break{statement.InternalLabel}"));
	}

	static void GenerateForLoop(List<IntermediateNode.Instruction> instructions, SymbolTable symbol_table, ParseNode.For statement)
	{
		if (statement.Initialization is ParseNode.VariableDeclaration init_declaration) GenerateVariableDeclaration(instructions, symbol_table, init_declaration);
		else if (statement.Initialization is ParseNode.Expression init_expression) GenerateExpression(instructions, symbol_table, init_expression);
		else if (statement.Initialization is null) { }
		else throw new IntermediateGeneratorException("", null, null, null);

		instructions.Add(new IntermediateNode.Label($"Start{statement.InternalLabel}"));


		if (statement.Condition != null)
		{
			IntermediateNode.Operand value = GenerateExpression(instructions, symbol_table, statement.Condition);
			instructions.Add(new IntermediateNode.JumpIfZero($"Break{statement.InternalLabel}", value));
		}
		
		GenerateStatement(instructions, symbol_table, statement.Body);
		instructions.Add(new IntermediateNode.Label($"Continue{statement.InternalLabel}"));
		
		if (statement.Post != null) GenerateExpression(instructions, symbol_table, statement.Post);
		
		instructions.Add(new IntermediateNode.Jump($"Start{statement.InternalLabel}"));
		instructions.Add(new IntermediateNode.Label($"Break{statement.InternalLabel}"));
	}
	
	static IntermediateNode.Operand GenerateExpression(List<IntermediateNode.Instruction> instructions, SymbolTable symbol_table, ParseNode.Expression expression)
		{
			if (expression is ParseNode.Factor factor) return GenerateFactor(instructions, symbol_table, factor);
			if (expression is ParseNode.BinaryExpression binary) return GenerateBinaryExpression(instructions, symbol_table, binary);
			if (expression is ParseNode.Assignment assignment) return GenerateAssignment(instructions, symbol_table, assignment);
			if (expression is ParseNode.Conditional conditional) return GenerateConditional(instructions, symbol_table, conditional);
			if (expression is ParseNode.FunctionCall function_call) return GenerateFunctionCall(instructions, symbol_table, function_call);

			throw new IntermediateGeneratorException("GenerateExpression", expression.GetType(), expression, typeof(ParseNode.Factor), typeof(ParseNode.BinaryExpression));
		}

	static IntermediateNode.Operand GenerateFactor(List<IntermediateNode.Instruction> instructions, SymbolTable symbol_table, ParseNode.Factor factor)
	{
		if (factor is ParseNode.Cast cast) return GenerateCast(instructions, symbol_table, cast);
		if (factor is ParseNode.UnaryExpression unaryExpression) return GenerateUnaryExpression(instructions, symbol_table, unaryExpression);
		if (factor is ParseNode.IntegerConstant integer) return new IntermediateNode.IntegerConstant(integer.ReturnType, integer.Value);
		if (factor is ParseNode.FloatingPointConstant @double) return new IntermediateNode.FloatingPointConstant(@double.Value);
		if (factor is ParseNode.Variable variable) return new IntermediateNode.Variable(variable.Identifier.Value);
		
		throw new IntermediateGeneratorException("GenerateFactor", factor.GetType(), factor, typeof(ParseNode.UnaryExpression), typeof(ParseNode.IntegerConstant), typeof(ParseNode.Variable));
	}
	
	static IntermediateNode.Operand GenerateCast(List<IntermediateNode.Instruction> instructions, SymbolTable symbol_table, ParseNode.Cast cast)
	{
		var value = GenerateExpression(instructions, symbol_table, cast.Expression);
		if (cast.ReturnType == cast.Expression.ReturnType) return value;
		
		var destination = NextTemporaryVariable(symbol_table, cast.ReturnType);
		
		instructions.Add(new IntermediateNode.Comment($"({cast.ReturnType}) {cast.Expression}"));
		
		// If casting from an integer to a double, use either IntegerToDouble or UnsignedIntegerToDouble
		if (cast.ReturnType == PrimitiveType.Double && cast.Expression.ReturnType != PrimitiveType.Double)
		{
			if (cast.Expression.ReturnType.IsSigned())
				instructions.Add(new IntermediateNode.IntegerToDouble(value, destination));
			else
				instructions.Add(new IntermediateNode.UnsignedIntegerToDouble(value, destination));
		}
		// If casting from a double to an integer, use either DoubleToInteger or UnsignedDoubleToInteger
		else if (cast.ReturnType != PrimitiveType.Double && cast.Expression.ReturnType == PrimitiveType.Double)
		{
			if (cast.ReturnType.IsSigned())
				instructions.Add(new IntermediateNode.DoubleToInteger(value, destination));
			else
				instructions.Add(new IntermediateNode.DoubleToUnsignedInteger(value, destination));
		}
		
		// If casting to a destination of the same size, simply copy the value to its destination
		else if (cast.ReturnType.GetSize() == cast.Expression.ReturnType.GetSize())
			instructions.Add(new IntermediateNode.Copy(value, destination));
		// If casting to a destination of a smaller size, truncate the value
		else if (cast.ReturnType.GetSize() < cast.Expression.ReturnType.GetSize())
			instructions.Add(new IntermediateNode.Truncate(value, destination));
		// If the expression being casted is signed, sign extend its value
		else if (cast.Expression.ReturnType.IsSigned())
			instructions.Add(new IntermediateNode.SignExtend(value, destination));
		// Otherwise, zero extend the value
		else 
			instructions.Add(new IntermediateNode.ZeroExtend(value, destination));
		
		return destination;
	}
	
	static IntermediateNode.Variable GenerateUnaryExpression(List<IntermediateNode.Instruction> instructions, SymbolTable symbol_table, ParseNode.UnaryExpression unary)
	{
		IntermediateNode.Operand source = GenerateExpression(instructions, symbol_table, unary.Expression);
		IntermediateNode.Variable destination = NextTemporaryVariable(symbol_table, unary.ReturnType);
		instructions.Add(new IntermediateNode.Comment($"{destination.ToCommentString()} = {unary.Operator.GetOperator()}{source.ToCommentString()}"));
		instructions.Add(new IntermediateNode.UnaryInstruction(unary.Operator, source, destination));
		return destination;
	}
	
	static IntermediateNode.Variable GenerateBinaryExpression(List<IntermediateNode.Instruction> instructions, SymbolTable symbol_table, ParseNode.BinaryExpression binary) {
		if (binary.Operator == Syntax.BinaryOperator.LogicalAnd) return GenerateLogicalAndExpression(instructions, symbol_table, binary);
		if (binary.Operator == Syntax.BinaryOperator.LogicalOr) return GenerateLogicalOrExpression(instructions, symbol_table, binary);
		
		IntermediateNode.Operand lhs = GenerateExpression(instructions, symbol_table, binary.LeftExpression);
		IntermediateNode.Operand rhs = GenerateExpression(instructions, symbol_table, binary.RightExpression);
		IntermediateNode.Variable destination = NextTemporaryVariable(symbol_table, binary.ReturnType);
		instructions.Add(new IntermediateNode.Comment($"{destination.ToCommentString()} = {lhs.ToCommentString()} {binary.Operator.GetOperator()} {rhs.ToCommentString()}"));
		instructions.Add(new IntermediateNode.BinaryInstruction(binary.Operator, lhs, rhs, destination));
		
		return destination;
	}
	
	static IntermediateNode.Operand GenerateAssignment(List<IntermediateNode.Instruction> instructions, SymbolTable symbol_table, ParseNode.Assignment assignment) {
		IntermediateNode.Operand source = GenerateExpression(instructions, symbol_table, assignment.Source);
		IntermediateNode.Operand destination = GenerateExpression(instructions, symbol_table, assignment.Destination);
		
		instructions.Add(new IntermediateNode.Comment($"{destination.ToCommentString()} = {source.ToCommentString()}"));
		instructions.Add(new IntermediateNode.Copy(source, destination));

		return destination;
	}

	static IntermediateNode.Variable GenerateConditional(List<IntermediateNode.Instruction> instructions, SymbolTable symbol_table, ParseNode.Conditional conditional)
	{
		int comment_index = instructions.Count;
		IntermediateNode.Operand condition = GenerateExpression(instructions, symbol_table, conditional.Condition);
		instructions.Insert(comment_index, new IntermediateNode.Comment($"compare {condition.ToCommentString()}"));

		IntermediateNode.Label else_label = NextTemporaryLabel;
		instructions.Add(new IntermediateNode.JumpIfZero(else_label.Identifier, condition));

		instructions.Add(new IntermediateNode.Comment("then"));
		IntermediateNode.Operand then_value = GenerateExpression(instructions, symbol_table, conditional.Then);
		IntermediateNode.Variable result = NextTemporaryVariable(symbol_table, conditional.ReturnType);
		instructions.Add(new IntermediateNode.Copy(then_value, result));
		IntermediateNode.Label end_label = NextTemporaryLabel;
		instructions.Add(new IntermediateNode.Jump(end_label.Identifier));
		
		instructions.Add(new IntermediateNode.Comment("otherwise"));
		instructions.Add(else_label);
		IntermediateNode.Operand else_value = GenerateExpression(instructions, symbol_table, conditional.Else);
		instructions.Add(new IntermediateNode.Copy(else_value, result));
		instructions.Add(end_label);

		return result;
	}

	static IntermediateNode.Operand GenerateFunctionCall(List<IntermediateNode.Instruction> instructions, SymbolTable symbol_table, ParseNode.FunctionCall function_call)
	{
		instructions.Add(new IntermediateNode.Comment($"call {function_call.Identifier.Value}"));
		
		List<IntermediateNode.Operand> arguments = [];
		foreach (var argument in function_call.Arguments)
		{
			IntermediateNode.Operand argument_value = GenerateExpression(instructions, symbol_table, argument);
			arguments.Add(argument_value);
		}

		IntermediateNode.Operand destination = NextTemporaryVariable(symbol_table, function_call.ReturnType);
		instructions.Add(new IntermediateNode.FunctionCall(function_call.Identifier.Value, arguments.ToArray(), destination));

		return destination;
	}
	
	static IntermediateNode.Variable GenerateLogicalAndExpression(List<IntermediateNode.Instruction> instructions, SymbolTable symbol_table, ParseNode.BinaryExpression expression)
		{
			int comment_index = instructions.Count;

			// If lhs == 0, jump to false
			IntermediateNode.Operand lhs = GenerateExpression(instructions, symbol_table, expression.LeftExpression);
			IntermediateNode.Label false_label = NextTemporaryLabel;
			instructions.Add(new IntermediateNode.JumpIfZero(false_label.Identifier, lhs));

			// If rhs == 0, jump to false
			IntermediateNode.Operand rhs = GenerateExpression(instructions, symbol_table, expression.RightExpression);
			instructions.Add(new IntermediateNode.JumpIfZero(false_label.Identifier, rhs));

			// If lhs == rhs == 1, set result = 1, jump to end
			IntermediateNode.Variable destination = NextTemporaryVariable(symbol_table, PrimitiveType.Integer);
			IntermediateNode.Label end_label = NextTemporaryLabel;
			instructions.Add(new IntermediateNode.Copy(1.ToIntermediateConstant(), destination));
			instructions.Add(new IntermediateNode.Jump(end_label.Identifier));

			// If false, set result = 0
			instructions.Add(false_label);
			instructions.Add(new IntermediateNode.Copy(0.ToIntermediateConstant(), destination));
			instructions.Add(end_label);

			instructions.Insert(comment_index, new IntermediateNode.Comment($"{destination.ToCommentString()} = {lhs.ToCommentString()} {expression.Operator.GetOperator()} {rhs.ToCommentString()}"));

			return destination;
		}
	
	static IntermediateNode.Variable GenerateLogicalOrExpression(List<IntermediateNode.Instruction> instructions, SymbolTable symbol_table, ParseNode.BinaryExpression expression) {
		int comment_index = instructions.Count;
		
		// If lhs == 1, jump to true
		IntermediateNode.Operand lhs = GenerateExpression(instructions, symbol_table, expression.LeftExpression);
		IntermediateNode.Label true_label = NextTemporaryLabel;
		instructions.Add(new IntermediateNode.JumpIfNotZero(true_label.Identifier, lhs));
		
		// If rhs == 1, jump to true
		IntermediateNode.Operand rhs = GenerateExpression(instructions, symbol_table, expression.RightExpression);
		instructions.Add(new IntermediateNode.JumpIfNotZero(true_label.Identifier, rhs));
		
		// If lhs == rhs == 0, set result = 0, jump to end
		IntermediateNode.Variable destination = NextTemporaryVariable(symbol_table, PrimitiveType.Integer);
		IntermediateNode.Label end_label = NextTemporaryLabel;
		instructions.Add(new IntermediateNode.Copy(0.ToIntermediateConstant(), destination));
		instructions.Add(new IntermediateNode.Jump(end_label.Identifier));
		
		// If true, set result = 1
		instructions.Add(true_label);
		instructions.Add(new IntermediateNode.Copy(1.ToIntermediateConstant(), destination));
		instructions.Add(end_label);
		
		instructions.Insert(comment_index, new IntermediateNode.Comment($"{destination.ToCommentString()} = {lhs.ToCommentString()} {expression.Operator.GetOperator()} {rhs.ToCommentString()}"));
		
		return destination;
	}
}