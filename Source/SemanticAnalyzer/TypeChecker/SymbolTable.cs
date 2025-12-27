using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using FTG.Studios.MCC.Parser;

namespace FTG.Studios.MCC.SemanticAnalysis;

public abstract class IdentifierAttributes
{
	public class Function(bool is_defined, bool is_global) : IdentifierAttributes
	{
		public bool IsDefined = is_defined;
		public bool IsGlobal = is_global;
	}

	public class Static(InitialValue value, bool is_global) : IdentifierAttributes
	{
		public InitialValue InitialValue = value;
		public bool IsGlobal = is_global;
	}

	public class Local : IdentifierAttributes { }
}

public abstract class InitialValue
{
	public class None : InitialValue { }
	public class Tentative : InitialValue { }
	
	public class Constant(ParseNode.PrimitiveType type, BigInteger value) : InitialValue
	{
		public readonly ParseNode.PrimitiveType Type = type;
		public readonly BigInteger Value = value;
	}
}

public abstract class SymbolTableEntry(IdentifierAttributes attributes, ParseNode.PrimitiveType return_type)
{
	public IdentifierAttributes Attributes = attributes;
	public ParseNode.PrimitiveType ReturnType = return_type;
}

public class VariableEntry(IdentifierAttributes attributes, ParseNode.PrimitiveType return_type) : SymbolTableEntry(attributes, return_type);

public class FunctionEntry(IdentifierAttributes attributes, ParseNode.PrimitiveType return_type, List<ParseNode.PrimitiveType> parameter_types) : SymbolTableEntry(attributes, return_type)
{
	public List<ParseNode.PrimitiveType> ParameterTypes = parameter_types;
}

public class SymbolTable : IEnumerable<(string, SymbolTableEntry)>
{
	public enum SymbolClass { Variable, Function };

	readonly Dictionary<string, SymbolTableEntry> symbols = [];

	public void AddVariable(string identifier, IdentifierAttributes attributes, ParseNode.PrimitiveType type)
	{
		symbols[identifier] = new VariableEntry(attributes, type);
	}

	public void AddFunction(string identifier, IdentifierAttributes attributes, ParseNode.PrimitiveType return_type, List<ParseNode.PrimitiveType> parameter_types)
	{
		symbols[identifier] = new FunctionEntry(attributes, return_type, parameter_types);
	}

	public SymbolTableEntry GetSymbol(string identifier)
	{
		if (!symbols.TryGetValue(identifier, out SymbolTableEntry entry)) throw new SemanticAnalzyerException($"Identifier \"{identifier}\" is not defined.", identifier);
		return entry;
	}

	public bool TryGetSymbol(string identifier, out SymbolTableEntry entry)
	{
		return symbols.TryGetValue(identifier, out entry);
	}

	public bool ContainsSymbol(string identifier)
	{
		return symbols.ContainsKey(identifier);
	}

	public IEnumerator<(string, SymbolTableEntry)> GetEnumerator()
	{
		foreach (var pair in symbols) yield return (pair.Key, pair.Value);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}