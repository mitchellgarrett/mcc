using System.Collections.Generic;
using FTG.Studios.MCC.Assembly;
using FTG.Studios.MCC.Lexer;

namespace FTG.Studios.MCC;

public static class VariableAccessFixer
{
	public static void FixVariableAccesses(AssemblyTree tree)
	{
		foreach (var definition in tree.Program.TopLevelDefinitions)
			if (definition is AssemblyNode.Function function) FixVariableAccessesFunction(function);
	}
	
	static void FixVariableAccessesFunction(AssemblyNode.Function function) {
		for (int index = 0; index < function.Body.Count; index++)
		{
			if (function.Body[index] is AssemblyNode.MOV) FixVariableAccessesMOV(function.Body, index);
			if (function.Body[index] is AssemblyNode.MOVSX) FixVariableAccessesMOVSX(function.Body, index);
			if (function.Body[index] is AssemblyNode.MOVZ) FixVariableAccessesMOVZ(function.Body, index);
			if (function.Body[index] is AssemblyNode.CMP) FixVariableAccessesCMP(function.Body, index);
			if (function.Body[index] is AssemblyNode.IDIV) FixVariableAccessesIDIV(function.Body, index);
			if (function.Body[index] is AssemblyNode.DIV) FixVariableAccessesDIV(function.Body, index);
			if (function.Body[index] is AssemblyNode.Push) FixVariableAccessesPush(function.Body, index);
			if (function.Body[index] is AssemblyNode.Binary) FixVariableAccessesBinaryInstruction(function.Body, index);
		}
	}
	
	static void FixVariableAccessesMOV(List<AssemblyNode.Instruction> instructions, int index) {
		// MOV can't have memory locations as both operands
		AssemblyNode.MOV instruction = instructions[index] as AssemblyNode.MOV;
		if (instruction.Source is AssemblyNode.MemoryAccess && instruction.Destination is AssemblyNode.MemoryAccess) {
			instructions.Insert(index, new AssemblyNode.MOV(instruction.Type, instruction.Source, RegisterType.R10.ToOperand()));
			instruction.Source = RegisterType.R10.ToOperand();
		}
		
		// MOVQ can't move immediates into memory
		if (instruction.Source is AssemblyNode.Immediate && instruction.Destination is AssemblyNode.MemoryAccess && instruction.Type == AssemblyType.QuadWord)
		{
			instructions.Insert(index, new AssemblyNode.MOV(instruction.Type, instruction.Source, RegisterType.R10.ToOperand()));
			instruction.Source = RegisterType.R10.ToOperand();
		}
	}
	
	static void FixVariableAccessesMOVSX(List<AssemblyNode.Instruction> instructions, int index) {
		// MOVSX can't have an immediate as the source
		AssemblyNode.MOVSX instruction = instructions[index] as AssemblyNode.MOVSX;
		var source = instruction.Source;
		if (instruction.Source is AssemblyNode.Immediate)
		{
			source = RegisterType.R10.ToOperand();
			instructions.Insert(
				index++, 
				new AssemblyNode.MOV(
					AssemblyType.LongWord,
					instruction.Source, 
					source
				)
			);
		}
		
		// MOVSX can't have a memory location as the destination
		if (instruction.Destination is AssemblyNode.MemoryAccess)
		{
			instructions.Insert(
			index + 1, 
			new AssemblyNode.MOV(
				AssemblyType.QuadWord,
				RegisterType.R11.ToOperand(), 
				instruction.Destination
				)
			);
			instruction.Destination = RegisterType.R11.ToOperand();
		}
		instruction.Source = source;
	}
	
	static void FixVariableAccessesMOVZ(List<AssemblyNode.Instruction> instructions, int index) {
		AssemblyNode.MOVZ instruction = instructions[index] as AssemblyNode.MOVZ;
		// Remove the original instruction since it doesn't actually exist
		instructions.RemoveAt(index);
		
		// If the destination is already a register, we just move the source to it
		if (instruction.Destination is AssemblyNode.Register) {
			instructions.Insert(index, new AssemblyNode.MOV(AssemblyType.LongWord, instruction.Source, instruction.Destination));
		}
		
		// If the destination is already a memory access, we have to move the source to a register first
		if (instruction.Destination is AssemblyNode.MemoryAccess) {
			instructions.Insert(index, new AssemblyNode.MOV(AssemblyType.LongWord, instruction.Source, RegisterType.R11.ToOperand()));
			instructions.Insert(index + 1, new AssemblyNode.MOV(AssemblyType.QuadWord, RegisterType.R11.ToOperand(), instruction.Destination));
		}
	}
	
