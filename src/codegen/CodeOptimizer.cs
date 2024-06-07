using System.Collections.Generic;

namespace FTG.Studios.MCC {
	
	public static class CodeOptimizer {
		
		static int stack_offset;
		static Dictionary<string, int> variable_stack_offsets = new Dictionary<string, int>();
		
		static AssemblyNode.StackAccess GetStackOffset(string identifier) {
			if (variable_stack_offsets.TryGetValue(identifier, out int offset)) return new AssemblyNode.StackAccess(offset);
			stack_offset -= 4;
			variable_stack_offsets[identifier] = stack_offset;
			return new AssemblyNode.StackAccess(stack_offset);
		}
		
		public static void AssignVariables(AssemblyTree tree) {
			AssemblyNode.Function function = tree.Program.Function;
			AssignVariablesFunction(function);
		}
		
		public static void AssignVariablesFunction(AssemblyNode.Function function) {
			stack_offset = 0;
			variable_stack_offsets.Clear();
			
			foreach (var instruction in function.Body) {
				if (instruction is AssemblyNode.MOV) AssignVariablesMOV(instruction as AssemblyNode.MOV);
				if (instruction is AssemblyNode.UnaryInstruction) AssignVariablesUnaryInstruction(instruction as AssemblyNode.UnaryInstruction);
				if (instruction is AssemblyNode.BinaryInstruction) AssignVariablesBinaryInstruction(instruction as AssemblyNode.BinaryInstruction);
			}
			
			int space_to_allocate = System.Math.Abs(stack_offset);
			if (space_to_allocate > 0) {
				function.Body.Insert(0, new AssemblyNode.AllocateStackInstruction(space_to_allocate));
				function.Body.Insert(0, new AssemblyNode.Comment($"Allocate {space_to_allocate} bytes"));
			}
		}
		
		static void AssignVariablesMOV(AssemblyNode.MOV instruction) {
			if (instruction.Source is AssemblyNode.Variable) {
				AssemblyNode.Variable variable = instruction.Source as AssemblyNode.Variable;
				instruction.Source = GetStackOffset(variable.Identifier);
			}
			
			if (instruction.Destination is AssemblyNode.Variable) {
				AssemblyNode.Variable variable = instruction.Destination as AssemblyNode.Variable;
				instruction.Destination = GetStackOffset(variable.Identifier);
			}
		}
		
		static void AssignVariablesUnaryInstruction(AssemblyNode.UnaryInstruction instruction) {
			if (instruction.Operand is AssemblyNode.Variable) {
				AssemblyNode.Variable variable = instruction.Operand as AssemblyNode.Variable;
				instruction.Operand = GetStackOffset(variable.Identifier);
			}
		}
		
		static void AssignVariablesBinaryInstruction(AssemblyNode.BinaryInstruction instruction) {
			if (instruction.Source is AssemblyNode.Variable) {
				AssemblyNode.Variable variable = instruction.Source as AssemblyNode.Variable;
				instruction.Source = GetStackOffset(variable.Identifier);
			}
			
			if (instruction.Destination is AssemblyNode.Variable) {
				AssemblyNode.Variable variable = instruction.Destination as AssemblyNode.Variable;
				instruction.Destination = GetStackOffset(variable.Identifier);
			}
		}
		
		public static void FixVariableAccesses(AssemblyTree tree) {
			AssemblyNode.Function function = tree.Program.Function;
			FixVariableAccessesFunction(function);
		}
		
		public static void FixVariableAccessesFunction(AssemblyNode.Function function) {
			for (int index = 0; index < function.Body.Count; index++)
			{
				if (function.Body[index] is AssemblyNode.MOV) FixVariableAccessesMOV(ref function.Body, index);
				if (function.Body[index] is AssemblyNode.BinaryInstruction) FixVariableAccessesBinaryInstruction(ref function.Body, index);
				if (function.Body[index] is AssemblyNode.IDIV) FixVariableAccessesIDIV(ref function.Body, index);
			}
		}
		
		static void FixVariableAccessesMOV(ref List<AssemblyNode.Instruction> instructions, int index) {
			// mov can't have memory locations as both operands
			AssemblyNode.MOV instruction = instructions[index] as AssemblyNode.MOV;
			if (instruction.Source is AssemblyNode.StackAccess && instruction.Destination is AssemblyNode.StackAccess) {
				AssemblyNode.StackAccess source = instruction.Source as AssemblyNode.StackAccess;
				instruction.Source = RegisterType.R10.ToOperand();
				instructions.Insert(index, new AssemblyNode.MOV(source, RegisterType.R10.ToOperand()));
			}
		}
		
		static void FixVariableAccessesIDIV(ref List<AssemblyNode.Instruction> instructions, int index) {
			// idiv can't have immediate as operand
			AssemblyNode.IDIV instruction = instructions[index] as AssemblyNode.IDIV;
			if (instruction.Operand is AssemblyNode.Immediate) {
				AssemblyNode.Immediate operand = instruction.Operand as AssemblyNode.Immediate;
				instruction.Operand = RegisterType.R10.ToOperand();
				instructions.Insert(index, new AssemblyNode.MOV(operand, RegisterType.R10.ToOperand()));
			}
		}
		
		static void FixVariableAccessesBinaryInstruction(ref List<AssemblyNode.Instruction> instructions, int index) {
			AssemblyNode.BinaryInstruction instruction = instructions[index] as AssemblyNode.BinaryInstruction;
			
			// imul can't have destination as a memory location
			if (instruction.Operator == Syntax.BinaryOperator.Multiplication && instruction.Destination is AssemblyNode.StackAccess) {
				AssemblyNode.StackAccess destination = instruction.Destination as AssemblyNode.StackAccess;
				instructions.Insert(index, new AssemblyNode.MOV(destination, RegisterType.R11.ToOperand()));
				instruction.Destination = RegisterType.R11.ToOperand();
				instructions.Insert(index + 2, new AssemblyNode.MOV(RegisterType.R11.ToOperand(), destination));
			}
			
			// add/sub can't have both operands be memory locations
			if (instruction.Source is AssemblyNode.StackAccess && instruction.Destination is AssemblyNode.StackAccess) {
				AssemblyNode.StackAccess source = instruction.Source as AssemblyNode.StackAccess;
				instruction.Source = RegisterType.R10.ToOperand();
				instructions.Insert(index, new AssemblyNode.MOV(source, RegisterType.R10.ToOperand()));
			}
		}
	}
}