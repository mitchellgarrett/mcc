using FTG.Studios.MCC.Lexer;

namespace FTG.Studios.MCC.Intermediate;

public static partial class IntermediateNode
{

	public abstract class Instruction : Node { }

	public class ReturnInstruction : Instruction
	{
		public readonly Operand Value;

		public ReturnInstruction(Operand value)
		{
			Value = value;
		}

		public override string ToString()
		{
			return $"Return({Value})";
		}
	}

	public class Copy : Instruction
	{
		public readonly Operand Source;
		public readonly Operand Destination;

		public Copy(Operand source, Operand destination)
		{
			Source = source;
			Destination = destination;
		}

		public override string ToString()
		{
			return $"Copy({Source}, {Destination})";
		}
	}

	public class Jump : Instruction
	{
		public readonly string Target;

		public Jump(string target)
		{
			Target = target;
		}

		public override string ToString()
		{
			return $"Jump({Target})";
		}
	}

	public class JumpIfZero : Instruction
	{
		public readonly string Target;
		public readonly Operand Condition;

		public JumpIfZero(string target, Operand condition)
		{
			Target = target;
			Condition = condition;
		}

		public override string ToString()
		{
			return $"JumpIfZero({Target}, {Condition})";
		}
	}

	public class JumpIfNotZero : Instruction
	{
		public readonly string Target;
		public readonly Operand Condition;

		public JumpIfNotZero(string target, Operand condition)
		{
			Target = target;
			Condition = condition;
		}

		public override string ToString()
		{
			return $"JumpIfNotZero({Target}, {Condition})";
		}
	}

	public class Label : Instruction
	{
		public readonly string Identifier;

		public Label(string identifier)
		{
			Identifier = identifier;
		}

		public override string ToString()
		{
			return $"Label({Identifier})";
		}
	}

	public class UnaryInstruction : Instruction
	{
		public readonly Syntax.UnaryOperator Operator;
		public readonly Operand Source;
		public readonly Operand Destination;

		public UnaryInstruction(Syntax.UnaryOperator @operator, Operand source, Operand destination)
		{
			Operator = @operator;
			Source = source;
			Destination = destination;
		}

		public override string ToString()
		{
			return $"Unary({Operator}, {Source}, {Destination})";
		}
	}

	public class BinaryInstruction : Instruction
	{
		public readonly Syntax.BinaryOperator Operator;
		public readonly Operand LeftOperand;
		public readonly Operand RightOperand;
		public readonly Operand Destination;

		public BinaryInstruction(Syntax.BinaryOperator @operator, Operand left_operand, Operand right_operand, Operand destination)
		{
			Operator = @operator;
			LeftOperand = left_operand;
			RightOperand = right_operand;
			Destination = destination;
		}

		public override string ToString()
		{
			return $"Binary({Operator}, {LeftOperand}, {RightOperand}, {Destination})";
		}
	}

	public class FunctionCall : Instruction
	{
		public readonly string Identifier;
		public readonly Operand[] Arguments;
		public readonly Operand Destination;

		public FunctionCall(string identifier, Operand[] arguments, Operand destination)
		{
			Identifier = identifier;
			Arguments = arguments;
			Destination = destination;
		}
		
		public override string ToString()
		{
			return $"FunctionCall(Identifier=\"{Identifier}\", Arguments({string.Join<Operand>(", ", Arguments)}), Destination({Destination}))";
		}
	}
}