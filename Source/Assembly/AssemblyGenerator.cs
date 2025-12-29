using System;
using System.Collections.Generic;
using System.Linq;
using FTG.Studios.MCC.Intermediate;
using FTG.Studios.MCC.Lexer;
using FTG.Studios.MCC.Parser;
using FTG.Studios.MCC.SemanticAnalysis;

namespace FTG.Studios.MCC.Assembly;
	
public static class AssemblyGenerator {
	
	public static AssemblyTree Generate(IntermediateTree tree, SymbolTable symbol_table) {
		AssemblyNode.Program program = GenerateProgram(tree.Program, symbol_table);
		return new AssemblyTree(program);
	}
	
	static AssemblyNode.Program GenerateProgram(IntermediateNode.Program program, SymbolTable symbol_table) {
		List<AssemblyNode.TopLevel> definitions = [];
		foreach (var definition in program.TopLevelDefinitions) definitions.Add(GenerateTopLevelDefinition(definition, symbol_table));
		return new AssemblyNode.Program(definitions);
	}

	static AssemblyNode.TopLevel GenerateTopLevelDefinition(IntermediateNode.TopLevel definition, SymbolTable symbol_table)
	{
		if (definition is IntermediateNode.Function function) return GenerateFunctionDefinition(function, symbol_table);
		if (definition is IntermediateNode.StaticVariable static_variable) return GenerateStaticVariable(static_variable);
		throw new Exception();
	}
	
	static AssemblyNode.Function GenerateFunctionDefinition(IntermediateNode.Function function, SymbolTable symbol_table)
	{
		string identifier = function.Identifier;

		AssemblyNode.Operand[] parameters = function.Parameters.Select(p => new AssemblyNode.PseudoRegister(p.Identifier)).ToArray();

		List<AssemblyNode.Instruction> instructions =
		[
			// TODO: This may or may not be necessary
			new AssemblyNode.Comment("align stack"),
			new AssemblyNode.Binary(AssemblyType.QuadWord, Syntax.BinaryOperator.Subtraction, 8.ToAssemblyImmediate(), RegisterType.SP.ToOperand()),
			new AssemblyNode.Comment("copy arguments to stack")
		];

		// The first 6 parameters are stored in registers
		for (int i = 0; i < 6 && i < function.Parameters.Length; i++)
		{
			instructions.Add(
				new AssemblyNode.MOV(
					function.Parameters[i].ToAssemblyType(symbol_table),
					RegisterTypeExtensions.FunctionCallOrder[i].ToOperand(),
					parameters[i]
				)
			);
		}

		// The rest of the parameters are stored in the stack in reverse order
		for (int i = function.Parameters.Length - 1; i >= 6; i--)
		{
			instructions.Add(
				new AssemblyNode.MOV(
					function.Parameters[i].ToAssemblyType(symbol_table),
					new AssemblyNode.StackAccess(16 + 8 * (i - 6)),
					parameters[i]
				)
			);
		}

		foreach (var instruction in function.Body) GenerateInstruction(instructions, symbol_table, instruction);

		return new AssemblyNode.Function(identifier, function.IsGlobal, parameters.ToArray(), instructions);
	}

	static AssemblyNode.StaticVariable GenerateStaticVariable(IntermediateNode.StaticVariable variable)
	{
		int alignment = variable.InitialValue.Type.ToAssemblyType().GetSize();
		return new AssemblyNode.StaticVariable(variable.Identifier, variable.IsGlobal, alignment, variable.InitialValue);
	}

	static void GenerateInstruction(List<AssemblyNode.Instruction> instructions, SymbolTable symbol_table, IntermediateNode.Instruction instruction)
	{
		if (instruction is IntermediateNode.Comment comment) GenerateComment(instructions, comment);
		if (instruction is IntermediateNode.Return @return) GenerateReturnInstruction(instructions, symbol_table, @return);
		if (instruction is IntermediateNode.UnaryInstruction unary) GenerateUnaryInstruction(instructions, symbol_table, unary);
		if (instruction is IntermediateNode.BinaryInstruction binary) GenerateBinaryInstruction(instructions, symbol_table, binary);
		if (instruction is IntermediateNode.Jump jump) GenerateJumpInstruction(instructions, jump);
		if (instruction is IntermediateNode.JumpIfZero jump_if_zero) GenerateJumpIfZeroInstruction(instructions, symbol_table, jump_if_zero);
		if (instruction is IntermediateNode.JumpIfNotZero jump_if_not_zero) GenerateJumpIfNotZeroInstruction(instructions, symbol_table, jump_if_not_zero);
		if (instruction is IntermediateNode.Copy copy) GenerateCopyInstruction(instructions, symbol_table, copy);
		if (instruction is IntermediateNode.Label label) GenerateLabel(instructions, label);
		if (instruction is IntermediateNode.FunctionCall function_call) GenerateFunctionCall(instructions, function_call, symbol_table);
		if (instruction is IntermediateNode.SignExtend sign_extend) GenerateSignExtend(instructions, sign_extend);
		if (instruction is IntermediateNode.Truncate truncate) GenerateTruncate(instructions, truncate);
	}
	
