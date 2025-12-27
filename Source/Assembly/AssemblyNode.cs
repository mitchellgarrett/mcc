using System.Collections.Generic;
using FTG.Studios.MCC.SemanticAnalysis;

namespace FTG.Studios.MCC.Assembly;

public static partial class AssemblyNode {
	
	public abstract class Node { }
	
	public class Program(List<TopLevel> definitions) : Node {
		public readonly List<TopLevel> TopLevelDefinitions = definitions;

		public override string ToString() {
			return $"Program(\n{string.Join(", ", TopLevelDefinitions)}".Replace("\n", "\n ") + "\n)";
		}
	}
	
	public abstract class TopLevel : Node;
	
	public class Function(string identifier, bool is_global, Operand[] parameters, List<Instruction> body) : TopLevel
	{
		public readonly string Identifier = identifier;
		public readonly bool IsGlobal = is_global;
		public readonly Operand[] Parameters = parameters;
		public List<Instruction> Body = body;

		public override string ToString()
		{
			string output = $"Function(\n Identifier=\"{Identifier}\", Global={IsGlobal},\n Body(\n  ";
			foreach (var instruction in Body)
			{
				output += (instruction.ToString() + '\n').Replace("\n", "\n  ");
			}
			output += ")\n)";
			return output;
		}
	}
	
	public class StaticVariable(string identifier, bool is_global, int alignment, InitialValue.Constant initial_value) : TopLevel
	{
		public readonly string Identifier = identifier;
		public readonly bool IsGlobal = is_global;
		public readonly int Alignment = alignment;
		public readonly InitialValue.Constant InitialValue = initial_value;

		public override string ToString()
		{
			return $"StaticVariable(\"{Identifier}\", Global={IsGlobal}, InitialValue={InitialValue.Value})";
		}
	}
	
	/// <summary>
	/// Node to store useful comment data.
	/// </summary>
	public class Comment(object data) : Instruction
	{
		public readonly object Data = data;

		public override string Emit()
		{
			return '\n' + $"// {Data}".Replace("\n", "\n// ");
		}

		public override string ToString()
		{
			return $"Comment(\"{Data}\")";
		}
	}
}