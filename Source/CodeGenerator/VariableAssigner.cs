using System.Collections.Generic;

namespace FTG.Studios.MCC
{

	public static class VariableAssigner
	{
		static int stack_offset;
		static readonly Dictionary<string, int> variable_stack_offsets = new Dictionary<string, int>();
		
		static AssemblyNode.StackAccess GetStackOffset(string identifier) {
			if (variable_stack_offsets.TryGetValue(identifier, out int offset)) return new AssemblyNode.StackAccess(offset);
			stack_offset -= 4;
			variable_stack_offsets[identifier] = stack_offset;
			return new AssemblyNode.StackAccess(stack_offset);
		}
		
		public static void AssignVariables(AssemblyTree tree)
		{
			foreach (var function in tree.Program.Functions) AssignVariablesFunction(function);
		}
		
		static void AssignVariablesFunction(AssemblyNode.Function function) {
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
	}
}