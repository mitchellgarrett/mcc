using System.Collections.Generic;

namespace FTG.Studios.MCC {
	
	public static partial class AssemblyNode {
		
		public abstract class Node { }
		
		public class Program : Node {
			public readonly Function Function;
			
			public Program(Function function) {
				Function = function;
			}
			
			public override string ToString() {
				return $"Program(\n{Function}".Replace("\n", "\n ") + "\n)";
			}
		}
		
		public class Function : Node {
			public readonly string Identifier;
			public List<List<Instruction>> Body;
			
			public Function(string identifier, List<List<Instruction>> body) {
				Identifier = identifier;
				Body = body;
			}
			
			public override string ToString() {
				string output = $"Function(\n Identifier=\"{Identifier}\"\n Body(\n  ";
				foreach (var instruction_list in Body) {
					foreach (var instruction in instruction_list)
					{
						output += (instruction.ToString() + '\n').Replace("\n", "\n  ");
					}
					output += '\n';
				}
				output += ")\n)"; 
				return output;
			}
		}		
	}
}