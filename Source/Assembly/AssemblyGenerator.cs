using System;
using System.Collections.Generic;
using System.Linq;

namespace FTG.Studios.MCC {
	
	public static class AssemblyGenerator {
		
		public static AssemblyTree Generate(IntermediateTree tree) {
			AssemblyNode.Program program = GenerateProgram(tree.Program);
			return new AssemblyTree(program);
		}
		
		static AssemblyNode.Program GenerateProgram(IntermediateNode.Program program) {
			List<AssemblyNode.Function> functions = new List<AssemblyNode.Function>();
			foreach (var function in program.Functions) functions.Add(GenerateFunction(function));
			return new AssemblyNode.Program(functions);
		}
		
		static AssemblyNode.Function GenerateFunction(IntermediateNode.Function function) {
			string identifier = function.Identifier;

			AssemblyNode.Operand[] parameters = function.Parameters.Select(p => new AssemblyNode.Variable(p.Identifier)).ToArray();
			
			List<AssemblyNode.Instruction> instructions = new List<AssemblyNode.Instruction>();
			
			// TODO: This may or may not be necessary
			instructions.Add(new AssemblyNode.Comment("align stack"));
			instructions.Add(new AssemblyNode.AllocateStack(8));
			
			instructions.Add(new AssemblyNode.Comment("copy arguments to stack"));
			
			// The first 6 parameters are stored in registers
			for (int i = 0; i < 6 && i < function.Parameters.Length; i++)
			{
				instructions.Add(
					new AssemblyNode.MOV(
						RegisterTypeExtensions.FunctionCallOrder[i].ToOperand(),
						parameters[i]
					)
				);
			}

			// The rest of the parameters are stored in the stack in reverse order
			for (int i = function.Parameters.Length - 1; i >= 6; i--)
			{
				Console.WriteLine(i);
				instructions.Add(
					new AssemblyNode.MOV(
						new AssemblyNode.StackAccess(16 + 8 * (i - 6)),
						parameters[i]
					)
				);
			}
			
			foreach (var instruction in function.Body) GenerateInstruction(ref instructions, instruction);
			
			return new AssemblyNode.Function(identifier, parameters.ToArray(), instructions);
		}

		static void GenerateInstruction(ref List<AssemblyNode.Instruction> instructions, IntermediateNode.Instruction instruction)
		{
			if (instruction is IntermediateNode.Comment) GenerateComment(ref instructions, instruction as IntermediateNode.Comment);
			if (instruction is IntermediateNode.ReturnInstruction) GenerateReturnInstruction(ref instructions, instruction as IntermediateNode.ReturnInstruction);
			if (instruction is IntermediateNode.UnaryInstruction) GenerateUnaryInstruction(ref instructions, instruction as IntermediateNode.UnaryInstruction);
			if (instruction is IntermediateNode.BinaryInstruction) GenerateBinaryInstruction(ref instructions, instruction as IntermediateNode.BinaryInstruction);
			if (instruction is IntermediateNode.Jump) GenerateJumpInstruction(ref instructions, instruction as IntermediateNode.Jump);
			if (instruction is IntermediateNode.JumpIfZero) GenerateJumpIfZeroInstruction(ref instructions, instruction as IntermediateNode.JumpIfZero);
			if (instruction is IntermediateNode.JumpIfNotZero) GenerateJumpIfNotZeroInstruction(ref instructions, instruction as IntermediateNode.JumpIfNotZero);
			if (instruction is IntermediateNode.Copy) GenerateCopyInstruction(ref instructions, instruction as IntermediateNode.Copy);
			if (instruction is IntermediateNode.Label) GenerateLabel(ref instructions, instruction as IntermediateNode.Label);
			if (instruction is IntermediateNode.FunctionCall function_call) GenerateFunctionCall(ref instructions, function_call);
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
			instructions.Add(new AssemblyNode.Unary(instruction.Operator, destination));
		}

