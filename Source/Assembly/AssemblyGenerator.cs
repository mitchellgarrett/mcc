using System;
using System.Collections.Generic;
using System.Linq;
using FTG.Studios.MCC.Intermediate;
using FTG.Studios.MCC.Lexer;
using FTG.Studios.MCC.Parser;
using FTG.Studios.MCC.SemanticAnalysis;

namespace FTG.Studios.MCC.Assembly;

public static class AssemblyGenerator {
	
	// TODO: Make this a symbol table that can produce the same identifier for constants of the same value
	static int next_constant_label_index;
	static string NextConstantLabel {
		get { return $"C{next_constant_label_index++}"; }
	}
	
	public static AssemblyTree Generate(IntermediateTree tree, SymbolTable symbol_table) {
		next_constant_label_index = 0;
		AssemblyNode.Program program = GenerateProgram(tree.Program, symbol_table);
		return new AssemblyTree(program);
	}
	
	static AssemblyNode.Program GenerateProgram(IntermediateNode.Program program, SymbolTable symbol_table) {
		List<AssemblyNode.TopLevel> definitions = [];
		List<AssemblyNode.StaticConstant> constants = [];
		foreach (var definition in program.TopLevelDefinitions) definitions.Add(GenerateTopLevelDefinition(definition, constants, symbol_table));
		definitions.AddRange(constants);
		return new AssemblyNode.Program(definitions);
	}

	static AssemblyNode.TopLevel GenerateTopLevelDefinition(IntermediateNode.TopLevel definition, List<AssemblyNode.StaticConstant> constants, SymbolTable symbol_table)
	{
		if (definition is IntermediateNode.Function function) return GenerateFunctionDefinition(function, constants, symbol_table);
		if (definition is IntermediateNode.StaticVariable static_variable) return GenerateStaticVariable(static_variable, symbol_table);
		throw new Exception();
	}
	
	static AssemblyNode.Function GenerateFunctionDefinition(IntermediateNode.Function function, List<AssemblyNode.StaticConstant> constants, SymbolTable symbol_table)
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
		
		(var integer_register_arguments, var double_register_arguments, var stack_arguments) = ClassifyFunctionParameters(constants, symbol_table, function.Parameters);

		// Copy first 6 integer arguments into general-purpose registers
		for (int i = 0; i < integer_register_arguments.Count; i++)
		{
			(var type, var operand) = integer_register_arguments[i];
			instructions.Add(
				new AssemblyNode.MOV(
					type,
					RegisterTypeExtensions.integer_function_call_order[i].ToOperand(),
					operand
				)
			);
		}
		
		// Copy first 8 double arguments into floating pont registers
		for (int i = 0; i < double_register_arguments.Count; i++)
		{
			var operand = double_register_arguments[i];
			instructions.Add(
				new AssemblyNode.MOV(
					AssemblyType.Double,
					RegisterTypeExtensions.double_function_call_order[i].ToOperand(),
					operand
				)
			);
		}

		// Copy the rest of the arguments from the stack
		int stack_offset = 16;
		for (int i = 0; i < stack_arguments.Count; i++)
		{
			(var type, var operand) = stack_arguments[i];
			instructions.Add(new AssemblyNode.MOV(type, new AssemblyNode.StackAccess(stack_offset), operand));
			stack_offset += 8;
		}

		foreach (var instruction in function.Body) GenerateInstruction(instructions, constants, symbol_table, instruction);

