using System.Collections;
using System.Collections.Generic;
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

	public class Constant(int value) : InitialValue
	{
		public int Value = value;
	}
}

public struct SymbolTableEntry(SymbolTable.SymbolClass symbol_class, ParseNode.PrimitiveType return_type, int parameter_count, IdentifierAttributes attributes)
{
	public SymbolTable.SymbolClass SymbolClass = symbol_class;
	public ParseNode.PrimitiveType ReturnType = return_type;
	public int ParamaterCount = parameter_count;
	public IdentifierAttributes Attributes = attributes;
}

public class SymbolTable : IEnumerable<(string, SymbolTableEntry)>
{
	public enum SymbolClass { Variable, Function };

	readonly Dictionary<string, SymbolTableEntry> symbols = [];

	public void AddVariable(string identifier, ParseNode.PrimitiveType type, IdentifierAttributes attributes)
	{
		symbols[identifier] = new SymbolTableEntry(SymbolClass.Variable, type, 0, attributes);
	}

	public void AddFunction(string identifier, ParseNode.PrimitiveType type, int parameter_count, IdentifierAttributes attributes)
	{
		symbols[identifier] = new SymbolTableEntry(SymbolClass.Function, type, parameter_count, attributes);
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