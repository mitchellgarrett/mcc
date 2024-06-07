using System;
using System.Collections.Generic;

namespace FTG.Studios.MCC {
	
	public static class CodeGenerator {
		
		public static AssemblyTree Generate(IntermediateTree tree) {
			AssemblyNode.Program program = GenerateProgram(tree.Program);
			return new AssemblyTree(program);
		}
		
		static AssemblyNode.Program GenerateProgram(IntermediateNode.Program program) {
			AssemblyNode.Function function = GenerateFunction(program.Function);
			return new AssemblyNode.Program(function);
		}
		
		static AssemblyNode.Function GenerateFunction(IntermediateNode.Function function) {
			string identifier = function.Identifier;
			List<AssemblyNode.Instruction> instructions = new List<AssemblyNode.Instruction>();
			foreach (var instruction in function.Body) {
				GenerateInstruction(ref instructions, instruction);
			}
			return new AssemblyNode.Function(identifier, instructions);
		}
		
		static void GenerateInstruction(ref List<AssemblyNode.Instruction> instructions, IntermediateNode.Instruction instruction) {
			if (instruction is IntermediateNode.Comment) GenerateComment(ref instructions, instruction as IntermediateNode.Comment);
			if (instruction is IntermediateNode.ReturnInstruction) GenerateReturnInstruction(ref instructions, instruction as IntermediateNode.ReturnInstruction);
			if (instruction is IntermediateNode.UnaryInstruction) GenerateUnaryInstruction(ref instructions, instruction as IntermediateNode.UnaryInstruction);
			if (instruction is IntermediateNode.BinaryInstruction) GenerateBinaryInstruction(ref instructions, instruction as IntermediateNode.BinaryInstruction);
			if (instruction is IntermediateNode.Jump) GenerateJumpInstruction(ref instructions, instruction as IntermediateNode.Jump);
			if (instruction is IntermediateNode.JumpIfZero) GenerateJumpIfZeroInstruction(ref instructions, instruction as IntermediateNode.JumpIfZero);
			if (instruction is IntermediateNode.JumpIfNotZero) GenerateJumpIfNotZeroInstruction(ref instructions, instruction as IntermediateNode.JumpIfNotZero);
			if (instruction is IntermediateNode.Copy) GenerateCopyInstruction(ref instructions, instruction as IntermediateNode.Copy);
			if (instruction is IntermediateNode.Label) GenerateLabel(ref instructions, instruction as IntermediateNode.Label);
		}
		
		static void GenerateComment(ref List<AssemblyNode.Instruction> instructions, IntermediateNode.Comment comment) {
			instructions.Add(new AssemblyNode.Comment(comment.Data));
		}
		
		static void GenerateReturnInstruction(ref List<AssemblyNode.Instruction> instructions, IntermediateNode.ReturnInstruction instruction) {
			AssemblyNode.Operand source = GenerateOperand(instruction.Value);
			AssemblyNode.Operand destination = RegisterType.AX.ToOperand();
			
			instructions.Add(new AssemblyNode.MOV(source, destination));
			instructions.Add(new AssemblyNode.RET());
		}
		
		static void GenerateUnaryInstruction(ref List<AssemblyNode.Instruction> instructions, IntermediateNode.UnaryInstruction instruction) {
			if (instruction.Operator == Syntax.UnaryOperator.Not) {
				GenerateConditional(ref instructions, 0.ToIntermediateConstant(), instruction.Source, instruction.Destination, ConditionType.E);
				return;
			}
			
			AssemblyNode.Operand source = GenerateOperand(instruction.Source);
			AssemblyNode.Operand destination = GenerateOperand(instruction.Destination);
			
			instructions.Add(new AssemblyNode.MOV(source, destination));
			instructions.Add(new AssemblyNode.UnaryInstruction(instruction.Operator, destination));
		}
		
		static void GenerateBinaryInstruction(ref List<AssemblyNode.Instruction> instructions, IntermediateNode.BinaryInstruction instruction) {
			switch (instruction.Operator)
			{
				case Syntax.BinaryOperator.Addition: 
				case Syntax.BinaryOperator.Subtraction: 
				case Syntax.BinaryOperator.Multiplication: 
					GenerateSimpleBinaryOperation(ref instructions, instruction);
					break;
					
				case Syntax.BinaryOperator.Division: 
					GenerateIntegerDivision(ref instructions, instruction);
					break;
					
				case Syntax.BinaryOperator.Remainder: 
					GenerateIntegerRemainder(ref instructions, instruction);
					break;
				
				case Syntax.BinaryOperator.LogicalLessThan: 
					GenerateConditional(ref instructions, instruction.LeftOperand, instruction.RightOperand, instruction.Destination, ConditionType.L);
					break;
				
				case Syntax.BinaryOperator.LogicalGreatherThan: 
					GenerateConditional(ref instructions, instruction.LeftOperand, instruction.RightOperand, instruction.Destination, ConditionType.G);
					break;
					
				case Syntax.BinaryOperator.LogicalLessThanEqualTo: 
					GenerateConditional(ref instructions, instruction.LeftOperand, instruction.RightOperand, instruction.Destination, ConditionType.GE);
					break;
					
				case Syntax.BinaryOperator.LogicalGreatherThanEqualTo: 
					GenerateConditional(ref instructions, instruction.LeftOperand, instruction.RightOperand, instruction.Destination, ConditionType.LE);
					break;
			}
		}
		
