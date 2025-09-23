using System.Collections.Generic;

namespace FTG.Studios.MCC {
	
	public static class CodeOptimizer {
		
		static int stack_offset;
		static readonly Dictionary<string, int> variable_stack_offsets = new Dictionary<string, int>();
		
		static AssemblyNode.StackAccess GetStackOffset(string identifier) {
			if (variable_stack_offsets.TryGetValue(identifier, out int offset)) return new AssemblyNode.StackAccess(offset);
			stack_offset -= 4;
			variable_stack_offsets[identifier] = stack_offset;
			return new AssemblyNode.StackAccess(stack_offset);
		}
		
		public static void AssignVariables(AssemblyTree tree) {
			foreach (var function in tree.Program.Functions) AssignVariablesFunction(function);
		}
		
		public static void AssignVariablesFunction(AssemblyNode.Function function) {
			stack_offset = 0;
			variable_stack_offsets.Clear();

			foreach (var instruction in function.Body)
			{
				if (instruction is AssemblyNode.MOV instruction_mov) AssignVariablesMOV(instruction_mov);
				if (instruction is AssemblyNode.CMP instruction_cmp) AssignVariablesCMP(instruction_cmp);
				if (instruction is AssemblyNode.SETCC instruction_set) AssignVariablesSET(instruction_set);
				if (instruction is AssemblyNode.IDIV instruction_idiv) AssignVariablesIDIV(instruction_idiv);
				if (instruction is AssemblyNode.Unary instruction_unary) AssignVariablesUnaryInstruction(instruction_unary);
				if (instruction is AssemblyNode.Binary instruction_binary) AssignVariablesBinaryInstruction(instruction_binary);
				if (instruction is AssemblyNode.Push instruction_push) AssignVariablesPushInstruction(instruction_push);
			}
			
			int space_to_allocate = System.Math.Abs(stack_offset);
			if (space_to_allocate > 0) {
				// Ensure the stack is 16-byte aligned
				if (space_to_allocate % 16 != 0) space_to_allocate += space_to_allocate % 16;
				function.Body.Insert(0, new AssemblyNode.AllocateStack(space_to_allocate));
				function.Body.Insert(0, new AssemblyNode.Comment($"allocate {space_to_allocate} bytes"));
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
		
		static void AssignVariablesCMP(AssemblyNode.CMP instruction) {
			if (instruction.LeftOperand is AssemblyNode.Variable) {
				AssemblyNode.Variable variable = instruction.LeftOperand as AssemblyNode.Variable;
				instruction.LeftOperand = GetStackOffset(variable.Identifier);
			}
			
			if (instruction.RightOperand is AssemblyNode.Variable) {
				AssemblyNode.Variable variable = instruction.RightOperand as AssemblyNode.Variable;
				instruction.RightOperand = GetStackOffset(variable.Identifier);
			}
		}
		
		static void AssignVariablesSET(AssemblyNode.SETCC instruction) {
			if (instruction.Operand is AssemblyNode.Variable) {
				AssemblyNode.Variable variable = instruction.Operand as AssemblyNode.Variable;
				instruction.Operand = GetStackOffset(variable.Identifier);
			}
		}
		
		static void AssignVariablesIDIV(AssemblyNode.IDIV instruction) {
			if (instruction.Operand is AssemblyNode.Variable operand)
			{
				instruction.Operand = GetStackOffset(operand.Identifier);
			}
		}
		
		static void AssignVariablesUnaryInstruction(AssemblyNode.Unary instruction)
		{
			if (instruction.Operand is AssemblyNode.Variable)
			{
				AssemblyNode.Variable variable = instruction.Operand as AssemblyNode.Variable;
				instruction.Operand = GetStackOffset(variable.Identifier);
			}
		}
		
		static void AssignVariablesBinaryInstruction(AssemblyNode.Binary instruction) {
			if (instruction.Source is AssemblyNode.Variable) {
				AssemblyNode.Variable variable = instruction.Source as AssemblyNode.Variable;
				instruction.Source = GetStackOffset(variable.Identifier);
			}
			
			if (instruction.Destination is AssemblyNode.Variable) {
				AssemblyNode.Variable variable = instruction.Destination as AssemblyNode.Variable;
				instruction.Destination = GetStackOffset(variable.Identifier);
			}
		}
		
		static void AssignVariablesPushInstruction(AssemblyNode.Push instruction)
		{
			if (instruction.Operand is AssemblyNode.Variable variable)
				instruction.Operand = GetStackOffset(variable.Identifier);
		}
		
		public static void FixVariableAccesses(AssemblyTree tree)
		{
			foreach (var function in tree.Program.Functions) FixVariableAccessesFunction(function);
		}
		
		public static void FixVariableAccessesFunction(AssemblyNode.Function function) {
			for (int index = 0; index < function.Body.Count; index++)
			{
				if (function.Body[index] is AssemblyNode.MOV) FixVariableAccessesMOV(ref function.Body, index);
				if (function.Body[index] is AssemblyNode.CMP) FixVariableAccessesCMP(ref function.Body, index);
				if (function.Body[index] is AssemblyNode.IDIV) FixVariableAccessesIDIV(ref function.Body, index);
				if (function.Body[index] is AssemblyNode.Binary) FixVariableAccessesBinaryInstruction(ref function.Body, index);
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
		
		static void FixVariableAccessesCMP(ref List<AssemblyNode.Instruction> instructions, int index) {
			// cmp can't have memory locations as both operands
			AssemblyNode.CMP instruction = instructions[index] as AssemblyNode.CMP;
			if (instruction.LeftOperand is AssemblyNode.StackAccess && instruction.RightOperand is AssemblyNode.StackAccess) {
				AssemblyNode.StackAccess left_operand = instruction.LeftOperand as AssemblyNode.StackAccess;
				AssemblyNode.StackAccess right_operand = instruction.RightOperand as AssemblyNode.StackAccess;
				instruction.RightOperand = RegisterType.R10.ToOperand();
				instructions.Insert(index, new AssemblyNode.MOV(right_operand, RegisterType.R10.ToOperand()));
			}
			
			// cmp can't have immediate as second operand
			if (instruction.RightOperand is AssemblyNode.Immediate) {
				AssemblyNode.Immediate right_operand = instruction.RightOperand as AssemblyNode.Immediate;
				instruction.RightOperand = RegisterType.R11.ToOperand();
				instructions.Insert(index, new AssemblyNode.MOV(right_operand, RegisterType.R11.ToOperand()));
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
			AssemblyNode.Binary instruction = instructions[index] as AssemblyNode.Binary;
			
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