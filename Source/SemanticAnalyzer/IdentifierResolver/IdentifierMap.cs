using System.Collections.Generic;

namespace FTG.Studios.MCC.SemanticAnalysis;

public struct IdentifierMapEntry(string unique_identifier, bool from_current_scope, bool has_linkage)
{
	public string UniqueIdentifier = unique_identifier;
	public bool FromCurrentScope = from_current_scope;
	public bool HasLinkage = has_linkage;

	public override readonly string ToString()
	{
		return $"IdentifierMapEntry(Identifier=\"{UniqueIdentifier}\", FromCurrentScope={FromCurrentScope}, HasLinkage={HasLinkage})";
	}
}

public class IdentifierMap
{
	static int next_temporary_identifier_index;

	readonly Dictionary<string, IdentifierMapEntry> data = [];

	public static void Reset()
	{
		next_temporary_identifier_index = 0;
	}

	public IdentifierMap Copy()
	{
		IdentifierMap new_map = new();
		foreach (KeyValuePair<string, IdentifierMapEntry> pair in data)
		{
			new_map.data[pair.Key] = new IdentifierMapEntry(pair.Value.UniqueIdentifier, false, pair.Value.HasLinkage);
		}
		return new_map;
	}

	public string InsertUniqueIdentifier(string original_identifier, bool has_linkage, SymbolTable.SymbolClass symbol_class)
	{
		if (data.TryGetValue(original_identifier, out IdentifierMapEntry entry))
		{
			// If this symbol has external linkage, duplicates are allowed
			if (has_linkage && entry.HasLinkage)
			{
				// Since thiis symbol was reinserted, mark it as from the current scope
				data[original_identifier] = new IdentifierMapEntry(entry.UniqueIdentifier, true, has_linkage);
				return entry.UniqueIdentifier;
			}
			// Otherwise, throw an error
			if (entry.FromCurrentScope) throw new SemanticAnalzyerException($"{symbol_class} \"{original_identifier}\" is already defined.", original_identifier);
		}

		// If this symbol has external linkage, no change is made to its identifier
		// Otherwise, create a unique identifier
		string unique_identifier = has_linkage ? original_identifier : $"{original_identifier}.{next_temporary_identifier_index++}";

		// Associate the original identifier with its unique companion
		data[original_identifier] = new IdentifierMapEntry(unique_identifier, true, has_linkage);

		return unique_identifier;
	}

	public string GetUniqueIdentifier(string original_identifier, SymbolTable.SymbolClass symbol_class)
	{
		if (data.TryGetValue(original_identifier, out IdentifierMapEntry entry)) return entry.UniqueIdentifier;
		throw new SemanticAnalzyerException($"{symbol_class} \"{original_identifier}\" is not defined.", original_identifier);
	}

	public bool ContainsIdentifier(string original_identifier)
	{
		return data.ContainsKey(original_identifier);
	}

	public bool TryGetUniqueIdentifier(string original_identifier, out IdentifierMapEntry entry)
	{
		return data.TryGetValue(original_identifier, out entry);
	}

	public override string ToString()
	{
		string output = "";
		foreach (var item in data)
		{
			output += $"{item.Key}: ({item.Value.UniqueIdentifier}, {item.Value.FromCurrentScope}, {item.Value.HasLinkage})\n";
		}
		return output;
	}
}