		static void GenerateSimpleBinaryOperation(ref List<AssemblyNode.Instruction> instructions, IntermediateNode.BinaryInstruction instruction) {
			AssemblyNode.Operand lhs = GenerateOperand(instruction.LeftOperand);
			AssemblyNode.Operand rhs = GenerateOperand(instruction.RightOperand);
			AssemblyNode.Operand destination = GenerateOperand(instruction.Destination);
			
			instructions.Add(new AssemblyNode.MOV(lhs, destination));
			instructions.Add(new AssemblyNode.BinaryInstruction(instruction.Operator, rhs, destination));
		}
		
		static void GenerateIntegerDivision(ref List<AssemblyNode.Instruction> instructions, IntermediateNode.BinaryInstruction instruction) {
			AssemblyNode.Operand lhs = GenerateOperand(instruction.LeftOperand);
			AssemblyNode.Operand rhs = GenerateOperand(instruction.RightOperand);
			AssemblyNode.Operand destination = GenerateOperand(instruction.Destination);
			
			instructions.Add(new AssemblyNode.MOV(lhs, RegisterType.AX.ToOperand()));
			instructions.Add(new AssemblyNode.CDQ());
			instructions.Add(new AssemblyNode.IDIV(rhs));
			instructions.Add(new AssemblyNode.MOV(RegisterType.AX.ToOperand(), destination));
		}
		
		static void GenerateIntegerRemainder(ref List<AssemblyNode.Instruction> instructions, IntermediateNode.BinaryInstruction instruction) {
			AssemblyNode.Operand lhs = GenerateOperand(instruction.LeftOperand);
			AssemblyNode.Operand rhs = GenerateOperand(instruction.RightOperand);
			AssemblyNode.Operand destination = GenerateOperand(instruction.Destination);
			
			instructions.Add(new AssemblyNode.MOV(lhs, RegisterType.AX.ToOperand()));
			instructions.Add(new AssemblyNode.CDQ());
			instructions.Add(new AssemblyNode.IDIV(rhs));
			instructions.Add(new AssemblyNode.MOV(RegisterType.DX.ToOperand(), destination));
		}
		
		static void GenerateConditional(ref List<AssemblyNode.Instruction> instructions, IntermediateNode.Operand left_operand, IntermediateNode.Operand right_operand, IntermediateNode.Operand destination, ConditionType condition) {
			AssemblyNode.Operand assembly_destination = GenerateOperand(destination);
			instructions.Add(new AssemblyNode.CMP(GenerateOperand(right_operand), GenerateOperand(left_operand)));
			instructions.Add(new AssemblyNode.MOV(0.ToAssemblyImmediate(), assembly_destination));
			instructions.Add(new AssemblyNode.SETCC(assembly_destination, condition));
		}
		
		static void GenerateJumpInstruction(ref List<AssemblyNode.Instruction> instructions, IntermediateNode.Jump instruction) {
			instructions.Add(new AssemblyNode.JMP(instruction.Target));
		}
		
		static void GenerateJumpIfZeroInstruction(ref List<AssemblyNode.Instruction> instructions, IntermediateNode.JumpIfZero instruction) {
			instructions.Add(new AssemblyNode.CMP(0.ToAssemblyImmediate(), GenerateOperand(instruction.Condition)));
			instructions.Add(new AssemblyNode.JMPCC(instruction.Target, ConditionType.E));
		}
		
		static void GenerateJumpIfNotZeroInstruction(ref List<AssemblyNode.Instruction> instructions, IntermediateNode.JumpIfNotZero instruction) {
			instructions.Add(new AssemblyNode.CMP(0.ToAssemblyImmediate(), GenerateOperand(instruction.Condition)));
			instructions.Add(new AssemblyNode.JMPCC(instruction.Target, ConditionType.NE));
		}
		
		static void GenerateCopyInstruction(ref List<AssemblyNode.Instruction> instructions, IntermediateNode.Copy instruction) {
			instructions.Add(new AssemblyNode.MOV(GenerateOperand(instruction.Source), GenerateOperand(instruction.Destination)));
		}
		
		static void GenerateLabel(ref List<AssemblyNode.Instruction> instructions, IntermediateNode.Label instruction) {
			instructions.Add(new AssemblyNode.Label(instruction.Identifier));
		}
		
		static AssemblyNode.Operand GenerateOperand(IntermediateNode.Operand expression) {
			Console.WriteLine(expression);
			Console.WriteLine(expression.GetType());
			if (expression is IntermediateNode.Constant) return GenerateConstant(expression as IntermediateNode.Constant);
			if (expression is IntermediateNode.Variable) return GenerateVariable(expression as IntermediateNode.Variable);
			System.Environment.Exit(1);
			return null;
		}
		
		static AssemblyNode.Operand GenerateConstant(IntermediateNode.Constant operand) {
			return new AssemblyNode.Immediate(operand.Value);
		}
		
		static AssemblyNode.Operand GenerateVariable(IntermediateNode.Variable operand) {
			return new AssemblyNode.Variable(operand.Identifier);
		}
	}
}