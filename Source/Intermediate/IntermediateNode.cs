using System.Collections.Generic;
using FTG.Studios.MCC.SemanticAnalysis;

namespace FTG.Studios.MCC.Intermediate;

public static partial class IntermediateNode {
	
	public abstract class Node { }
	
	public class Program(List<TopLevel> definitions) : Node {
		public readonly List<TopLevel> TopLevelDefinitions = definitions;

		public override string ToString() {
			return $"Program(\n{string.Join(", ", TopLevelDefinitions)}".Replace("\n", "\n ") + "\n)";
		}
	}
	
	public abstract class TopLevel : Node;
	
	public class Function(string identifier, bool is_global, Variable[] parameters, Instruction[] body) : TopLevel
	{
		public readonly string Identifier = identifier;
		public readonly bool IsGlobal = is_global;
		public readonly Variable[] Parameters = parameters;
		public readonly Instruction[] Body = body;

		public override string ToString()
		{
			string output = $"Function(\n Identifier=\"{Identifier}\",Global={IsGlobal},\n Parameters({string.Join<Variable>(", ", Parameters)})\n Body(\n  ";
			foreach (var instruction in Body)
			{
				output += (instruction.ToString() + '\n').Replace("\n", "\n  ");
			}
			output += ")\n)";
			return output;
		}
	}
	
	public class StaticVariable(string identifier, bool is_global, InitialValue.Constant initial_value) : TopLevel
	{
		public readonly string Identifier = identifier;
		public readonly bool IsGlobal = is_global;
		public readonly InitialValue.Constant InitialValue = initial_value;

		public override string ToString()
		{
			return $"StaticVariable(\"{Identifier}\", Global={IsGlobal}, InitialValue={InitialValue})";
		}
	}
	
	/// <summary>
	/// Node to store useful comment data that gets propogated to the final assembly file.
	/// </summary>
	public class Comment(object data) : Instruction
	{
		public readonly object Data = data;

		public override string ToString()
		{
			return $"Comment(\"{Data}\")";
		}
	}
}