	static void FixVariableAccessesCMP(List<AssemblyNode.Instruction> instructions, int index) {
		// CMP can't have an immediate value >= 2^32, including negatives since their binary representation has the MSB set
		AssemblyNode.CMP instruction = instructions[index] as AssemblyNode.CMP;
		if (instruction.LeftOperand is AssemblyNode.Immediate left_operand && (left_operand.Value > int.MaxValue || left_operand.Value < 0))
		{
			instructions.Insert(index++, new AssemblyNode.MOV(AssemblyType.QuadWord, left_operand, RegisterType.R10.ToOperand()));
			instruction.LeftOperand = RegisterType.R10.ToOperand();
		}
		
		// CMP can't have memory locations as both operands
		if (instruction.LeftOperand is AssemblyNode.MemoryAccess && instruction.RightOperand is AssemblyNode.MemoryAccess memory_access) {
			instructions.Insert(index, new AssemblyNode.MOV(instruction.Type, memory_access, RegisterType.R10.ToOperand()));
			instruction.RightOperand = RegisterType.R10.ToOperand();
		}
		
		// CMP can't have immediate as second operand
		if (instruction.RightOperand is AssemblyNode.Immediate immediate) {
			instructions.Insert(index, new AssemblyNode.MOV(instruction.Type, immediate, RegisterType.R11.ToOperand()));
			instruction.RightOperand = RegisterType.R11.ToOperand();
		}
	}
	
	static void FixVariableAccessesIDIV(List<AssemblyNode.Instruction> instructions, int index) {
		// IDIV can't have an immediate as the operand
		AssemblyNode.IDIV instruction = instructions[index] as AssemblyNode.IDIV;
		if (instruction.Operand is AssemblyNode.Immediate immediate) {
			instruction.Operand = RegisterType.R10.ToOperand();
			instructions.Insert(index, new AssemblyNode.MOV(instruction.Type, immediate, RegisterType.R10.ToOperand()));
		}
	}
	
	static void FixVariableAccessesDIV(List<AssemblyNode.Instruction> instructions, int index) {
		// DIV can't have an immediate as the operand
		AssemblyNode.DIV instruction = instructions[index] as AssemblyNode.DIV;
		if (instruction.Operand is AssemblyNode.Immediate immediate) {
			instruction.Operand = RegisterType.R10.ToOperand();
			instructions.Insert(index, new AssemblyNode.MOV(instruction.Type, immediate, RegisterType.R10.ToOperand()));
		}
	}
	
	static void FixVariableAccessesPush(List<AssemblyNode.Instruction> instructions, int index) {
		// Push can't have an immediate value >= 2^32
		AssemblyNode.Push instruction = instructions[index] as AssemblyNode.Push;
		if (instruction.Operand is AssemblyNode.Immediate immediate && immediate.Value > int.MaxValue)
		{
			instructions.Insert(index, new AssemblyNode.MOV(AssemblyType.QuadWord, immediate, RegisterType.R10.ToOperand()));
			instruction.Operand = RegisterType.R10.ToOperand();
		}
	}
	
	static void FixVariableAccessesBinaryInstruction(List<AssemblyNode.Instruction> instructions, int index) {
		AssemblyNode.Binary instruction = instructions[index] as AssemblyNode.Binary;

		// IMUL/ADD/SUB can't have an immediate value >= 2^32
		if (
			(instruction.Operator == Syntax.BinaryOperator.Multiplication || instruction.Operator == Syntax.BinaryOperator.Addition || instruction.Operator == Syntax.BinaryOperator.Subtraction)
			&& instruction.Source is AssemblyNode.Immediate immediate
			&& immediate.Value > int.MaxValue
		)
		{
			instructions.Insert(index++, new AssemblyNode.MOV(AssemblyType.QuadWord, immediate, RegisterType.R10.ToOperand()));
			instruction.Source = RegisterType.R10.ToOperand();
		}
		
		// IMUL can't have destination as a memory location
		if (instruction.Operator == Syntax.BinaryOperator.Multiplication && instruction.Destination is AssemblyNode.MemoryAccess destination) {
			instructions.Insert(index, new AssemblyNode.MOV(instruction.Type, destination, RegisterType.R11.ToOperand()));
			instruction.Destination = RegisterType.R11.ToOperand();
			instructions.Insert(index + 2, new AssemblyNode.MOV(instruction.Type, RegisterType.R11.ToOperand(), destination));
		}
		
		// ADD/SUB can't have both operands be memory locations
		if (instruction.Source is AssemblyNode.MemoryAccess source && instruction.Destination is AssemblyNode.MemoryAccess) {
			instruction.Source = RegisterType.R10.ToOperand();
			instructions.Insert(index, new AssemblyNode.MOV(instruction.Type, source, RegisterType.R10.ToOperand()));
		}
	}
}