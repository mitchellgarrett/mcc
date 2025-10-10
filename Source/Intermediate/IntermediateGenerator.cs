using System;
using System.Collections.Generic;
using System.Linq;
using FTG.Studios.MCC.Lexer;
using FTG.Studios.MCC.Parser;
using FTG.Studios.MCC.SemanticAnalysis;

namespace FTG.Studios.MCC.Intermediate;

public static class IntermediateGenerator {
	
	static int next_temporary_variable_index;
	static IntermediateNode.Variable NextTemporaryVariable {
		get { return new IntermediateNode.Variable($"tmp.{next_temporary_variable_index++}"); }
	}
	
	static int next_temporary_label_index;
	static IntermediateNode.Label NextTemporaryLabel {
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
				IntermediateNode.Function intermediate_function = GenerateFunctionDeclaration(function_declaration, symbol_table);
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
				if (static_attributes.InitialValue is InitialValue.Constant constant)
					static_variables.Add(new IntermediateNode.StaticVariable(identifier, static_attributes.IsGlobal, constant.Value));
				else if (static_attributes.InitialValue is InitialValue.Tentative)
					static_variables.Add(new IntermediateNode.StaticVariable(identifier, static_attributes.IsGlobal, 0));
				// Ignore variables with no initializer
			}
		}

		return static_variables;
	}
	
	static IntermediateNode.Function GenerateFunctionDeclaration(ParseNode.FunctionDeclaration function, SymbolTable symbol_table)
	{
		// If the function has no body, there is nothing to generate
		if (function.Body == null) return null;

		string identifier = function.Identifier.Value;

		// Convert list of ParseNode.Identifiers to array of strings
		IntermediateNode.Variable[] parameters = function.Parameters.Select(p => new IntermediateNode.Variable(p.Value)).ToArray();

		// Generate instructions for the body of the function
		List<IntermediateNode.Instruction> instructions = [];
		GenerateBlock(ref instructions, function.Body);

		// Add Return(0) to the end of every function in case there is no explicit return given
		GenerateReturnStatement(ref instructions, new ParseNode.Return(new ParseNode.Constant(0)));

		// Pull the function's globality from the symbol table since it might have been definied with a different globality earlier
		if (!symbol_table.TryGetSymbol(function.Identifier.Value, out SymbolTableEntry entry)) throw new IntermediateGeneratorException($"Function \"{function.Identifier.Value}\" does not exist in symbol table.", null, null, null);
		bool is_global = (entry.Attributes as IdentifierAttributes.Function).IsGlobal;
		
		return new IntermediateNode.Function(identifier, is_global, parameters, instructions.ToArray());
	}

	static void GenerateBlock(ref List<IntermediateNode.Instruction> instructions, ParseNode.Block block)
	{
		foreach (ParseNode.BlockItem item in block.Items) GenerateBlockItem(ref instructions, item);
	}

	static void GenerateBlockItem(ref List<IntermediateNode.Instruction> instructions, ParseNode.BlockItem item)
	{
		if (item is ParseNode.VariableDeclaration declaration) GenerateVariableDeclaration(ref instructions, declaration);
		if (item is ParseNode.Statement statement) GenerateStatement(ref instructions, statement);
	}

	static void GenerateVariableDeclaration(ref List<IntermediateNode.Instruction> instructions, ParseNode.VariableDeclaration declaration)
	{
		// Do not generate initializers for static variables
		if (declaration.StorageClass == ParseNode.StorageClass.Static) return;
		
		// Generate declaration initializer, if it exists
		IntermediateNode.Operand source = null;
		if (declaration.Source != null) source = GenerateExpression(ref instructions, declaration.Source);
		
		IntermediateNode.Operand destination = new IntermediateNode.Variable(declaration.Identifier.Value);

		// Copy initialization value to destination
		if (source != null)
		{
			instructions.Add(new IntermediateNode.Comment($"{destination.ToCommentString()} = {source.ToCommentString()}"));
			instructions.Add(new IntermediateNode.Copy(source, destination));
		}
	}

	static void GenerateStatement(ref List<IntermediateNode.Instruction> instructions, ParseNode.Statement statement)
	{
		if (statement is null) return;
		if (statement is ParseNode.Return return_statement)
		{
			GenerateReturnStatement(ref instructions, return_statement);
			return;
		}
		if (statement is ParseNode.If if_statement)
		{
			GenerateIfStatement(ref instructions, if_statement);
			return;
		}
		if (statement is ParseNode.Block block)
		{
			GenerateBlock(ref instructions, block);
			return;
		}
		if (statement is ParseNode.Expression expression)
		{
			GenerateExpression(ref instructions, expression);
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
			GenerateWhileLoop(ref instructions, @while);
			return;
		}
		if (statement is ParseNode.DoWhile do_while)
		{
			GenerateDoWhileLoop(ref instructions, do_while);
			return;
		}
		if (statement is ParseNode.For @for)
		{
			GenerateForLoop(ref instructions, @for);
			return;
		}
		
		throw new IntermediateGeneratorException("GenerateStatement", statement.GetType(), statement, typeof(Nullable), typeof(ParseNode.Return), typeof(ParseNode.Expression), typeof(ParseNode.Break), typeof(ParseNode.Continue), typeof(ParseNode.While), typeof(ParseNode.DoWhile), typeof(ParseNode.For));
	}
	
	static void GenerateReturnStatement(ref List<IntermediateNode.Instruction> instructions, ParseNode.Return statement) {
		IntermediateNode.Operand value = GenerateExpression(ref instructions, statement.Expression);
		instructions.Add(new IntermediateNode.Comment($"return {value.ToCommentString()}"));
		instructions.Add(new IntermediateNode.Return(value));
	}

	static void GenerateIfStatement(ref List<IntermediateNode.Instruction> instructions, ParseNode.If statement)
	{
		if (statement.Else == null)
		{
			GenerateIfStatementWithoutElse(ref instructions, statement);
			return;
		}
		
		GenerateIfStatementWithElse(ref instructions, statement);
	}

	static void GenerateIfStatementWithoutElse(ref List<IntermediateNode.Instruction> instructions, ParseNode.If statement)
	{
		int comment_index = instructions.Count;
		IntermediateNode.Operand condition = GenerateExpression(ref instructions, statement.Condition);
		instructions.Insert(comment_index, new IntermediateNode.Comment($"compare {condition.ToCommentString()}"));

		IntermediateNode.Label end_label = NextTemporaryLabel;
		instructions.Add(new IntermediateNode.JumpIfZero(end_label.Identifier, condition));
		
		instructions.Add(new IntermediateNode.Comment("then"));
		GenerateStatement(ref instructions, statement.Then);
		instructions.Add(end_label);
	}

	static void GenerateIfStatementWithElse(ref List<IntermediateNode.Instruction> instructions, ParseNode.If statement)
	{
		int comment_index = instructions.Count;
		IntermediateNode.Operand condition = GenerateExpression(ref instructions, statement.Condition);
		instructions.Insert(comment_index, new IntermediateNode.Comment($"compare {condition.ToCommentString()}"));

		IntermediateNode.Label else_label = NextTemporaryLabel;
		instructions.Add(new IntermediateNode.JumpIfZero(else_label.Identifier, condition));
		
		instructions.Add(new IntermediateNode.Comment("then"));
		GenerateStatement(ref instructions, statement.Then);
		IntermediateNode.Label end_label = NextTemporaryLabel;
		instructions.Add(new IntermediateNode.Jump(end_label.Identifier));
		
		instructions.Add(new IntermediateNode.Comment("otherwise"));
		instructions.Add(else_label);
		GenerateStatement(ref instructions, statement.Else);
		instructions.Add(end_label);

	}

	static void GenerateWhileLoop(ref List<IntermediateNode.Instruction> instructions, ParseNode.While statement)
	{
		instructions.Add(new IntermediateNode.Label($"Continue{statement.InternalLabel}"));
		IntermediateNode.Operand value = GenerateExpression(ref instructions, statement.Condition);
		instructions.Add(new IntermediateNode.JumpIfZero($"Break{statement.InternalLabel}", value));
		GenerateStatement(ref instructions, statement.Body);
		instructions.Add(new IntermediateNode.Jump($"Continue{statement.InternalLabel}"));
		instructions.Add(new IntermediateNode.Label($"Break{statement.InternalLabel}"));
	}
	
	static void GenerateDoWhileLoop(ref List<IntermediateNode.Instruction> instructions, ParseNode.DoWhile statement)
	{
		instructions.Add(new IntermediateNode.Label($"Start{statement.InternalLabel}"));
		GenerateStatement(ref instructions, statement.Body);
		instructions.Add(new IntermediateNode.Label($"Continue{statement.InternalLabel}"));
		IntermediateNode.Operand value = GenerateExpression(ref instructions, statement.Condition);
		instructions.Add(new IntermediateNode.JumpIfNotZero($"Start{statement.InternalLabel}", value));
		instructions.Add(new IntermediateNode.Label($"Break{statement.InternalLabel}"));
	}

	static void GenerateForLoop(ref List<IntermediateNode.Instruction> instructions, ParseNode.For statement)
	{
		if (statement.Initialization is ParseNode.VariableDeclaration init_declaration) GenerateVariableDeclaration(ref instructions, init_declaration);
		else if (statement.Initialization is ParseNode.Expression init_expression) GenerateExpression(ref instructions, init_expression);
		else if (statement.Initialization is null) { }
		else throw new IntermediateGeneratorException("", null, null, null);

		instructions.Add(new IntermediateNode.Label($"Start{statement.InternalLabel}"));


		if (statement.Condition != null)
		{
			IntermediateNode.Operand value = GenerateExpression(ref instructions, statement.Condition);
			instructions.Add(new IntermediateNode.JumpIfZero($"Break{statement.InternalLabel}", value));
		}
		
		GenerateStatement(ref instructions, statement.Body);
		instructions.Add(new IntermediateNode.Label($"Continue{statement.InternalLabel}"));
		
		if (statement.Post != null) GenerateExpression(ref instructions, statement.Post);
		
		instructions.Add(new IntermediateNode.Jump($"Start{statement.InternalLabel}"));
		instructions.Add(new IntermediateNode.Label($"Break{statement.InternalLabel}"));
	}
	
	static IntermediateNode.Operand GenerateExpression(ref List<IntermediateNode.Instruction> instructions, ParseNode.Expression expression)
		{
			if (expression is ParseNode.Factor factor) return GenerateFactor(ref instructions, factor);
			if (expression is ParseNode.BinaryExpression binary) return GenerateBinaryExpression(ref instructions, binary);
			if (expression is ParseNode.Assignment assignment) return GenerateAssignment(ref instructions, assignment);
			if (expression is ParseNode.Conditional conditional) return GenerateConditional(ref instructions, conditional);
			if (expression is ParseNode.FunctionCall function_call) return GenerateFunctionCall(ref instructions, function_call);

			throw new IntermediateGeneratorException("GenerateExpression", expression.GetType(), expression, typeof(ParseNode.Factor), typeof(ParseNode.BinaryExpression));
		}

	static IntermediateNode.Operand GenerateFactor(ref List<IntermediateNode.Instruction> instructions, ParseNode.Factor factor)
	{
		if (factor is ParseNode.UnaryExpression unaryExpression) return GenerateUnaryExpression(ref instructions, unaryExpression);
		if (factor is ParseNode.Constant constant) return new IntermediateNode.Constant(constant.Value);
		if (factor is ParseNode.Variable variable) return new IntermediateNode.Variable(variable.Identifier.Value);
		
		throw new IntermediateGeneratorException("GenerateFactor", factor.GetType(), factor, typeof(ParseNode.UnaryExpression), typeof(ParseNode.Constant), typeof(ParseNode.Variable));
	}
	
	static IntermediateNode.Operand GenerateUnaryExpression(ref List<IntermediateNode.Instruction> instructions, ParseNode.UnaryExpression expression)
	{
		IntermediateNode.Operand source = GenerateExpression(ref instructions, expression.Expression);
		IntermediateNode.Operand destination = NextTemporaryVariable;
		instructions.Add(new IntermediateNode.Comment($"{destination.ToCommentString()} = {expression.Operator.GetOperator()} {source.ToCommentString()}"));
		instructions.Add(new IntermediateNode.UnaryInstruction(expression.Operator, source, destination));
		return destination;
	}
	
	static IntermediateNode.Operand GenerateBinaryExpression(ref List<IntermediateNode.Instruction> instructions, ParseNode.BinaryExpression expression) {
		if (expression.Operator == Syntax.BinaryOperator.LogicalAnd) return GenerateLogicalAndExpression(ref instructions, expression);
		if (expression.Operator == Syntax.BinaryOperator.LogicalOr) return GenerateLogicalOrExpression(ref instructions, expression);
		
		IntermediateNode.Operand lhs = GenerateExpression(ref instructions, expression.LeftExpression);
		IntermediateNode.Operand rhs = GenerateExpression(ref instructions, expression.RightExpression);
		IntermediateNode.Operand destination = NextTemporaryVariable;
		instructions.Add(new IntermediateNode.Comment($"{destination.ToCommentString()} = {lhs.ToCommentString()} {expression.Operator.GetOperator()} {rhs.ToCommentString()}"));
		instructions.Add(new IntermediateNode.BinaryInstruction(expression.Operator, lhs, rhs, destination));
		
		return destination;
	}
	
	static IntermediateNode.Operand GenerateAssignment(ref List<IntermediateNode.Instruction> instructions, ParseNode.Assignment assignment) {
		IntermediateNode.Operand source = GenerateExpression(ref instructions, assignment.Source);
		IntermediateNode.Operand destination = GenerateExpression(ref instructions, assignment.Destination);
		
		instructions.Add(new IntermediateNode.Comment($"{destination.ToCommentString()} = {source.ToCommentString()}"));
		instructions.Add(new IntermediateNode.Copy(source, destination));

		return destination;
	}

	static IntermediateNode.Operand GenerateConditional(ref List<IntermediateNode.Instruction> instructions, ParseNode.Conditional conditional)
	{
		int comment_index = instructions.Count;
		IntermediateNode.Operand condition = GenerateExpression(ref instructions, conditional.Condition);
		instructions.Insert(comment_index, new IntermediateNode.Comment($"compare {condition.ToCommentString()}"));

		IntermediateNode.Label else_label = NextTemporaryLabel;
		instructions.Add(new IntermediateNode.JumpIfZero(else_label.Identifier, condition));

		instructions.Add(new IntermediateNode.Comment("then"));
		IntermediateNode.Operand then_value = GenerateExpression(ref instructions, conditional.Then);
		IntermediateNode.Variable result = NextTemporaryVariable;
		instructions.Add(new IntermediateNode.Copy(then_value, result));
		IntermediateNode.Label end_label = NextTemporaryLabel;
		instructions.Add(new IntermediateNode.Jump(end_label.Identifier));
		
		instructions.Add(new IntermediateNode.Comment("otherwise"));
		instructions.Add(else_label);
		IntermediateNode.Operand else_value = GenerateExpression(ref instructions, conditional.Else);
		instructions.Add(new IntermediateNode.Copy(else_value, result));
		instructions.Add(end_label);

		return result;
	}

	static IntermediateNode.Operand GenerateFunctionCall(ref List<IntermediateNode.Instruction> instructions, ParseNode.FunctionCall function_call)
	{
		instructions.Add(new IntermediateNode.Comment($"call {function_call.Identifier.Value}"));
		
		List<IntermediateNode.Operand> arguments = new List<IntermediateNode.Operand>();
		foreach (var argument in function_call.Arguments)
		{
			IntermediateNode.Operand argument_value = GenerateExpression(ref instructions, argument);
			arguments.Add(argument_value);
		}

		IntermediateNode.Operand destination = NextTemporaryVariable;
		instructions.Add(new IntermediateNode.FunctionCall(function_call.Identifier.Value, arguments.ToArray(), destination));

		return destination;
	}
	
	static IntermediateNode.Operand GenerateLogicalAndExpression(ref List<IntermediateNode.Instruction> instructions, ParseNode.BinaryExpression expression)
		{
			int comment_index = instructions.Count;

			// If lhs == 0, jump to false
			IntermediateNode.Operand lhs = GenerateExpression(ref instructions, expression.LeftExpression);
			IntermediateNode.Label false_label = NextTemporaryLabel;
			instructions.Add(new IntermediateNode.JumpIfZero(false_label.Identifier, lhs));

			// If rhs == 0, jump to false
			IntermediateNode.Operand rhs = GenerateExpression(ref instructions, expression.RightExpression);
			instructions.Add(new IntermediateNode.JumpIfZero(false_label.Identifier, rhs));

			// If lhs == rhs == 1, set result = 1, jump to end
			IntermediateNode.Operand destination = NextTemporaryVariable;
			IntermediateNode.Label end_label = NextTemporaryLabel;
			instructions.Add(new IntermediateNode.Copy(new IntermediateNode.Constant(1), destination));
			instructions.Add(new IntermediateNode.Jump(end_label.Identifier));

			// If false, set result = 0
			instructions.Add(false_label);
			instructions.Add(new IntermediateNode.Copy(new IntermediateNode.Constant(0), destination));
			instructions.Add(end_label);

			instructions.Insert(comment_index, new IntermediateNode.Comment($"{destination.ToCommentString()} = {lhs.ToCommentString()} {expression.Operator.GetOperator()} {rhs.ToCommentString()}"));

			return destination;
		}
	
	static IntermediateNode.Operand GenerateLogicalOrExpression(ref List<IntermediateNode.Instruction> instructions, ParseNode.BinaryExpression expression) {
		int comment_index = instructions.Count;
		
		// If lhs == 1, jump to true
		IntermediateNode.Operand lhs = GenerateExpression(ref instructions, expression.LeftExpression);
		IntermediateNode.Label true_label = NextTemporaryLabel;
		instructions.Add(new IntermediateNode.JumpIfNotZero(true_label.Identifier, lhs));
		
		// If rhs == 1, jump to true
		IntermediateNode.Operand rhs = GenerateExpression(ref instructions, expression.RightExpression);
		instructions.Add(new IntermediateNode.JumpIfNotZero(true_label.Identifier, rhs));
		
		// If lhs == rhs == 0, set result = 0, jump to end
		IntermediateNode.Operand destination = NextTemporaryVariable;
		IntermediateNode.Label end_label = NextTemporaryLabel;
		instructions.Add(new IntermediateNode.Copy(new IntermediateNode.Constant(0), destination));
		instructions.Add(new IntermediateNode.Jump(end_label.Identifier));
		
		// If true, set result = 1
		instructions.Add(true_label);
		instructions.Add(new IntermediateNode.Copy(new IntermediateNode.Constant(1), destination));
		instructions.Add(end_label);
		
		instructions.Insert(comment_index, new IntermediateNode.Comment($"{destination.ToCommentString()} = {lhs.ToCommentString()} {expression.Operator.GetOperator()} {rhs.ToCommentString()}"));
		
		return destination;
	}
}