using System.Runtime.InteropServices;
using FTG.Studios.MCC.CodeGeneration;
using FTG.Studios.MCC.Lexer;

namespace FTG.Studios.MCC.Assembly;

public static partial class AssemblyNode
{

	public abstract class Instruction : Node
	{
		public abstract string Emit();
	}

	public class MOV(AssemblyType type, Operand source, Operand destination) : Instruction
	{
		public AssemblyType Type = type;
		public Operand Source = source;
		public Operand Destination = destination;

		public override string Emit()
		{
			return $"mov{Type.GetSuffix()} {Source.Emit(Type)}, {Destination.Emit(Type)}";
		}

		public override string ToString()
		{
			return $"MOV({Type}, {Source}, {Destination})";
		}
	}
	
	public class MOVSX(Operand source, Operand destination) : Instruction
	{
		public Operand Source = source;
		public Operand Destination = destination;

		public override string Emit()
		{
			return $"movslq {Source.Emit(AssemblyType.LongWord)}, {Destination.Emit(AssemblyType.QuadWord)}";
		}

		public override string ToString()
		{
			return $"MOVSX({Source}, {Destination})";
		}
	}
	
	public class MOVZ(Operand source, Operand destination) : Instruction
	{
		public Operand Source = source;
		public Operand Destination = destination;

		public override string Emit()
		{
			throw new System.Exception();
		}

		public override string ToString()
		{
			return $"MOVZ({Source}, {Destination})";
		}
	}

	public class RET : Instruction
	{
		public override string Emit()
		{
			return "\nmovq %rbp, %rsp\npopq %rbp\nret";
		}

		public override string ToString()
		{
			return "RET";
		}
	}

	public class IDIV(AssemblyType type, Operand operand) : Instruction
	{
		public AssemblyType Type = type;
		public Operand Operand = operand;

		public override string Emit()
		{
			return $"idiv{Type.GetSuffix()} {Operand.Emit(Type)}";
		}

		public override string ToString()
		{
			return $"IDIV({Operand})";
		}
	}
	
	public class DIV(AssemblyType type, Operand operand) : Instruction
	{
		public AssemblyType Type = type;
		public Operand Operand = operand;

		public override string Emit()
		{
			return $"div{Type.GetSuffix()} {Operand.Emit(Type)}";
		}

		public override string ToString()
		{
			return $"DIV({Operand})";
		}
	}

	public class CDQ(AssemblyType type) : Instruction
	{
		public AssemblyType Type = type;
		
		public override string Emit()
		{
			return Type switch
			{
				AssemblyType.LongWord => "cdq",
				AssemblyType.QuadWord => "cqo",
				_ => throw new System.Exception(),
			};
		}

		public override string ToString()
		{
			return "CDQ";
		}
	}

	public class CMP(AssemblyType type, Operand left_operand, Operand right_operand) : Instruction
	{
		public AssemblyType Type = type;
		public Operand LeftOperand = left_operand;
		public Operand RightOperand = right_operand;

		public override string Emit()
		{
			if (Type == AssemblyType.Double)
				return $"comisd {LeftOperand.Emit(Type)}, {RightOperand.Emit(Type)}";
			else
				return $"cmp{Type.GetSuffix()} {LeftOperand.Emit(Type)}, {RightOperand.Emit(Type)}";
		}

		public override string ToString()
		{
			return $"CMP({LeftOperand.Emit(Type)}, {RightOperand.Emit(Type)})";
		}
	}

	public class JMP(string identifier) : Instruction
	{
		public readonly string Identifier = identifier;

		public override string Emit()
		{
			return $"jmp {Identifier}";
		}

		public override string ToString()
		{
			return $"JMP({Identifier})";
		}
	}

	public class JMPCC(string identifier, ConditionType condition) : Instruction
	{
		public readonly string Identifier = identifier;
		public readonly ConditionType Condition = condition;

		public override string Emit()
		{
			return $"j{Condition.ToString().ToLower()} {Identifier}";
		}

		public override string ToString()
		{
			return $"JMP({Condition}, {Identifier})";
		}
	}

	public class SETCC(Operand operand, ConditionType condition) : Instruction
	{
		public Operand Operand = operand;
		public readonly ConditionType Condition = condition;

		public override string Emit()
		{
			return $"set{Condition.ToString().ToLower()} {Operand.Emit(AssemblyType.LongWord)}";
		}

		public override string ToString()
		{
			return $"SET({Condition}, {Operand})";
		}
	}

	public class Label(string identifier) : Instruction
	{
		public readonly string Identifier = identifier;

