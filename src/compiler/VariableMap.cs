using System.Collections.Generic;

namespace FTG.Studios.MCC
{
	public struct VariableMapEntry
	{
		public string UniqueIdentifier;
		public bool FromCurrentBlock;

		public VariableMapEntry(string unique_identifier, bool from_current_block)
		{
			UniqueIdentifier = unique_identifier;
			FromCurrentBlock = from_current_block;
		}
	}

	public class VariableMap
	{
		static int next_temporary_variable_index;
		
		public readonly Dictionary<string, VariableMapEntry> Data = new Dictionary<string, VariableMapEntry>();

		public static void Reset()
		{
			next_temporary_variable_index = 0;
		}

		public VariableMap Copy()
		{
			VariableMap new_map = new VariableMap();
			foreach (KeyValuePair<string, VariableMapEntry> pair in Data)
			{
				new_map.Data[pair.Key] = new VariableMapEntry(pair.Value.UniqueIdentifier, false);
			}
			return new_map;
		}
		
		public string InsertUniqueIdentifier(string original_identifier)
		{
			if (Data.TryGetValue(original_identifier, out VariableMapEntry entry))
			{
				if (entry.FromCurrentBlock) throw new SemanticAnalzyerException($"Variable \"{original_identifier}\" is already defined.", original_identifier);	
			}
			
			string unique_identifier = $"{original_identifier}.{next_temporary_variable_index++}";
			Data[original_identifier] = new VariableMapEntry(unique_identifier, true);
			return unique_identifier;
		}

		public string GetUniqueIdentifier(string original_identifier)
		{
			if (Data.TryGetValue(original_identifier, out VariableMapEntry entry)) return entry.UniqueIdentifier;
			throw new SemanticAnalzyerException($"Variable \"{original_identifier}\" is not defined.", original_identifier);
		}
	}
}