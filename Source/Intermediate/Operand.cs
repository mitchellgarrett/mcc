using System.Numerics;
using FTG.Studios.MCC.Parser;
using FTG.Studios.MCC.SemanticAnalysis;

namespace FTG.Studios.MCC.Intermediate;

public static partial class IntermediateNode
{

	public abstract class Operand : Node
	{
		public abstract PrimitiveType GetPrimitiveType(SymbolTable symbol_table);
		public abstract string ToCommentString();
	}

	public class Constant(PrimitiveType type, BigInteger value) : Operand
	{
		public readonly PrimitiveType Type = type;
		public readonly BigInteger Value = value;
		
		public override PrimitiveType GetPrimitiveType(SymbolTable symbol_table)
		{
			return Type;
		}

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
		return new Constant(PrimitiveType.Integer, value);
	}
	
	public static Constant ToIntermediateConstant(this long value)
	{
		return new Constant(PrimitiveType.Long, value);
	}

	public class Variable(string identifier) : Operand
	{
		public readonly string Identifier = identifier;
		
		public override PrimitiveType GetPrimitiveType(SymbolTable symbol_table)
		{
			if (!symbol_table.TryGetSymbol(Identifier, out SymbolTableEntry entry)) throw new System.Exception();
			return entry.ReturnType;
		}

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