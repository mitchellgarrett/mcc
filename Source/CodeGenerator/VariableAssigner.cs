using System;
using System.Collections.Generic;
using FTG.Studios.MCC.Assembly;

namespace FTG.Studios.MCC.CodeGeneration;

public static class VariableAssigner
{
	static int stack_offset;
	static readonly Dictionary<string, int> variable_stack_offsets = [];
	
	static AssemblyNode.StackAccess GetStackOffset(string identifier, int size) {
		if (variable_stack_offsets.TryGetValue(identifier, out int offset)) return new AssemblyNode.StackAccess(offset);
		stack_offset -= size;
		// Ensure stack is aligned with the requested size of the variable
		stack_offset += stack_offset % size;
		variable_stack_offsets[identifier] = stack_offset;
		return new AssemblyNode.StackAccess(stack_offset);
	}
	
	public static void AssignVariables(AssemblyTree tree, AssemblySymbolTable symbol_table)
	{
		foreach (var definition in tree.Program.TopLevelDefinitions)
			if (definition is AssemblyNode.Function function) AssignVariablesFunction(function, symbol_table);
	}

	static AssemblyNode.MemoryAccess GetMemoryAccess(string identifier, AssemblySymbolTable symbol_table)
	{
		// FIXME: Idk if this is right
		if (symbol_table.TryGetSymbol(identifier, out AssemblySymbolTableEntry entry))
		{
			if (entry is ObjectEntry @object) {
				if (@object.IsStatic) return new AssemblyNode.DataAccess(identifier);
				return GetStackOffset(identifier, @object.Type.GetSize());
			}
			if (entry is Assembly.FunctionEntry) return new AssemblyNode.DataAccess(identifier);
		}
		throw new Exception();
	}
	
	static void AssignVariablesFunction(AssemblyNode.Function function, AssemblySymbolTable symbol_table)
	{
		stack_offset = 0;
		variable_stack_offsets.Clear();

		foreach (var instruction in function.Body)
		{
			if (instruction is AssemblyNode.MOV mov) AssignVariablesMOV(mov, symbol_table);
			if (instruction is AssemblyNode.MOVSX movsx) AssignVariablesMOVSX(movsx, symbol_table);
			if (instruction is AssemblyNode.MOVZ movz) AssignVariablesMOVZ(movz, symbol_table);
			if (instruction is AssemblyNode.CMP cmp) AssignVariablesCMP(cmp, symbol_table);
			if (instruction is AssemblyNode.SETCC setcc) AssignVariablesSET(setcc, symbol_table);
			if (instruction is AssemblyNode.IDIV idiv) AssignVariablesIDIV(idiv, symbol_table);
			if (instruction is AssemblyNode.DIV div) AssignVariablesDIV(div, symbol_table);
			if (instruction is AssemblyNode.Unary unary) AssignVariablesUnaryInstruction(unary, symbol_table);
			if (instruction is AssemblyNode.Binary binary) AssignVariablesBinaryInstruction(binary, symbol_table);
			if (instruction is AssemblyNode.Push push) AssignVariablesPushInstruction(push, symbol_table);
		}

		int space_to_allocate = Math.Abs(stack_offset);
		if (space_to_allocate > 0)
		{
			// Ensure the stack is 16-byte aligned
			if (space_to_allocate % 16 != 0) space_to_allocate += space_to_allocate % 16;
			function.Body.Insert(
				0, 
				new AssemblyNode.Binary(
					AssemblyType.QuadWord, 
					Lexer.Syntax.BinaryOperator.Subtraction, 
					space_to_allocate.ToAssemblyImmediate(), 
					RegisterType.SP.ToOperand()
				)
			);
			function.Body.Insert(0, new AssemblyNode.Comment($"allocate {space_to_allocate} bytes"));
		}
	}
	
	static void AssignVariablesMOV(AssemblyNode.MOV instruction, AssemblySymbolTable symbol_table) {
		if (instruction.Source is AssemblyNode.PseudoRegister source) {
			instruction.Source = GetMemoryAccess(source.Identifier, symbol_table);
		}
		
		if (instruction.Destination is AssemblyNode.PseudoRegister destination) {
			instruction.Destination = GetMemoryAccess(destination.Identifier, symbol_table);
		}
	}
	
	static void AssignVariablesMOVSX(AssemblyNode.MOVSX instruction, AssemblySymbolTable symbol_table) {
		if (instruction.Source is AssemblyNode.PseudoRegister source) {
			instruction.Source = GetMemoryAccess(source.Identifier, symbol_table);
		}
		
		if (instruction.Destination is AssemblyNode.PseudoRegister destination) {
			instruction.Destination = GetMemoryAccess(destination.Identifier, symbol_table);
		}
	}
	
	static void AssignVariablesMOVZ(AssemblyNode.MOVZ instruction, AssemblySymbolTable symbol_table) {
		if (instruction.Source is AssemblyNode.PseudoRegister source) {
			instruction.Source = GetMemoryAccess(source.Identifier, symbol_table);
		}
		
		if (instruction.Destination is AssemblyNode.PseudoRegister destination) {
			instruction.Destination = GetMemoryAccess(destination.Identifier, symbol_table);
		}
	}
	
	static void AssignVariablesCMP(AssemblyNode.CMP instruction, AssemblySymbolTable symbol_table) {
		if (instruction.LeftOperand is AssemblyNode.PseudoRegister left_operand) {
			instruction.LeftOperand = GetMemoryAccess(left_operand.Identifier, symbol_table);
		}
		
		if (instruction.RightOperand is AssemblyNode.PseudoRegister right_operand) {
			instruction.RightOperand = GetMemoryAccess(right_operand.Identifier, symbol_table);
		}
	}
	
	static void AssignVariablesSET(AssemblyNode.SETCC instruction, AssemblySymbolTable symbol_table) {
		if (instruction.Operand is AssemblyNode.PseudoRegister variable) {
			instruction.Operand = GetMemoryAccess(variable.Identifier, symbol_table);
		}
	}
	
	static void AssignVariablesIDIV(AssemblyNode.IDIV instruction, AssemblySymbolTable symbol_table) {
		if (instruction.Operand is AssemblyNode.PseudoRegister operand)
			instruction.Operand = GetMemoryAccess(operand.Identifier, symbol_table);
	}
	
	static void AssignVariablesDIV(AssemblyNode.DIV instruction, AssemblySymbolTable symbol_table) {
		if (instruction.Operand is AssemblyNode.PseudoRegister operand)
			instruction.Operand = GetMemoryAccess(operand.Identifier, symbol_table);
	}
	
	static void AssignVariablesUnaryInstruction(AssemblyNode.Unary instruction, AssemblySymbolTable symbol_table)
	{
		if (instruction.Operand is AssemblyNode.PseudoRegister variable)
			instruction.Operand = GetMemoryAccess(variable.Identifier, symbol_table);
	}
	
	static void AssignVariablesBinaryInstruction(AssemblyNode.Binary instruction, AssemblySymbolTable symbol_table) {
		if (instruction.Source is AssemblyNode.PseudoRegister source) {
			instruction.Source = GetMemoryAccess(source.Identifier, symbol_table);
		}
		
		if (instruction.Destination is AssemblyNode.PseudoRegister destination) {
			instruction.Destination = GetMemoryAccess(destination.Identifier, symbol_table);
		}
	}
	
	static void AssignVariablesPushInstruction(AssemblyNode.Push instruction, AssemblySymbolTable symbol_table)
	{
		if (instruction.Operand is AssemblyNode.PseudoRegister variable)
			instruction.Operand = GetMemoryAccess(variable.Identifier, symbol_table);
	}
}