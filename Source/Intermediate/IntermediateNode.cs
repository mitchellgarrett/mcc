using System.Collections.Generic;

namespace FTG.Studios.MCC.Intermediate;

public static partial class IntermediateNode {
	
	public abstract class Node { }
	
	public class Program : Node {
		public readonly List<Function> Functions;
		
		public Program(List<Function> functions) {
			Functions = functions;
		}
		
		public override string ToString() {
			return $"Program(\n{string.Join(", ", Functions)}".Replace("\n", "\n ") + "\n)";
		}
	}
	
	public class Function : Node {
		public readonly string Identifier;
		public readonly Variable[] Parameters;
		public readonly Instruction[] Body;
		
		public Function(string identifier, Variable[] parameters, Instruction[] body) {
			Identifier = identifier;
			Parameters = parameters;
			Body = body;
		}
		
		public override string ToString() {
			string output = $"Function(\n Identifier=\"{Identifier}\"\n Parameters({string.Join<Variable>(", ", Parameters)})\n Body(\n  ";
			foreach (var instruction in Body) {
				output += (instruction.ToString() + '\n').Replace("\n", "\n  ");
			}
			output += ")\n)"; 
			return output;
		}
	}
	
	/// <summary>
	/// Node to store useful comment data that gets propogated to the final assembly file.
	/// </summary>
	public class Comment : Instruction {
		public readonly object Data;
		
		public Comment(object data) {
			Data = data;
		}

		public override string ToString()
		{
			return $"Comment(\"{Data}\")";
		}
	}
}