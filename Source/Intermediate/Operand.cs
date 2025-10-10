namespace FTG.Studios.MCC.Intermediate;

public static partial class IntermediateNode
{

	public abstract class Operand : Node
	{
		public abstract string ToCommentString();
	}

	public class Constant(int value) : Operand
	{
		public readonly int Value = value;

		public override string ToCommentString()
		{
			return Value.ToString();
		}

		public override string ToString()
		{
			return $"Constant({Value})";
		}
	}

	public static Constant ToIntermediateConstant(this int value)
	{
		return new Constant(value);
	}

	public class Variable(string identifier) : Operand
	{
		public readonly string Identifier = identifier;

		public override string ToCommentString()
		{
			return Identifier;
		}

		public override string ToString()
		{
			return $"Variable(\"{Identifier}\")";
		}
	}
}