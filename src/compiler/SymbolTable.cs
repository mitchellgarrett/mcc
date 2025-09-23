using System.Collections.Generic;

namespace FTG.Studios.MCC
{

	public struct SymbolTableEntry
	{
		public SymbolTable.SymbolClass SymbolClass;
		public SymbolTable.Type ReturnType;
		public int ParamaterCount;
		public bool IsDefined;

		public SymbolTableEntry(SymbolTable.SymbolClass symbol_class, SymbolTable.Type return_type, bool is_defined, int parameter_count)
		{
			SymbolClass = symbol_class;
			ReturnType = return_type;
			IsDefined = is_defined;
			ParamaterCount = parameter_count;
		}
	}

	public class SymbolTable
	{
		public enum SymbolClass { Variable, Function };
		public enum Type { Integer };

		readonly Dictionary<string, SymbolTableEntry> symbols = new Dictionary<string, SymbolTableEntry>();

		public void AddVariable(string identifier, Type type, bool is_defined)
		{
			symbols[identifier] = new SymbolTableEntry(SymbolClass.Variable, type, is_defined, 0);
		}

		public void AddFunction(string identifier, Type type, bool is_defined, int parameter_count)
		{
			symbols[identifier] = new SymbolTableEntry(SymbolClass.Function, type, is_defined, parameter_count);
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
	}
}