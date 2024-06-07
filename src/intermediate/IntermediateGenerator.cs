using System.Collections.Generic;
using System.Linq;

namespace FTG.Studios.MCC {
	
	public static class IntermediateGenerator {
		
		static int next_temporary_index;
		
		static IntermediateNode.Variable PreviousTemporaryVariable {
			get { return new IntermediateNode.Variable($"tmp.{next_temporary_index}"); }
		}
		
		static IntermediateNode.Variable NextTemporaryVariable {
			get { return new IntermediateNode.Variable($"tmp.{next_temporary_index++}"); }
		}
		
		public static IntermediateTree Generate(ParseTree tree) {
			next_temporary_index = 0;
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
			GenerateStatement(ref instructions, function.Body);
			return new IntermediateNode.Function(identifier, instructions.ToArray());
		}
		
		static IntermediateNode.Operand GenerateStatement(ref List<IntermediateNode.Instruction> instructions, ParseNode.Statement statement) {
			if (statement is ParseNode.ReturnStatement) return GenerateReturnStatement(ref instructions, statement as ParseNode.ReturnStatement);
			return null;
		}
		
		static IntermediateNode.Operand GenerateReturnStatement(ref List<IntermediateNode.Instruction> instructions, ParseNode.ReturnStatement statement) {
			IntermediateNode.Operand value = GenerateExpression(ref instructions, statement.Expression);
			instructions.Add(new IntermediateNode.Comment($"Return {value}"));
			instructions.Add(new IntermediateNode.ReturnInstruction(value));
			return value;
		}
		
		static IntermediateNode.Operand GenerateExpression(ref List<IntermediateNode.Instruction> instructions, ParseNode.Expression expression) {
			if (expression is ParseNode.ConstantExpression) return GenerateConstantExpression(expression as ParseNode.ConstantExpression);
			if (expression is ParseNode.UnaryExpression) return GenerateUnaryExpression(ref instructions, expression as ParseNode.UnaryExpression);
			if (expression is ParseNode.BinaryExpression) return GenerateBinaryExpression(ref instructions, expression as ParseNode.BinaryExpression);
			return null;
		}
		
		static IntermediateNode.Operand GenerateConstantExpression(ParseNode.ConstantExpression expression) {
			return new IntermediateNode.Constant(expression.Value);
		}
		
		static IntermediateNode.Operand GenerateUnaryExpression(ref List<IntermediateNode.Instruction> instructions, ParseNode.UnaryExpression expression) {
			IntermediateNode.Operand source = GenerateExpression(ref instructions, expression.Expression);
			IntermediateNode.Operand destination = NextTemporaryVariable;
			instructions.Add(new IntermediateNode.Comment($"{destination} = {expression.Operator.GetOperator()} {source}"));
			instructions.Add(new IntermediateNode.UnaryInstruction(expression.Operator, source, destination));
			return destination;
		}
		
		static IntermediateNode.Operand GenerateBinaryExpression(ref List<IntermediateNode.Instruction> instructions, ParseNode.BinaryExpression expression) {
			IntermediateNode.Operand lhs = GenerateExpression(ref instructions, expression.LeftExpression);
			IntermediateNode.Operand rhs = GenerateExpression(ref instructions, expression.RightExpression);
			IntermediateNode.Operand destination = NextTemporaryVariable;
			instructions.Add(new IntermediateNode.Comment($"{destination} = {lhs} {expression.Operator.GetOperator()} {rhs}"));
			instructions.Add(new IntermediateNode.BinaryInstruction(expression.Operator, lhs, rhs, destination));
			return destination;
		}
	}
}