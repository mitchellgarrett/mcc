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
			if (instruction is IntermediateNode.ReturnInstruction) GenerateReturnInstruction(ref instructions, instruction as IntermediateNode.ReturnInstruction);
			if (instruction is IntermediateNode.UnaryInstruction) GenerateUnaryInstruction(ref instructions, instruction as IntermediateNode.UnaryInstruction);
		}
		
		static void GenerateReturnInstruction(ref List<AssemblyNode.Instruction> instructions, IntermediateNode.ReturnInstruction instruction) {
			AssemblyNode.Operand source = GenerateOperand(instruction.Value);
			AssemblyNode.Operand destination = new AssemblyNode.Register(RegisterType.AX);
			
			instructions.Add(new AssemblyNode.MOV(source, destination));
			instructions.Add(new AssemblyNode.RET());
		}
		
		static void GenerateUnaryInstruction(ref List<AssemblyNode.Instruction> instructions, IntermediateNode.UnaryInstruction instruction) {
			AssemblyNode.Operand source = GenerateOperand(instruction.Source);
			AssemblyNode.Operand destination = GenerateOperand(instruction.Destination);
			
			instructions.Add(new AssemblyNode.MOV(source, destination));
			instructions.Add(new AssemblyNode.UnaryInstruction(instruction.Operator, destination));
		}
		
		static AssemblyNode.Operand GenerateOperand(IntermediateNode.Operand expression) {
			if (expression is IntermediateNode.Constant) return GenerateConstant(expression as IntermediateNode.Constant);
			if (expression is IntermediateNode.Variable) return GenerateVariable(expression as IntermediateNode.Variable);
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