	static void GenerateComment(List<AssemblyNode.Instruction> instructions, IntermediateNode.Comment comment) {
		instructions.Add(new AssemblyNode.Comment(comment.Data));
	}
	
	static void GenerateReturnInstruction(List<AssemblyNode.Instruction> instructions, SymbolTable symbol_table, IntermediateNode.Return instruction) {
		AssemblyNode.Operand source = GenerateOperand(instruction.Value);
		AssemblyNode.Operand destination = RegisterType.AX.ToOperand();
		
		instructions.Add(
			new AssemblyNode.MOV(
				instruction.Value.ToAssemblyType(symbol_table),
				source, 
				destination
			)
		);
		instructions.Add(new AssemblyNode.RET());
	}
	
	static void GenerateUnaryInstruction(List<AssemblyNode.Instruction> instructions, SymbolTable symbol_table, IntermediateNode.UnaryInstruction instruction) {
		if (instruction.Operator == Syntax.UnaryOperator.Not) {
			// The source value has to be the left operand so the GenerateConditional function will pick the right type
			GenerateConditional(instructions, symbol_table, instruction.Source, 0.ToIntermediateConstant(), instruction.Destination, ConditionType.E);
			return;
		}
		
		AssemblyNode.Operand source = GenerateOperand(instruction.Source);
		AssemblyNode.Operand destination = GenerateOperand(instruction.Destination);
		
		var type = instruction.Source.ToAssemblyType(symbol_table);
		instructions.Add(
			new AssemblyNode.MOV(
				type,
				source, 
				destination
			)
		);
		instructions.Add(
			new AssemblyNode.Unary(
				type,
				instruction.Operator, 
				destination
			)
		);
	}

	static void GenerateBinaryInstruction(List<AssemblyNode.Instruction> instructions, SymbolTable symbol_table, IntermediateNode.BinaryInstruction instruction)
	{
		switch (instruction.Operator)
		{
			case Syntax.BinaryOperator.Addition:
			case Syntax.BinaryOperator.Subtraction:
			case Syntax.BinaryOperator.Multiplication:
				GenerateSimpleBinaryOperation(instructions, symbol_table, instruction);
				return;

			case Syntax.BinaryOperator.Division:
				GenerateIntegerDivision(instructions, symbol_table, instruction);
				return;

			case Syntax.BinaryOperator.Remainder:
				GenerateIntegerRemainder(instructions, symbol_table, instruction);
				return;

			case Syntax.BinaryOperator.LogicalLess:
				GenerateConditional(instructions, symbol_table, instruction.LeftOperand, instruction.RightOperand, instruction.Destination, ConditionType.L);
				return;

			case Syntax.BinaryOperator.LogicalGreater:
				GenerateConditional(instructions, symbol_table, instruction.LeftOperand, instruction.RightOperand, instruction.Destination, ConditionType.G);
				return;

			case Syntax.BinaryOperator.LogicalLessEqual:
				GenerateConditional(instructions, symbol_table, instruction.LeftOperand, instruction.RightOperand, instruction.Destination, ConditionType.LE);
				return;

			case Syntax.BinaryOperator.LogicalGreaterEqual:
				GenerateConditional(instructions, symbol_table, instruction.LeftOperand, instruction.RightOperand, instruction.Destination, ConditionType.GE);
				return;

			case Syntax.BinaryOperator.LogicalEqual:
				GenerateConditional(instructions, symbol_table, instruction.LeftOperand, instruction.RightOperand, instruction.Destination, ConditionType.E);
				return;
			
			case Syntax.BinaryOperator.LogicalNotEqual:
				GenerateConditional(instructions, symbol_table, instruction.LeftOperand, instruction.RightOperand, instruction.Destination, ConditionType.NE);
				return;
		}

		throw new Exception();
	}
	
	static void GenerateSimpleBinaryOperation(List<AssemblyNode.Instruction> instructions, SymbolTable symbol_table, IntermediateNode.BinaryInstruction instruction) {
		AssemblyNode.Operand lhs = GenerateOperand(instruction.LeftOperand);
		AssemblyNode.Operand rhs = GenerateOperand(instruction.RightOperand);
		AssemblyNode.Operand destination = GenerateOperand(instruction.Destination);
		
		AssemblyType type = instruction.LeftOperand.ToAssemblyType(symbol_table);
		
		instructions.Add(new AssemblyNode.MOV(type, lhs, destination));
		instructions.Add(new AssemblyNode.Binary(type, instruction.Operator, rhs, destination));
	}
	
