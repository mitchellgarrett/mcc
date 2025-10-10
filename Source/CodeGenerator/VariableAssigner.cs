using System.Collections.Generic;
using FTG.Studios.MCC.Assembly;
using FTG.Studios.MCC.SemanticAnalysis;

namespace FTG.Studios.MCC.CodeGeneration;

public static class VariableAssigner
{
	static int stack_offset;
	static readonly Dictionary<string, int> variable_stack_offsets = [];
	
	static AssemblyNode.StackAccess GetStackOffset(string identifier) {
		if (variable_stack_offsets.TryGetValue(identifier, out int offset)) return new AssemblyNode.StackAccess(offset);
		stack_offset -= 4;
		variable_stack_offsets[identifier] = stack_offset;
		return new AssemblyNode.StackAccess(stack_offset);
	}
	
	public static void AssignVariables(AssemblyTree tree, SymbolTable symbol_table)
	{
		foreach (var definition in tree.Program.TopLevelDefinitions)
			if (definition is AssemblyNode.Function function) AssignVariablesFunction(function, symbol_table);
	}

	static AssemblyNode.MemoryAccess GetMemoryAccess(string identifier, SymbolTable symbol_table)
	{
		if (symbol_table.TryGetSymbol(identifier, out SymbolTableEntry entry))
			if (entry.Attributes is IdentifierAttributes.Static) return new AssemblyNode.DataAccess(identifier);
		return GetStackOffset(identifier);
	}
	
	static void AssignVariablesFunction(AssemblyNode.Function function, SymbolTable symbol_table)
	{
		stack_offset = 0;
		variable_stack_offsets.Clear();

		foreach (var instruction in function.Body)
		{
			if (instruction is AssemblyNode.MOV mov) AssignVariablesMOV(mov, symbol_table);
			if (instruction is AssemblyNode.CMP cmp) AssignVariablesCMP(cmp, symbol_table);
			if (instruction is AssemblyNode.SETCC setcc) AssignVariablesSET(setcc, symbol_table);
			if (instruction is AssemblyNode.IDIV idiv) AssignVariablesIDIV(idiv, symbol_table);
			if (instruction is AssemblyNode.Unary unary) AssignVariablesUnaryInstruction(unary, symbol_table);
			if (instruction is AssemblyNode.Binary binary) AssignVariablesBinaryInstruction(binary, symbol_table);
			if (instruction is AssemblyNode.Push push) AssignVariablesPushInstruction(push, symbol_table);
		}

		int space_to_allocate = System.Math.Abs(stack_offset);
		if (space_to_allocate > 0)
		{
			// Ensure the stack is 16-byte aligned
			if (space_to_allocate % 16 != 0) space_to_allocate += space_to_allocate % 16;
			function.Body.Insert(0, new AssemblyNode.AllocateStack(space_to_allocate));
			function.Body.Insert(0, new AssemblyNode.Comment($"allocate {space_to_allocate} bytes"));
		}
	}
	
	static void AssignVariablesMOV(AssemblyNode.MOV instruction, SymbolTable symbol_table) {
		if (instruction.Source is AssemblyNode.PseudoRegister) {
			AssemblyNode.PseudoRegister variable = instruction.Source as AssemblyNode.PseudoRegister;
			instruction.Source = GetMemoryAccess(variable.Identifier, symbol_table);
		}
		
		if (instruction.Destination is AssemblyNode.PseudoRegister) {
			AssemblyNode.PseudoRegister variable = instruction.Destination as AssemblyNode.PseudoRegister;
			instruction.Destination = GetMemoryAccess(variable.Identifier, symbol_table);
		}
	}
	
	static void AssignVariablesCMP(AssemblyNode.CMP instruction, SymbolTable symbol_table) {
		if (instruction.LeftOperand is AssemblyNode.PseudoRegister) {
			AssemblyNode.PseudoRegister variable = instruction.LeftOperand as AssemblyNode.PseudoRegister;
			instruction.LeftOperand = GetMemoryAccess(variable.Identifier, symbol_table);
		}
		
		if (instruction.RightOperand is AssemblyNode.PseudoRegister) {
			AssemblyNode.PseudoRegister variable = instruction.RightOperand as AssemblyNode.PseudoRegister;
			instruction.RightOperand = GetMemoryAccess(variable.Identifier, symbol_table);
		}
	}
	
	static void AssignVariablesSET(AssemblyNode.SETCC instruction, SymbolTable symbol_table) {
		if (instruction.Operand is AssemblyNode.PseudoRegister) {
			AssemblyNode.PseudoRegister variable = instruction.Operand as AssemblyNode.PseudoRegister;
			instruction.Operand = GetMemoryAccess(variable.Identifier, symbol_table);
		}
	}
	
	static void AssignVariablesIDIV(AssemblyNode.IDIV instruction, SymbolTable symbol_table) {
		if (instruction.Operand is AssemblyNode.PseudoRegister operand)
			instruction.Operand = GetMemoryAccess(operand.Identifier, symbol_table);
	}
	
	static void AssignVariablesUnaryInstruction(AssemblyNode.Unary instruction, SymbolTable symbol_table)
	{
		if (instruction.Operand is AssemblyNode.PseudoRegister)
		{
			AssemblyNode.PseudoRegister variable = instruction.Operand as AssemblyNode.PseudoRegister;
			instruction.Operand = GetMemoryAccess(variable.Identifier, symbol_table);
		}
	}
	
	static void AssignVariablesBinaryInstruction(AssemblyNode.Binary instruction, SymbolTable symbol_table) {
		if (instruction.Source is AssemblyNode.PseudoRegister) {
			AssemblyNode.PseudoRegister variable = instruction.Source as AssemblyNode.PseudoRegister;
			instruction.Source = GetMemoryAccess(variable.Identifier, symbol_table);
		}
		
		if (instruction.Destination is AssemblyNode.PseudoRegister) {
			AssemblyNode.PseudoRegister variable = instruction.Destination as AssemblyNode.PseudoRegister;
			instruction.Destination = GetMemoryAccess(variable.Identifier, symbol_table);
		}
	}
	
	static void AssignVariablesPushInstruction(AssemblyNode.Push instruction, SymbolTable symbol_table)
	{
		if (instruction.Operand is AssemblyNode.PseudoRegister variable)
			instruction.Operand = GetMemoryAccess(variable.Identifier, symbol_table);
	}
}