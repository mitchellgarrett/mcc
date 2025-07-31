using System;
using System.Collections.Generic;

namespace FTG.Studios.MCC {
	
	public static class IntermediateGenerator {
		
		static int next_temporary_variable_index;
		static IntermediateNode.Variable NextTemporaryVariable {
			get { return new IntermediateNode.Variable($"tmp.{next_temporary_variable_index++}"); }
		}
		
		static int next_temporary_label_index;
		static IntermediateNode.Label NextTemporaryLabel {
			get { return new IntermediateNode.Label($"L{next_temporary_label_index++}"); }
		}
		
		public static IntermediateTree Generate(ParseTree tree) {
			next_temporary_variable_index = 0;
			next_temporary_label_index = 0;
			IntermediateNode.Program program = GenerateProgram(tree.Program);
			return new IntermediateTree(program);
		}
		
		static IntermediateNode.Program GenerateProgram(ParseNode.Program program) {
			IntermediateNode.Function function = GenerateFunction(program.Function);
			return new IntermediateNode.Program(function);
		}
		
		static IntermediateNode.Function GenerateFunction(ParseNode.Function function) {
			string identifier = function.Identifier.Value;
			List<IntermediateNode.Instruction> instructions = new List<IntermediateNode.Instruction>();
			foreach (ParseNode.BlockItem item in function.Body)
			{
				GenerateBlockItem(ref instructions, item);
			}

			// Add Return(0) to the end of every function in case there is no explicit return given
			GenerateReturnStatement(ref instructions, new ParseNode.ReturnStatement(new ParseNode.Constant(0)));
			
			return new IntermediateNode.Function(identifier, instructions.ToArray());
		}

		static void GenerateBlockItem(ref List<IntermediateNode.Instruction> instructions, ParseNode.BlockItem item)
		{
			if (item is ParseNode.Declaration declaration) GenerateDeclaration(ref instructions, declaration);
			if (item is ParseNode.Statement statement) GenerateStatement(ref instructions, statement);
		}

		static void GenerateDeclaration(ref List<IntermediateNode.Instruction> instructions, ParseNode.Declaration declaration)
		{
			// If declaration initializer, if it exists
			IntermediateNode.Operand source = null;
			if (declaration.Source != null) source = GenerateExpression(ref instructions, declaration.Source);
			
			IntermediateNode.Operand destination = new IntermediateNode.Variable(declaration.Identifier.Value);
			
			// Copy initialization value to destination
			if (source != null) instructions.Add(new IntermediateNode.Copy(source, destination));
		}

		static void GenerateStatement(ref List<IntermediateNode.Instruction> instructions, ParseNode.Statement statement)
		{
			if (statement is null) return;
			if (statement is ParseNode.ReturnStatement return_statement)
			{
				GenerateReturnStatement(ref instructions, return_statement);
				return;
			}
			if (statement is ParseNode.Expression expression)
			{
				GenerateExpression(ref instructions, expression);
				return;
			}
			
			throw new IntermediateGeneratorException("GenerateStatement", statement.GetType(), statement, typeof(Nullable), typeof(ParseNode.ReturnStatement), typeof(ParseNode.Expression));
		}
		
		static void GenerateReturnStatement(ref List<IntermediateNode.Instruction> instructions, ParseNode.ReturnStatement statement) {
			IntermediateNode.Operand value = GenerateExpression(ref instructions, statement.Expression);
			instructions.Add(new IntermediateNode.Comment($"return {value.ToCommentString()}"));
			instructions.Add(new IntermediateNode.ReturnInstruction(value));
		}
		
		static IntermediateNode.Operand GenerateExpression(ref List<IntermediateNode.Instruction> instructions, ParseNode.Expression expression) {
			if (expression is ParseNode.Factor factor) return GenerateFactor(ref instructions, factor);
			if (expression is ParseNode.BinaryExpression binary) return GenerateBinaryExpression(ref instructions, binary);
			if (expression is ParseNode.Assignment assignment) return GenerateAssignment(ref instructions, assignment);
			
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
}