	static void GenerateIntegerDivision(List<AssemblyNode.Instruction> instructions, SymbolTable symbol_table, IntermediateNode.BinaryInstruction instruction) {
		AssemblyNode.Operand lhs = GenerateOperand(instruction.LeftOperand);
		AssemblyNode.Operand rhs = GenerateOperand(instruction.RightOperand);
		AssemblyNode.Operand destination = GenerateOperand(instruction.Destination);
		
		var type = instruction.LeftOperand.ToAssemblyType(symbol_table);
		instructions.Add(
			new AssemblyNode.MOV(
				type,
				lhs, 
				RegisterType.AX.ToOperand()
			)
		);
		instructions.Add(
			new AssemblyNode.CDQ(
				type
			)
		);
		instructions.Add(
			new AssemblyNode.IDIV(
				type,
				rhs
			)
		);
		instructions.Add(
			new AssemblyNode.MOV(
				type, 
				RegisterType.AX.ToOperand(), 
				destination
			)
		);
	}
	
	static void GenerateIntegerRemainder(List<AssemblyNode.Instruction> instructions, SymbolTable symbol_table, IntermediateNode.BinaryInstruction instruction) {
		AssemblyNode.Operand lhs = GenerateOperand(instruction.LeftOperand);
		AssemblyNode.Operand rhs = GenerateOperand(instruction.RightOperand);
		AssemblyNode.Operand destination = GenerateOperand(instruction.Destination);
		
		var type = instruction.LeftOperand.ToAssemblyType(symbol_table);
		instructions.Add(
			new AssemblyNode.MOV(
				type,
				lhs, 
				RegisterType.AX.ToOperand()
			)
		);
		instructions.Add(
			new AssemblyNode.CDQ(
				type
			)
		);
		instructions.Add(
			new AssemblyNode.IDIV(
				type,
				rhs
			)
		);
		instructions.Add(
			new AssemblyNode.MOV(
				type, 
				RegisterType.DX.ToOperand(), 
				destination
			)
		);
	}
	
	static void GenerateConditional(List<AssemblyNode.Instruction> instructions, SymbolTable symbol_table, IntermediateNode.Operand left_operand, IntermediateNode.Operand right_operand, IntermediateNode.Operand destination, ConditionType condition) {
		var result = GenerateOperand(destination);
		instructions.Add(
			new AssemblyNode.CMP(
				left_operand.ToAssemblyType(symbol_table),
				GenerateOperand(right_operand), 
				GenerateOperand(left_operand)
			)
		);
		instructions.Add(
			new AssemblyNode.MOV(
				destination.ToAssemblyType(symbol_table),
				0.ToAssemblyImmediate(), result
			)
		);
		instructions.Add(new AssemblyNode.SETCC(result, condition));
	}
	
	static void GenerateJumpInstruction(List<AssemblyNode.Instruction> instructions, IntermediateNode.Jump instruction) {
		instructions.Add(new AssemblyNode.JMP(instruction.Target));
	}
	
	static void GenerateJumpIfZeroInstruction(List<AssemblyNode.Instruction> instructions, SymbolTable symbol_table, IntermediateNode.JumpIfZero instruction) {
		instructions.Add(
			new AssemblyNode.CMP(
				instruction.Condition.ToAssemblyType(symbol_table),
				0.ToAssemblyImmediate(), 
				GenerateOperand(instruction.Condition)
			)
		);
		instructions.Add(
			new AssemblyNode.JMPCC(
				instruction.Target, 
				ConditionType.E
			)
		);
	}
	
	static void GenerateJumpIfNotZeroInstruction(List<AssemblyNode.Instruction> instructions, SymbolTable symbol_table, IntermediateNode.JumpIfNotZero instruction) {
		instructions.Add(
			new AssemblyNode.CMP(
				instruction.Condition.ToAssemblyType(symbol_table),
				0.ToAssemblyImmediate(), 
				GenerateOperand(instruction.Condition)
			)
		);
		instructions.Add(
			new AssemblyNode.JMPCC(
				instruction.Target,
				ConditionType.NE
			)
		);
	}
	
	static void GenerateCopyInstruction(List<AssemblyNode.Instruction> instructions, SymbolTable symbol_table, IntermediateNode.Copy instruction) {
		instructions.Add(
			new AssemblyNode.MOV(
				instruction.Source.ToAssemblyType(symbol_table),
				GenerateOperand(instruction.Source), 
				GenerateOperand(instruction.Destination)
			)
		);
	}
	
	static void GenerateLabel(List<AssemblyNode.Instruction> instructions, IntermediateNode.Label instruction) {
		instructions.Add(new AssemblyNode.Label(instruction.Identifier));
	}