		static void GenerateBinaryInstruction(ref List<AssemblyNode.Instruction> instructions, IntermediateNode.BinaryInstruction instruction)
		{
			switch (instruction.Operator)
			{
				case Syntax.BinaryOperator.Addition:
				case Syntax.BinaryOperator.Subtraction:
				case Syntax.BinaryOperator.Multiplication:
					GenerateSimpleBinaryOperation(ref instructions, instruction);
					return;

				case Syntax.BinaryOperator.Division:
					GenerateIntegerDivision(ref instructions, instruction);
					return;

				case Syntax.BinaryOperator.Remainder:
					GenerateIntegerRemainder(ref instructions, instruction);
					return;

				case Syntax.BinaryOperator.LogicalLess:
					GenerateConditional(ref instructions, instruction.LeftOperand, instruction.RightOperand, instruction.Destination, ConditionType.L);
					return;

				case Syntax.BinaryOperator.LogicalGreater:
					GenerateConditional(ref instructions, instruction.LeftOperand, instruction.RightOperand, instruction.Destination, ConditionType.G);
					return;

				case Syntax.BinaryOperator.LogicalLessEqual:
					GenerateConditional(ref instructions, instruction.LeftOperand, instruction.RightOperand, instruction.Destination, ConditionType.LE);
					return;

				case Syntax.BinaryOperator.LogicalGreaterEqual:
					GenerateConditional(ref instructions, instruction.LeftOperand, instruction.RightOperand, instruction.Destination, ConditionType.GE);
					return;

				case Syntax.BinaryOperator.LogicalEqual:
					GenerateConditional(ref instructions, instruction.LeftOperand, instruction.RightOperand, instruction.Destination, ConditionType.E);
					return;
				
				case Syntax.BinaryOperator.LogicalNotEqual:
					GenerateConditional(ref instructions, instruction.LeftOperand, instruction.RightOperand, instruction.Destination, ConditionType.NE);
					return;
			}

			throw new Exception();
		}
		
		static void GenerateSimpleBinaryOperation(ref List<AssemblyNode.Instruction> instructions, IntermediateNode.BinaryInstruction instruction) {
			AssemblyNode.Operand lhs = GenerateOperand(instruction.LeftOperand);
			AssemblyNode.Operand rhs = GenerateOperand(instruction.RightOperand);
			AssemblyNode.Operand destination = GenerateOperand(instruction.Destination);
			
			instructions.Add(new AssemblyNode.MOV(lhs, destination));
			instructions.Add(new AssemblyNode.Binary(instruction.Operator, rhs, destination));
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

		static void GenerateFunctionCall(ref List<AssemblyNode.Instruction> instructions, IntermediateNode.FunctionCall function_call)
		{
			// Ensure stack is 16-byte aligned
			// FIXME: This doesn't work when the stack is already misaligned
			int stack_padding = 0;
			if ((function_call.Arguments.Length - 6) % 2 != 0)
			{
				stack_padding += 8;
				instructions.Add(new AssemblyNode.AllocateStack(8));
			}

			// Frst 6 arguments are copied to registers
			for (int i = 0; i < 6 && i < function_call.Arguments.Length; i++)
			{
				instructions.Add(
					new AssemblyNode.MOV(
						GenerateOperand(function_call.Arguments[i]),
						RegisterTypeExtensions.FunctionCallOrder[i].ToOperand()
					)
				);
			}

			// The rest of the arguments are pushed to the stack in reverse order
			for (int i = function_call.Arguments.Length - 1; i >= 6; i--)
			{
				stack_padding += 8;
				AssemblyNode.Operand operand = GenerateOperand(function_call.Arguments[i]);
				if (operand is AssemblyNode.Register || operand is AssemblyNode.Immediate)
				{
					instructions.Add(new AssemblyNode.Push(operand));
				}
				else
				{
					instructions.Add(new AssemblyNode.MOV(operand, RegisterType.AX.ToOperand()));
					instructions.Add(new AssemblyNode.Push(RegisterType.AX.ToOperand()));
				}
			}

			instructions.Add(new AssemblyNode.Call(function_call.Identifier));

			// Restore stack pointer
			if (stack_padding != 0) instructions.Add(new AssemblyNode.DeallocateStack(stack_padding));

			// Retrieve return value
			instructions.Add(
				new AssemblyNode.MOV(RegisterType.AX.ToOperand(),
				GenerateOperand(function_call.Destination))
			);
		}
		
		static AssemblyNode.Operand GenerateOperand(IntermediateNode.Operand operand)
		{
			if (operand is IntermediateNode.Constant constant) return GenerateConstant(constant);
			if (operand is IntermediateNode.Variable variable) return GenerateVariable(variable);
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