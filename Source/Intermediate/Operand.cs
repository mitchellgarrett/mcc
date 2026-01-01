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

	public abstract class Constant(PrimitiveType type) : Operand
	{
		public readonly PrimitiveType Type = type;
		
		public override PrimitiveType GetPrimitiveType(SymbolTable symbol_table)
		{
			return Type;
		}
	}
	
	public class IntegerConstant(PrimitiveType type, BigInteger value) : Constant(type)
	{
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
	
	public class FloatingPointConstant(double value) : Constant(PrimitiveType.Double)
	{
		public readonly double Value = value;
		
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

	public static IntegerConstant ToIntermediateConstant(this int value)
	{
		return new IntegerConstant(PrimitiveType.Integer, value);
	}
	
	public static IntegerConstant ToIntermediateConstant(this long value)
	{
		return new IntegerConstant(PrimitiveType.Long, value);
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