	static void GenerateFunctionCall(List<AssemblyNode.Instruction> instructions, IntermediateNode.FunctionCall function_call, SymbolTable symbol_table)
	{
		// Ensure stack is 16-byte aligned
		int stack_padding = 0;
		if ((function_call.Arguments.Length - 6) % 2 != 0)
		{
			stack_padding += 8;
			instructions.Add(
				new AssemblyNode.Binary(
					AssemblyType.QuadWord, 
					Syntax.BinaryOperator.Subtraction, 
					8.ToAssemblyImmediate(), 
					RegisterType.SP.ToOperand()
				)
			);
		}

		// Frst 6 arguments are copied to registers
		for (int i = 0; i < 6 && i < function_call.Arguments.Length; i++)
		{
			instructions.Add(
				new AssemblyNode.MOV(
					function_call.Arguments[i].ToAssemblyType(symbol_table),
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
			// TODO: Idk if the ToDataType business works here
			if (operand is AssemblyNode.Register || operand is AssemblyNode.Immediate || function_call.Arguments[i].ToAssemblyType(symbol_table) == AssemblyType.QuadWord)
			{
				instructions.Add(new AssemblyNode.Push(operand));
			}
			else
			{
				instructions.Add(new AssemblyNode.MOV(AssemblyType.LongWord, operand, RegisterType.AX.ToOperand()));
				instructions.Add(new AssemblyNode.Push(RegisterType.AX.ToOperand()));
			}
		}

		instructions.Add(new AssemblyNode.Call(function_call.Identifier, symbol_table.ContainsSymbol(function_call.Identifier)));

		// Restore stack pointer
		if (stack_padding != 0) instructions.Add(new AssemblyNode.Binary(AssemblyType.QuadWord, Syntax.BinaryOperator.Addition, stack_padding.ToAssemblyImmediate(), RegisterType.SP.ToOperand()));

		// Retrieve return value
		instructions.Add(
			new AssemblyNode.MOV(
				function_call.Destination.ToAssemblyType(symbol_table), 
				RegisterType.AX.ToOperand(), 
				GenerateOperand(function_call.Destination)
			)
		);
	}
	
	static void GenerateSignExtend(List<AssemblyNode.Instruction> instructions, IntermediateNode.SignExtend sign_extend)
	{
		instructions.Add(
			new AssemblyNode.MOVSX(
				GenerateOperand(sign_extend.Source),
				GenerateOperand(sign_extend.Destination)
				)
			);
	}
	
	static void GenerateTruncate(List<AssemblyNode.Instruction> instructions, IntermediateNode.Truncate truncate)
	{
		// Pre-truncate the int-to-long conversion
		var source = GenerateOperand(truncate.Source);
		if (source is AssemblyNode.Immediate immediate)
			if (immediate.Value > int.MaxValue) source = ((int)(long)immediate.Value).ToAssemblyImmediate();
		
		instructions.Add(
			new AssemblyNode.MOV(
				AssemblyType.LongWord,
				source,
				GenerateOperand(truncate.Destination)
			)
		);
	}
	
	static AssemblyNode.Operand GenerateOperand(IntermediateNode.Operand operand)
	{
		if (operand is IntermediateNode.Constant constant) return GenerateConstant(constant);
		if (operand is IntermediateNode.Variable variable) return GenerateVariable(variable);
		throw new Exception();
	}
	
	static AssemblyNode.Immediate GenerateConstant(IntermediateNode.Constant constant) {
		return constant.Type switch
		{
			PrimitiveType.Integer => new AssemblyNode.Immediate(constant.Value),
			PrimitiveType.Long => new AssemblyNode.Immediate(constant.Value),
			_ => throw new Exception(),
		};
	}
	
	static AssemblyNode.PseudoRegister GenerateVariable(IntermediateNode.Variable operand) {
		return new AssemblyNode.PseudoRegister(operand.Identifier);
	}
	
	public static AssemblyType ToAssemblyType(this IntermediateNode.Operand operand, SymbolTable symbol_table)
	{
		if (operand is IntermediateNode.Constant constant) return constant.ToAssemblyType();
		if (operand is IntermediateNode.Variable variable)
		{
			if (symbol_table.TryGetSymbol(variable.Identifier, out SymbolTableEntry entry))
				return entry.ReturnType.ToAssemblyType();
		}
		throw new Exception();
	}
	
	public static AssemblyType ToAssemblyType(this IntermediateNode.Constant constant)
	{
		return constant.Type switch
		{
			PrimitiveType.Integer => AssemblyType.LongWord,
			PrimitiveType.Long => AssemblyType.QuadWord,
			_ => throw new Exception(),
		};
	}
	
	public static AssemblyType ToAssemblyType(this PrimitiveType type)
	{
		return type switch
		{
			PrimitiveType.Integer => AssemblyType.LongWord,
			PrimitiveType.Long => AssemblyType.QuadWord,
			_ => throw new Exception(),
		};
	}
}