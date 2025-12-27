using FTG.Studios.MCC.Lexer;

namespace FTG.Studios.MCC.Intermediate;

public static partial class IntermediateNode
{

	public abstract class Instruction : Node { }

	public class Return(Operand value) : Instruction
	{
		public readonly Operand Value = value;

		public override string ToString()
		{
			return $"Return({Value})";
		}
	}
	
	public class SignExtend(Operand source, Operand destination) : Instruction
	{
		public readonly Operand Source = source;
		public readonly Operand Destination = destination;

		public override string ToString()
		{
			return $"SignExtend({Source}, {Destination})";
		}
	}
	
	public class Truncate(Operand source, Operand destination) : Instruction
	{
		public readonly Operand Source = source;
		public readonly Operand Destination = destination;

		public override string ToString()
		{
			return $"Truncate({Source}, {Destination})";
		}
	}

	public class Copy(Operand source, Operand destination) : Instruction
	{
		public readonly Operand Source = source;
		public readonly Operand Destination = destination;

		public override string ToString()
		{
			return $"Copy({Source}, {Destination})";
		}
	}

	public class Jump(string target) : Instruction
	{
		public readonly string Target = target;

		public override string ToString()
		{
			return $"Jump({Target})";
		}
	}

	public class JumpIfZero(string target, Operand condition) : Instruction
	{
		public readonly string Target = target;
		public readonly Operand Condition = condition;

		public override string ToString()
		{
			return $"JumpIfZero({Target}, {Condition})";
		}
	}

	public class JumpIfNotZero(string target, Operand condition) : Instruction
	{
		public readonly string Target = target;
		public readonly Operand Condition = condition;

		public override string ToString()
		{
			return $"JumpIfNotZero({Target}, {Condition})";
		}
	}

	public class Label(string identifier) : Instruction
	{
		public readonly string Identifier = identifier;

		public override string ToString()
		{
			return $"Label({Identifier})";
		}
	}

	public class UnaryInstruction(Syntax.UnaryOperator @operator, Operand source, Operand destination) : Instruction
	{
		public readonly Syntax.UnaryOperator Operator = @operator;
		public readonly Operand Source = source;
		public readonly Operand Destination = destination;

		public override string ToString()
		{
			return $"Unary({Operator}, {Source}, {Destination})";
		}
	}

	public class BinaryInstruction(Syntax.BinaryOperator @operator, Operand left_operand, Operand right_operand, Operand destination) : Instruction
	{
		public readonly Syntax.BinaryOperator Operator = @operator;
		public readonly Operand LeftOperand = left_operand;
		public readonly Operand RightOperand = right_operand;
		public readonly Operand Destination = destination;

		public override string ToString()
		{
			return $"Binary({Operator}, {LeftOperand}, {RightOperand}, {Destination})";
		}
	}

	public class FunctionCall(string identifier, Operand[] arguments, Operand destination) : Instruction
	{
		public readonly string Identifier = identifier;
		public readonly Operand[] Arguments = arguments;
		public readonly Operand Destination = destination;

		public override string ToString()
		{
			return $"FunctionCall(Identifier=\"{Identifier}\", Arguments({string.Join<Operand>(", ", Arguments)}), Destination({Destination}))";
		}
	}
}