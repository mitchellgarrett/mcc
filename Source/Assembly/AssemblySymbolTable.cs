using System.Collections;
using System.Collections.Generic;

namespace FTG.Studios.MCC.Assembly;

public abstract class AssemblySymbolTableEntry;

public class ObjectEntry(AssemblyType type, bool is_static) : AssemblySymbolTableEntry
{
	public AssemblyType Type = type;
	public bool IsStatic = is_static;
}

public class FunctionEntry(bool is_defined) : AssemblySymbolTableEntry
{
	public bool IsDefined = is_defined;
}

public class AssemblySymbolTable : IEnumerable<(string, AssemblySymbolTableEntry)>
{
	readonly Dictionary<string, AssemblySymbolTableEntry> symbols = [];

	public void AddObject(string identifier, AssemblyType type, bool is_static)
	{
		symbols[identifier] = new ObjectEntry(type, is_static);
	}

	public void AddFunction(string identifier, bool is_defined)
	{
		symbols[identifier] = new FunctionEntry(is_defined);
	}

	public AssemblySymbolTableEntry GetSymbol(string identifier)
	{
		if (!symbols.TryGetValue(identifier, out AssemblySymbolTableEntry entry)) throw new System.Exception($"Identifier \"{identifier}\" is not defined.");
		return entry;
	}

	public bool TryGetSymbol(string identifier, out AssemblySymbolTableEntry entry)
	{
		return symbols.TryGetValue(identifier, out entry);
	}

	public bool ContainsSymbol(string identifier)
	{
		return symbols.ContainsKey(identifier);
	}

	public IEnumerator<(string, AssemblySymbolTableEntry)> GetEnumerator()
	{
		foreach (var pair in symbols) yield return (pair.Key, pair.Value);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}