		public override string Emit()
		{
			return $"{Identifier}:";
		}

		public override string ToString()
		{
			return $"Label({Identifier})";
		}
	}

	public class Unary(AssemblyType type, Syntax.UnaryOperator @operator, Operand operand) : Instruction
	{
		public AssemblyType Type = type;
		public readonly Syntax.UnaryOperator Operator = @operator;
		public Operand Operand = operand;

		public override string Emit()
		{
			return Operator switch
			{
				Syntax.UnaryOperator.BitwiseComplement => $"not{Type.GetSuffix()} {Operand.Emit(Type)}",
				Syntax.UnaryOperator.Negation => $"neg{Type.GetSuffix()} {Operand.Emit(Type)}",
				Syntax.UnaryOperator.ShiftRight => $"shr {Operand.Emit(Type)}",
				_ => null,
			};
		}

		public override string ToString()
		{
			return $"Unary({Operator}, {Operand})";
		}
	}

	public class Binary(AssemblyType type, Syntax.BinaryOperator @operator, Operand source, Operand destination) : Instruction
	{
		public AssemblyType Type = type;
		public readonly Syntax.BinaryOperator Operator = @operator;
		public Operand Source = source;
		public Operand Destination = destination;

		public override string Emit()
		{
			return Operator switch
			{
				Syntax.BinaryOperator.Addition => $"add{Type.GetSuffix()} {Source.Emit(Type)}, {Destination.Emit(Type)}",
				Syntax.BinaryOperator.Subtraction => $"sub{Type.GetSuffix()} {Source.Emit(Type)}, {Destination.Emit(Type)}",
				Syntax.BinaryOperator.Multiplication => Type == AssemblyType.Double ? $"mulsd {Source.Emit(Type)}, {Destination.Emit(Type)}" : $"imul{Type.GetSuffix()} {Source.Emit(Type)}, {Destination.Emit(Type)}",
				// This should only be floating point division
				Syntax.BinaryOperator.Division => $"imul{Type.GetSuffix()} {Source.Emit(Type)}, {Destination.Emit(Type)}",
				Syntax.BinaryOperator.BitwiseAnd => $"and {Source.Emit(Type)}, {Destination.Emit(Type)}",
				Syntax.BinaryOperator.BitwiseOr => $"or {Source.Emit(Type)}, {Destination.Emit(Type)}",
				Syntax.BinaryOperator.ExclusiveOr => $"xorpd {Source.Emit(Type)}, {Destination.Emit(Type)}",
				_ => null,
			};
		}

		public override string ToString()
		{
			return $"Binary({Operator}, {Source}, {Destination})";
		}
	}

	public class Push(Operand operand) : Instruction
	{
		public Operand Operand = operand;

		public override string Emit()
		{
			// Push requires 64-bit values
			return $"pushq {Operand.Emit(AssemblyType.QuadWord)}";
		}

		public override string ToString()
		{
			return $"Push({Operand})";
		}
	}
	
	public class Call(string identifier, bool is_defined) : Instruction
	{
		public readonly string Identifier = identifier;
		public readonly bool IsDefined = is_defined;

		public override string Emit()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return $"call {CodeEmitter.macos_function_prefix}{Identifier}";
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !IsDefined) return $"call {Identifier}{CodeEmitter.linux_plt_suffix}";
			return $"call {Identifier}";
		}

		public override string ToString()
		{
			return $"Call(\"{Identifier}\")";
		}
	}
	
	public class CVTTSD2SI(AssemblyType type, Operand source, Operand destination) : Instruction
	{
		public readonly AssemblyType DestinationType = type;
		public Operand Source = source;
		public Operand Destination = destination;

		public override string Emit()
		{
			return $"cvtsi2sd{DestinationType.GetSuffix()} {Source.Emit(DestinationType)}, {Destination.Emit(DestinationType)}";
		}

		public override string ToString()
		{
			return $"CVTTSD2SI({DestinationType}, {Source}, {Destination})";
		}
	}
	
	public class CVTSI2SD(AssemblyType type, Operand source, Operand destination) : Instruction
	{
		public readonly AssemblyType SourceType = type;
		public Operand Source = source;
		public Operand Destination = destination;

		public override string Emit()
		{
			return $"cvtsisd{SourceType.GetSuffix()} {Source.Emit(SourceType)}, {Destination.Emit(SourceType)}";
		}

		public override string ToString()
		{
			return $"CVTSI2SD({SourceType}, {Source}, {Destination})";
		}
	}
}