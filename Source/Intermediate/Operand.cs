using System.Numerics;
using FTG.Studios.MCC.Parser;

namespace FTG.Studios.MCC.Intermediate;

public static partial class IntermediateNode
{

	public abstract class Operand : Node
	{
		public abstract string ToCommentString();
	}

	public class Constant(ParseNode.PrimitiveType type, BigInteger value) : Operand
	{
		public readonly ParseNode.PrimitiveType Type = type;
		public readonly BigInteger Value = value;

		public override string ToCommentString()
		{
			return Value.ToString();
		}

		public override string ToString()
		{
			return $"Constant({Type}, {Value})";
		}
	}

	public static Constant ToIntermediateConstant(this int value)
	{
		return new Constant(ParseNode.PrimitiveType.Integer, value);
	}
	
	public static Constant ToIntermediateConstant(this long value)
	{
		return new Constant(ParseNode.PrimitiveType.Long, value);
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