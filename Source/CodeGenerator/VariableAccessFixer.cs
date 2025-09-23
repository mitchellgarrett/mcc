using System.Collections.Generic;
using FTG.Studios.MCC.Assembly;
using FTG.Studios.MCC.Lexer;

namespace FTG.Studios.MCC;

public static class VariableAccessFixer
{
	public static void FixVariableAccesses(AssemblyTree tree)
	{
		foreach (var function in tree.Program.Functions) FixVariableAccessesFunction(function);
	}
	
	static void FixVariableAccessesFunction(AssemblyNode.Function function) {
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