		return new AssemblyNode.Function(identifier, function.IsGlobal, parameters.ToArray(), instructions);
	}

	static AssemblyNode.StaticVariable GenerateStaticVariable(IntermediateNode.StaticVariable variable, SymbolTable symbol_table)
	{
		if (!symbol_table.TryGetSymbol(variable.Identifier, out SymbolTableEntry entry)) throw new Exception();
		int alignment = entry.ReturnType.ToAssemblyType().GetSize();
		return new AssemblyNode.StaticVariable(variable.Identifier, variable.IsGlobal, alignment, variable.InitialValue);
	}

	static void GenerateInstruction(List<AssemblyNode.Instruction> instructions, List<AssemblyNode.StaticConstant> constants, SymbolTable symbol_table, IntermediateNode.Instruction instruction)
	{
		if (instruction is IntermediateNode.Comment comment) GenerateComment(instructions, comment);
		if (instruction is IntermediateNode.Return @return) GenerateReturnInstruction(instructions, constants, symbol_table, @return);
		if (instruction is IntermediateNode.UnaryInstruction unary) GenerateUnaryInstruction(instructions, constants, symbol_table, unary);
		if (instruction is IntermediateNode.BinaryInstruction binary) GenerateBinaryInstruction(instructions, constants, symbol_table, binary);
		if (instruction is IntermediateNode.Jump jump) GenerateJumpInstruction(instructions, jump);
		if (instruction is IntermediateNode.JumpIfZero jump_if_zero) GenerateJumpIfZeroInstruction(instructions, constants, symbol_table, jump_if_zero);
		if (instruction is IntermediateNode.JumpIfNotZero jump_if_not_zero) GenerateJumpIfNotZeroInstruction(instructions, constants, symbol_table, jump_if_not_zero);
		if (instruction is IntermediateNode.Copy copy) GenerateCopyInstruction(instructions, constants, symbol_table, copy);
		if (instruction is IntermediateNode.Label label) GenerateLabel(instructions, label);
		if (instruction is IntermediateNode.FunctionCall function_call) GenerateFunctionCall(instructions, constants, symbol_table, function_call);
		if (instruction is IntermediateNode.SignExtend sign_extend) GenerateSignExtend(instructions, constants, sign_extend);
		if (instruction is IntermediateNode.ZeroExtend zero_extend) GenerateZeroExtend(instructions, constants, zero_extend);
		if (instruction is IntermediateNode.Truncate truncate) GenerateTruncate(instructions, constants, truncate);
		if (instruction is IntermediateNode.IntegerToDouble int2double) GenerateIntegerToDouble(instructions, constants, symbol_table, int2double);
		if (instruction is IntermediateNode.DoubleToInteger double2int) GenerateDoubleToInteger(instructions, constants, symbol_table, double2int);
		if (instruction is IntermediateNode.UnsignedIntegerToDouble uint2double) GenerateUnsgnedIntegerToDouble(instructions, constants, symbol_table, uint2double);
		if (instruction is IntermediateNode.DoubleToUnsignedInteger double2uint) GenerateDoubleToUnsignedInteger(instructions, constants, symbol_table, double2uint);
	}
	
	static void GenerateComment(List<AssemblyNode.Instruction> instructions, IntermediateNode.Comment comment) {
		instructions.Add(new AssemblyNode.Comment(comment.Data));
	}
	
	static void GenerateReturnInstruction(List<AssemblyNode.Instruction> instructions, List<AssemblyNode.StaticConstant> constants, SymbolTable symbol_table, IntermediateNode.Return instruction) {
		AssemblyNode.Operand source = GenerateOperand(constants, instruction.Value);
		var type = instruction.Value.ToAssemblyType(symbol_table);
		AssemblyNode.Operand destination = type == AssemblyType.Double ? RegisterType.XMM0.ToOperand() : RegisterType.AX.ToOperand();
		
		instructions.Add(
			new AssemblyNode.MOV(
				type,
				source, 
				destination
			)
		);
		instructions.Add(new AssemblyNode.RET());
	}
	
	static void GenerateUnaryInstruction(List<AssemblyNode.Instruction> instructions, List<AssemblyNode.StaticConstant> constants, SymbolTable symbol_table, IntermediateNode.UnaryInstruction instruction) {
		if (instruction.Operator == Syntax.UnaryOperator.Not) {
			GenerateNotInstruction(instructions, constants, symbol_table, instruction);
			return;
		}
		
		AssemblyNode.Operand source = GenerateOperand(constants, instruction.Source);
		AssemblyNode.Operand destination = GenerateOperand(constants, instruction.Destination);
		var type = instruction.Source.ToAssemblyType(symbol_table);
		
		if (instruction.Operator == Syntax.UnaryOperator.Negation && type == AssemblyType.Double)
		{
			string negative_zero = NextConstantLabel;
			constants.Add(new AssemblyNode.StaticConstant(negative_zero, 16, new InitialValue.FloatingPointConstant(double.NegativeZero)));
			instructions.Add(new AssemblyNode.MOV(AssemblyType.Double, source, destination));
			instructions.Add(new AssemblyNode.Binary(AssemblyType.Double, Syntax.BinaryOperator.ExclusiveOr, new AssemblyNode.ConstantAccess(negative_zero), destination));
			return;
		}
		
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
	
	static void GenerateNotInstruction(List<AssemblyNode.Instruction> instructions, List<AssemblyNode.StaticConstant> constants, SymbolTable symbol_table, IntermediateNode.UnaryInstruction instruction)
	{
		if (instruction.Operator != Syntax.UnaryOperator.Not) throw new Exception();
		
		// If it is a double comparison then we have too zero out a register using XOR
		if (instruction.Source.ToAssemblyType(symbol_table) == AssemblyType.Double)
		{
			// Copy code from GenerateConditional
			// TODO: Find a better way to handle this
			instructions.Add(
				new AssemblyNode.Binary(AssemblyType.Double, Syntax.BinaryOperator.ExclusiveOr, RegisterType.XMM0.ToOperand(), RegisterType.XMM0.ToOperand())
			);
			var result = GenerateOperand(constants, instruction.Destination);
			instructions.Add(
				new AssemblyNode.CMP(
					AssemblyType.Double,
					GenerateOperand(constants, instruction.Source), 
					RegisterType.XMM0.ToOperand()
				)
			);
			instructions.Add(
				new AssemblyNode.MOV(
					instruction.Destination.ToAssemblyType(symbol_table),
					0.ToAssemblyImmediate(),
					result
				)
			);
			instructions.Add(new AssemblyNode.SETCC(result, ConditionType.E));
			return;
		}
		
		// If this is not a floating point operation just compare the source value to 0
		// The source value has to be the left operand so the GenerateConditional function will pick the right type
		GenerateConditional(instructions, constants, symbol_table, instruction.Source, 0.ToIntermediateConstant(), instruction.Destination, ConditionType.E);
		return;
	}

	static void GenerateBinaryInstruction(List<AssemblyNode.Instruction> instructions, List<AssemblyNode.StaticConstant> constants, SymbolTable symbol_table, IntermediateNode.BinaryInstruction instruction)
	{
		bool is_signed = instruction.LeftOperand.GetPrimitiveType(symbol_table).IsSigned();
		bool is_double = instruction.LeftOperand.ToAssemblyType(symbol_table) == AssemblyType.Double;
		switch (instruction.Operator)
		{
			case Syntax.BinaryOperator.Addition:
			case Syntax.BinaryOperator.Subtraction:
			case Syntax.BinaryOperator.Multiplication:
				GenerateSimpleBinaryInstruction(instructions, constants, symbol_table, instruction);
				return;

			case Syntax.BinaryOperator.Division:
				// Floating point division can be handled by a normal binary operation
				if (is_double)
					GenerateSimpleBinaryInstruction(instructions, constants, symbol_table, instruction);
				// Integer division must be handled by a special case
				else
					GenerateIntegerDivision(instructions, constants, symbol_table, instruction);
				return;

			case Syntax.BinaryOperator.Remainder:
				GenerateIntegerRemainder(instructions, constants, symbol_table, instruction);
				return;

			case Syntax.BinaryOperator.LogicalLess:
				GenerateConditional(instructions, constants, symbol_table, instruction.LeftOperand, instruction.RightOperand, instruction.Destination, (is_signed && !is_double) ? ConditionType.L : ConditionType.B);
				return;

			case Syntax.BinaryOperator.LogicalGreater:
				GenerateConditional(instructions, constants, symbol_table, instruction.LeftOperand, instruction.RightOperand, instruction.Destination, (is_signed && !is_double) ? ConditionType.G : ConditionType.A);
				return;

			case Syntax.BinaryOperator.LogicalLessEqual:
				GenerateConditional(instructions, constants, symbol_table, instruction.LeftOperand, instruction.RightOperand, instruction.Destination, (is_signed && !is_double) ? ConditionType.LE : ConditionType.BE);
				return;

			case Syntax.BinaryOperator.LogicalGreaterEqual:
				GenerateConditional(instructions, constants, symbol_table, instruction.LeftOperand, instruction.RightOperand, instruction.Destination, (is_signed && !is_double) ? ConditionType.GE : ConditionType.AE);
				return;

			case Syntax.BinaryOperator.LogicalEqual:
				GenerateConditional(instructions, constants, symbol_table, instruction.LeftOperand, instruction.RightOperand, instruction.Destination, ConditionType.E);
				return;
			
			case Syntax.BinaryOperator.LogicalNotEqual:
				GenerateConditional(instructions, constants, symbol_table, instruction.LeftOperand, instruction.RightOperand, instruction.Destination, ConditionType.NE);
				return;
		}

		throw new Exception();
	}
	
	static void GenerateSimpleBinaryInstruction(List<AssemblyNode.Instruction> instructions, List<AssemblyNode.StaticConstant> constants, SymbolTable symbol_table, IntermediateNode.BinaryInstruction instruction) {
		AssemblyNode.Operand lhs = GenerateOperand(constants, instruction.LeftOperand);
		AssemblyNode.Operand rhs = GenerateOperand(constants, instruction.RightOperand);
		AssemblyNode.Operand destination = GenerateOperand(constants, instruction.Destination);
		
		AssemblyType type = instruction.LeftOperand.ToAssemblyType(symbol_table);
		
		instructions.Add(new AssemblyNode.MOV(type, lhs, destination));
		instructions.Add(new AssemblyNode.Binary(type, instruction.Operator, rhs, destination));
	}
	
	static void GenerateIntegerDivision(List<AssemblyNode.Instruction> instructions, List<AssemblyNode.StaticConstant> constants, SymbolTable symbol_table, IntermediateNode.BinaryInstruction instruction) {
		if (instruction.LeftOperand.GetPrimitiveType(symbol_table).IsSigned())
			GenerateSignedIntegerDivision(instructions, constants, symbol_table, instruction);
		else
			GenerateUnsignedIntegerDivision(instructions, constants, symbol_table, instruction);
	}
	
	static void GenerateSignedIntegerDivision(List<AssemblyNode.Instruction> instructions, List<AssemblyNode.StaticConstant> constants, SymbolTable symbol_table, IntermediateNode.BinaryInstruction instruction)
	{
		AssemblyNode.Operand lhs = GenerateOperand(constants, instruction.LeftOperand);
		AssemblyNode.Operand rhs = GenerateOperand(constants, instruction.RightOperand);
		AssemblyNode.Operand destination = GenerateOperand(constants, instruction.Destination);
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
		
	static void GenerateUnsignedIntegerDivision(List<AssemblyNode.Instruction> instructions, List<AssemblyNode.StaticConstant> constants, SymbolTable symbol_table, IntermediateNode.BinaryInstruction instruction) 
	{
		AssemblyNode.Operand lhs = GenerateOperand(constants, instruction.LeftOperand);
		AssemblyNode.Operand rhs = GenerateOperand(constants, instruction.RightOperand);
		AssemblyNode.Operand destination = GenerateOperand(constants, instruction.Destination);
		var type = instruction.LeftOperand.ToAssemblyType(symbol_table);
		
		instructions.Add(
			new AssemblyNode.MOV(
				type,
				lhs, 
				RegisterType.AX.ToOperand()
			)
		);
		instructions.Add(
			new AssemblyNode.MOV(
				type,
				0.ToAssemblyImmediate(), 
				RegisterType.DX.ToOperand()
			)
		);
		instructions.Add(
			new AssemblyNode.DIV(
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

	
	static void GenerateIntegerRemainder(List<AssemblyNode.Instruction> instructions, List<AssemblyNode.StaticConstant> constants, SymbolTable symbol_table, IntermediateNode.BinaryInstruction instruction) {
		if (instruction.LeftOperand.GetPrimitiveType(symbol_table).IsSigned())
			GenerateSignedIntegerRemainder(instructions, constants, symbol_table, instruction);
		else
			GenerateUnsignedIntegerRemainder(instructions, constants, symbol_table, instruction);
	}
	
	static void GenerateSignedIntegerRemainder(List<AssemblyNode.Instruction> instructions, List<AssemblyNode.StaticConstant> constants, SymbolTable symbol_table, IntermediateNode.BinaryInstruction instruction) {
		AssemblyNode.Operand lhs = GenerateOperand(constants, instruction.LeftOperand);
		AssemblyNode.Operand rhs = GenerateOperand(constants, instruction.RightOperand);
		AssemblyNode.Operand destination = GenerateOperand(constants, instruction.Destination);
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
	
	static void GenerateUnsignedIntegerRemainder(List<AssemblyNode.Instruction> instructions, List<AssemblyNode.StaticConstant> constants, SymbolTable symbol_table, IntermediateNode.BinaryInstruction instruction) {
		AssemblyNode.Operand lhs = GenerateOperand(constants, instruction.LeftOperand);
		AssemblyNode.Operand rhs = GenerateOperand(constants, instruction.RightOperand);
		AssemblyNode.Operand destination = GenerateOperand(constants, instruction.Destination);
		var type = instruction.LeftOperand.ToAssemblyType(symbol_table);
		
		instructions.Add(
			new AssemblyNode.MOV(
				type,
				lhs, 
				RegisterType.AX.ToOperand()
			)
		);
		instructions.Add(
			new AssemblyNode.MOV(
				type,
				0.ToAssemblyImmediate(),
				RegisterType.DX.ToOperand()
			)
		);
		instructions.Add(
			new AssemblyNode.DIV(
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
	
	static void GenerateConditional(List<AssemblyNode.Instruction> instructions, List<AssemblyNode.StaticConstant> constants, SymbolTable symbol_table, IntermediateNode.Operand left_operand, IntermediateNode.Operand right_operand, IntermediateNode.Operand destination, ConditionType condition) {
		var result = GenerateOperand(constants, destination);
		instructions.Add(
			new AssemblyNode.CMP(
				left_operand.ToAssemblyType(symbol_table),
				GenerateOperand(constants, right_operand), 
				GenerateOperand(constants, left_operand)
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
	
	static void GenerateJumpIfZeroInstruction(List<AssemblyNode.Instruction> instructions, List<AssemblyNode.StaticConstant> constants, SymbolTable symbol_table, IntermediateNode.JumpIfZero instruction) {
		// If this is an integer comparison we can use an immediate with value '0'
		AssemblyNode.Operand zero = 0.ToAssemblyImmediate();
		// If it is a double comparison then we have to zero out a register using XOR
		if (instruction.Condition.ToAssemblyType(symbol_table) == AssemblyType.Double)
		{
			instructions.Add(
				new AssemblyNode.Binary(AssemblyType.Double, Syntax.BinaryOperator.ExclusiveOr, RegisterType.XMM0.ToOperand(), RegisterType.XMM0.ToOperand())
			);
			zero = RegisterType.XMM0.ToOperand();
		}
		
		instructions.Add(
			new AssemblyNode.CMP(
				instruction.Condition.ToAssemblyType(symbol_table),
				zero, 
				GenerateOperand(constants, instruction.Condition)
			)
		);
		instructions.Add(
			new AssemblyNode.JMPCC(
				instruction.Target, 
				ConditionType.E
			)
		);
	}
	
	static void GenerateJumpIfNotZeroInstruction(List<AssemblyNode.Instruction> instructions, List<AssemblyNode.StaticConstant> constants, SymbolTable symbol_table, IntermediateNode.JumpIfNotZero instruction) {
		// If this is an integer comparison we can use an immediate with value '0'
		AssemblyNode.Operand zero = 0.ToAssemblyImmediate();
		// If it is a double comparison then we have to zero out a register using XOR
		if (instruction.Condition.ToAssemblyType(symbol_table) == AssemblyType.Double)
		{
			instructions.Add(
				new AssemblyNode.Binary(AssemblyType.Double, Syntax.BinaryOperator.ExclusiveOr, RegisterType.XMM0.ToOperand(), RegisterType.XMM0.ToOperand())
			);
			zero = RegisterType.XMM0.ToOperand();
		}
		
		instructions.Add(
			new AssemblyNode.CMP(
				instruction.Condition.ToAssemblyType(symbol_table),
				zero, 
				GenerateOperand(constants, instruction.Condition)
			)
		);
		instructions.Add(
			new AssemblyNode.JMPCC(
				instruction.Target,
				ConditionType.NE
			)
		);
	}
	
	static void GenerateCopyInstruction(List<AssemblyNode.Instruction> instructions, List<AssemblyNode.StaticConstant> constants, SymbolTable symbol_table, IntermediateNode.Copy instruction) {
		instructions.Add(
			new AssemblyNode.MOV(
				instruction.Source.ToAssemblyType(symbol_table),
				GenerateOperand(constants, instruction.Source), 
				GenerateOperand(constants, instruction.Destination)
			)
		);
	}
	
	static void GenerateLabel(List<AssemblyNode.Instruction> instructions, IntermediateNode.Label instruction) {
		instructions.Add(new AssemblyNode.Label(instruction.Identifier));
	}

	static void GenerateFunctionCall(List<AssemblyNode.Instruction> instructions, List<AssemblyNode.StaticConstant> constants, SymbolTable symbol_table, IntermediateNode.FunctionCall function_call)
	{
		(var integer_register_arguments, var double_register_arguments, var stack_arguments) = ClassifyFunctionParameters(constants, symbol_table, function_call.Arguments);

		// Ensure stack is 16-byte aligned
		int stack_padding = 0;
		if (stack_arguments.Count % 2 != 0)
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

		// Copy first 6 integer arguments into general-purpose registers
		for (int i = 0; i < integer_register_arguments.Count; i++)
		{
			(var type, var operand) = integer_register_arguments[i];
			instructions.Add(
				new AssemblyNode.MOV(
					type,
					operand,
					RegisterTypeExtensions.integer_function_call_order[i].ToOperand()
				)
			);
		}
		
		// Copy first 8 double arguments into floating pont registers
		for (int i = 0; i < double_register_arguments.Count; i++)
		{
			var operand = double_register_arguments[i];
			instructions.Add(
				new AssemblyNode.MOV(
					AssemblyType.Double,
					operand,
					RegisterTypeExtensions.double_function_call_order[i].ToOperand()
				)
			);
		}

		// The rest of the arguments are pushed to the stack in reverse order
		for (int i = stack_arguments.Count - 1; i >= 0; i--)
		{
			stack_padding += 8;
			(var type, var operand) = stack_arguments[i];
			if (type.GetSize() == 8)
			{
				instructions.Add(new AssemblyNode.Push(operand));
			} 
			else
			{
				instructions.Add(new AssemblyNode.MOV(type, operand, RegisterType.AX.ToOperand()));
				instructions.Add(new AssemblyNode.Push(RegisterType.AX.ToOperand()));
			}
		}

		instructions.Add(new AssemblyNode.Call(function_call.Identifier, symbol_table.ContainsSymbol(function_call.Identifier)));

		// Restore stack pointer
		if (stack_padding != 0) instructions.Add(new AssemblyNode.Binary(AssemblyType.QuadWord, Syntax.BinaryOperator.Addition, stack_padding.ToAssemblyImmediate(), RegisterType.SP.ToOperand()));

		// Retrieve return value
		var return_type = function_call.Destination.ToAssemblyType(symbol_table);
		if (return_type == AssemblyType.Double)
		{
			instructions.Add(
				new AssemblyNode.MOV(
					AssemblyType.Double, 
					RegisterType.XMM0.ToOperand(), 
					GenerateOperand(constants, function_call.Destination)
				)
			);
		} else {
			instructions.Add(
				new AssemblyNode.MOV(
					return_type, 
					RegisterType.AX.ToOperand(), 
					GenerateOperand(constants, function_call.Destination)
				)
			);
		}
	}
	
	static void GenerateSignExtend(List<AssemblyNode.Instruction> instructions, List<AssemblyNode.StaticConstant> constants, IntermediateNode.SignExtend sign_extend)
	{
		instructions.Add(
			new AssemblyNode.MOVSX(
				GenerateOperand(constants, sign_extend.Source),
				GenerateOperand(constants, sign_extend.Destination)
				)
			);
	}
	
	static void GenerateZeroExtend(List<AssemblyNode.Instruction> instructions, List<AssemblyNode.StaticConstant> constants, IntermediateNode.ZeroExtend zero_extend)
	{
		instructions.Add(
			new AssemblyNode.MOVZ(
				GenerateOperand(constants, zero_extend.Source),
				GenerateOperand(constants, zero_extend.Destination)
				)
			);
	}
	
	static void GenerateTruncate(List<AssemblyNode.Instruction> instructions, List<AssemblyNode.StaticConstant> constants, IntermediateNode.Truncate truncate)
	{
		// Pre-truncate the int-to-long conversion
		var source = GenerateOperand(constants, truncate.Source);
		if (source is AssemblyNode.Immediate immediate)
			if (immediate.Value > uint.MaxValue) source = ((uint)(ulong)immediate.Value).ToAssemblyImmediate();
			else if (immediate.Value > int.MaxValue) source = ((int)(long)immediate.Value).ToAssemblyImmediate();
		
		instructions.Add(
			new AssemblyNode.MOV(
				AssemblyType.LongWord,
				source,
				GenerateOperand(constants, truncate.Destination)
			)
		);
	}
	
	static void GenerateIntegerToDouble(List<AssemblyNode.Instruction> instructions, List<AssemblyNode.StaticConstant> constants, SymbolTable symbol_table, IntermediateNode.IntegerToDouble instruction)
	{
		instructions.Add(
			new AssemblyNode.CVTSI2SD(
				instruction.Source.ToAssemblyType(symbol_table), 
				GenerateOperand(constants, instruction.Source), 
				GenerateOperand(constants, instruction.Destination)
			)
		);
	}
	
	static void GenerateDoubleToInteger(List<AssemblyNode.Instruction> instructions, List<AssemblyNode.StaticConstant> constants, SymbolTable symbol_table, IntermediateNode.DoubleToInteger instruction)
	{
		instructions.Add(
			new AssemblyNode.CVTTSD2SI(
				instruction.Destination.ToAssemblyType(symbol_table), 
				GenerateOperand(constants, instruction.Source), 
				GenerateOperand(constants, instruction.Destination)
			)
		);
	}
	
	static void GenerateUnsgnedIntegerToDouble(List<AssemblyNode.Instruction> instructions, List<AssemblyNode.StaticConstant> constants, SymbolTable symbol_table, IntermediateNode.UnsignedIntegerToDouble instruction)
	{
		if (instruction.Source.ToAssemblyType(symbol_table) == AssemblyType.LongWord)
		{
			instructions.Add(
				new AssemblyNode.MOVZ(
					GenerateOperand(constants, instruction.Source), 
					RegisterType.DX.ToOperand()
				)
			);
			instructions.Add(
				new AssemblyNode.CVTSI2SD(
					AssemblyType.QuadWord, 
					RegisterType.DX.ToOperand(), 
					GenerateOperand(constants, instruction.Destination)
				)
			);
			return;
		}
		
		instructions.Add(
			new AssemblyNode.CMP(
				AssemblyType.QuadWord,
				0.ToAssemblyImmediate(),
				GenerateOperand(constants, instruction.Source)
			)
		);
		var label1 = IntermediateGenerator.NextTemporaryLabel;
		instructions.Add(new AssemblyNode.JMPCC(label1.Identifier, ConditionType.L));
		instructions.Add(
			new AssemblyNode.CVTSI2SD(
				AssemblyType.QuadWord,
				GenerateOperand(constants, instruction.Source),
				GenerateOperand(constants, instruction.Destination)
			)
		);
		var label2 = IntermediateGenerator.NextTemporaryLabel;
		instructions.Add(new AssemblyNode.JMP(label2.Identifier));
		instructions.Add(new AssemblyNode.Label(label1.Identifier));
		instructions.Add(
			new AssemblyNode.MOV(
				AssemblyType.QuadWord,
				GenerateOperand(constants, instruction.Source),
				RegisterType.DX.ToOperand()
			)
		);
		instructions.Add(
			new AssemblyNode.MOV(
				AssemblyType.QuadWord,
				RegisterType.DX.ToOperand(),
				RegisterType.CX.ToOperand()
			)
		);
		instructions.Add(
			new AssemblyNode.Unary(
				AssemblyType.QuadWord,
				Syntax.UnaryOperator.ShiftRight,
				RegisterType.CX.ToOperand()
			)
		);
		instructions.Add(
			new AssemblyNode.Binary(
				AssemblyType.QuadWord,
				Syntax.BinaryOperator.BitwiseAnd,
				1.ToAssemblyImmediate(),
				RegisterType.DX.ToOperand()
			)
		);
		instructions.Add(
			new AssemblyNode.Binary(
				AssemblyType.QuadWord,
				Syntax.BinaryOperator.BitwiseOr,
				RegisterType.DX.ToOperand(),
				RegisterType.CX.ToOperand()
			)
		);
		instructions.Add(
			new AssemblyNode.CVTSI2SD(
				AssemblyType.QuadWord,
				RegisterType.CX.ToOperand(),
				GenerateOperand(constants, instruction.Destination)
			)
		);
		instructions.Add(
			new AssemblyNode.Binary(
				AssemblyType.Double,
				Syntax.BinaryOperator.Addition,
				GenerateOperand(constants, instruction.Destination),
				GenerateOperand(constants, instruction.Destination)
			)
		);
		instructions.Add(new AssemblyNode.Label(label2.Identifier));
	}
	
	static void GenerateDoubleToUnsignedInteger(List<AssemblyNode.Instruction> instructions, List<AssemblyNode.StaticConstant> constants, SymbolTable symbol_table, IntermediateNode.DoubleToUnsignedInteger instruction)
	{
		if (instruction.Source.ToAssemblyType(symbol_table) == AssemblyType.LongWord)
		{
			instructions.Add(
				new AssemblyNode.CVTTSD2SI(
					AssemblyType.QuadWord, 
					GenerateOperand(constants, instruction.Source), 
					RegisterType.DX.ToOperand()
				)
			);
			instructions.Add(
				new AssemblyNode.MOV(
					AssemblyType.LongWord,
					RegisterType.DX.ToOperand(),
					GenerateOperand(constants, instruction.Destination)
				)
			);
			return;
		}
		
		var upper_bound = NextConstantLabel;
		constants.Add(new AssemblyNode.StaticConstant(upper_bound, 8, new InitialValue.FloatingPointConstant(9223372036854775808.0)));
		instructions.Add(
			new AssemblyNode.CMP(
				AssemblyType.Double,
				new AssemblyNode.ConstantAccess(upper_bound),
				GenerateOperand(constants, instruction.Source)
			)
		);
		var label1 = IntermediateGenerator.NextTemporaryLabel;
		instructions.Add(new AssemblyNode.JMPCC(label1.Identifier, ConditionType.AE));
		instructions.Add(
			new AssemblyNode.CVTTSD2SI(
				AssemblyType.QuadWord,
				GenerateOperand(constants, instruction.Source),
				GenerateOperand(constants, instruction.Destination)
			)
		);
		var label2 = IntermediateGenerator.NextTemporaryLabel;
		instructions.Add(new AssemblyNode.JMP(label2.Identifier));
		instructions.Add(new AssemblyNode.Label(label1.Identifier));
		instructions.Add(
			new AssemblyNode.MOV(
				AssemblyType.Double,
				GenerateOperand(constants, instruction.Source),
				RegisterType.XMM7.ToOperand()
			)
		);
		instructions.Add(
			new AssemblyNode.Binary(
				AssemblyType.Double,
				Syntax.BinaryOperator.Subtraction,
				new AssemblyNode.ConstantAccess(upper_bound),
				RegisterType.XMM7.ToOperand()
			)
		);
		instructions.Add(
			new AssemblyNode.CVTTSD2SI(
				AssemblyType.QuadWord,
				RegisterType.XMM7.ToOperand(),
				GenerateOperand(constants, instruction.Destination)
			)
		);
		instructions.Add(
			new AssemblyNode.MOV(
				AssemblyType.QuadWord,
				9223372036854775808.ToAssemblyImmediate(),
				RegisterType.DX.ToOperand()
			)
		);
		instructions.Add(
			new AssemblyNode.Binary(
				AssemblyType.QuadWord,
				Syntax.BinaryOperator.Addition,
				RegisterType.DX.ToOperand(),
				GenerateOperand(constants, instruction.Destination)
			)
		);
		instructions.Add(new AssemblyNode.Label(label2.Identifier));
	}
	
	static AssemblyNode.Operand GenerateOperand(List<AssemblyNode.StaticConstant> constants, IntermediateNode.Operand operand)
	{
		if (operand is IntermediateNode.IntegerConstant integer) return GenerateIntegerConstant(integer);
		if (operand is IntermediateNode.FloatingPointConstant @double) return GenerateFloatingPointConstant(constants, @double);
		if (operand is IntermediateNode.Variable variable) return GenerateVariable(variable);
		throw new Exception();
	}
	
	static AssemblyNode.Immediate GenerateIntegerConstant(IntermediateNode.IntegerConstant constant) {
		return new AssemblyNode.Immediate(constant.Value);
	}
	
	static AssemblyNode.ConstantAccess GenerateFloatingPointConstant(List<AssemblyNode.StaticConstant> constants, IntermediateNode.FloatingPointConstant constant) {
		var label = NextConstantLabel;
		constants.Add(new AssemblyNode.StaticConstant(label, 8, new InitialValue.FloatingPointConstant(constant.Value)));
		return new AssemblyNode.ConstantAccess(label);
	}
	
	static AssemblyNode.PseudoRegister GenerateVariable(IntermediateNode.Variable operand) {
		return new AssemblyNode.PseudoRegister(operand.Identifier);
	}
	
	static (List<(AssemblyType, AssemblyNode.Operand)>, List<AssemblyNode.Operand>, List<(AssemblyType, AssemblyNode.Operand)>) ClassifyFunctionParameters(List<AssemblyNode.StaticConstant> constants, SymbolTable symbol_table, IntermediateNode.Operand[] parameters)
	{
		List<(AssemblyType, AssemblyNode.Operand)> integer_register_arguments = [];
		List<AssemblyNode.Operand> double_register_arguments = [];
		List<(AssemblyType, AssemblyNode.Operand)> stack_arguments = [];
		
		foreach (var parameter in parameters)
		{
			var operand = GenerateOperand(constants, parameter);
			var type = parameter.ToAssemblyType(symbol_table);
			
			if (type == AssemblyType.Double)
			{
				if (double_register_arguments.Count < 8) double_register_arguments.Add(operand);
				else stack_arguments.Add((AssemblyType.Double, operand));
			} else
			{
				if (integer_register_arguments.Count < 6) integer_register_arguments.Add((type, operand));
				else stack_arguments.Add((type, operand));
			}
		}
		
		return (integer_register_arguments, double_register_arguments, stack_arguments);
	}
	
	public static AssemblyType ToAssemblyType(this IntermediateNode.Operand operand, SymbolTable symbol_table)
	{
		if (operand is IntermediateNode.Constant constant) return constant.GetPrimitiveType(symbol_table).ToAssemblyType();
		if (operand is IntermediateNode.Variable variable)
		{
			if (symbol_table.TryGetSymbol(variable.Identifier, out SymbolTableEntry entry))
				return entry.ReturnType.ToAssemblyType();
		}
		throw new Exception();
	}
	
	public static AssemblyType ToAssemblyType(this PrimitiveType type)
	{
		return type switch
		{
			PrimitiveType.Integer => AssemblyType.LongWord,
			PrimitiveType.Long => AssemblyType.QuadWord,
			PrimitiveType.UnsignedInteger => AssemblyType.LongWord,
			PrimitiveType.UnsignedLong => AssemblyType.QuadWord,
			PrimitiveType.Double => AssemblyType.Double,
			_ => throw new Exception(),
		};
	}
}