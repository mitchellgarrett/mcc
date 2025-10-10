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

	public class MOV(Operand source, Operand destination) : Instruction
	{
		public Operand Source = source;
		public Operand Destination = destination;

		public override string Emit()
		{
			return $"movl {Source.Emit()}, {Destination.Emit()}";
		}

		public override string ToString()
		{
			return $"MOV({Source}, {Destination})";
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

	public class IDIV(Operand operand) : Instruction
	{
		public Operand Operand = operand;

		public override string Emit()
		{
			return $"idivl {Operand.Emit()}";
		}

		public override string ToString()
		{
			return $"IDIV({Operand})";
		}
	}

	public class CDQ : Instruction
	{
		public override string Emit()
		{
			return "cdq";
		}

		public override string ToString()
		{
			return "CDQ";
		}
	}

	public class CMP(Operand left_operand, Operand right_operand) : Instruction
	{
		public Operand LeftOperand = left_operand;
		public Operand RightOperand = right_operand;

		public override string Emit()
		{
			return $"cmpl {LeftOperand.Emit()}, {RightOperand.Emit()}";
		}

		public override string ToString()
		{
			return $"CMP({LeftOperand.Emit()}, {RightOperand.Emit()})";
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
			return $"set{Condition.ToString().ToLower()} {Operand.Emit()}";
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

	public class AllocateStack(int offset) : Instruction
	{
		public int Offset = offset;

		public override string Emit()
		{
			return $"subq ${Offset}, %rsp";
		}

		public override string ToString()
		{
			return $"AllocateStack({Offset})";
		}
	}

	public class DeallocateStack(int offset) : Instruction
	{
		public int Offset = offset;

		public override string Emit()
		{
			return $"addq ${Offset}, %rsp";
		}

		public override string ToString()
		{
			return $"DeallocateStack({Offset})";
		}
	}

	public class Unary(Syntax.UnaryOperator @operator, Operand operand) : Instruction
	{
		public readonly Syntax.UnaryOperator Operator = @operator;
		public Operand Operand = operand;

		public override string Emit()
		{
			switch (Operator)
			{
				case Syntax.UnaryOperator.BitwiseComplement: return $"notl {Operand.Emit()}";
				case Syntax.UnaryOperator.Negation: return $"negl {Operand.Emit()}";
			}
			return null;
		}

		public override string ToString()
		{
			return $"Unary({Operator}, {Operand})";
		}
	}

	public class Binary(Syntax.BinaryOperator @operator, Operand source, Operand destination) : Instruction
	{
		public readonly Syntax.BinaryOperator Operator = @operator;
		public Operand Source = source;
		public Operand Destination = destination;

		public override string Emit()
		{
			return Operator switch
			{
				Syntax.BinaryOperator.Addition => $"addl {Source.Emit()}, {Destination.Emit()}",
				Syntax.BinaryOperator.Subtraction => $"subl {Source.Emit()}, {Destination.Emit()}",
				Syntax.BinaryOperator.Multiplication => $"imull {Source.Emit()}, {Destination.Emit()}",
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
			if (Operand is Register register)
				return $"pushq {register.Value.Emit64()}";
			return $"pushq {Operand.Emit()}";
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
}