namespace FTG.Studios.MCC {
	
	public static class IntermediateNode {
		
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
			public readonly Instruction[] Body;
			
			public Function(string identifier, Instruction[] body) {
				Identifier = identifier;
				Body = body;
			}
			
			public override string ToString() {
				string output = $"Function(\n Identifier=\"{Identifier}\"\n Body(\n  ";
				foreach (var instruction in Body) {
					output += (instruction.ToString() + '\n').Replace("\n", "\n  ");
				}
				output += ")\n)"; 
				return output;
			}
		}
		
		public abstract class Operand : Node { }
		
		public class Constant : Operand {
			public readonly int Value;
			
			public Constant(int value) {
				Value = value;
			}
			
			public override string ToString() {
				return $"Constant({Value})";
			}
		}
		
		public class Variable : Operand {
			public readonly string Identifier;
			
			public Variable(string identifier) {
				Identifier = identifier;
			}
			
			public override string ToString() {
				return $"Variable(\"{Identifier}\")";
			}
		}
		
		public abstract class Instruction : Node { }
		
		public class ReturnInstruction : Instruction {
			public readonly Operand Value;
			
			public ReturnInstruction(Operand value) {
				Value = value;
			}
			
			public override string ToString() {
				return $"Return({Value})";
			}
		}
		
		public class UnaryInstruction : Instruction {
			public readonly Syntax.UnaryOperator Operator;
			public readonly Operand Source;
			public readonly Operand Destination;
			
			public UnaryInstruction(Syntax.UnaryOperator @operator, Operand source, Operand destination) {
				Operator = @operator;
				Source = source;
				Destination = destination;
			}
			
			public override string ToString() {
				return $"Unary({Operator}, {Source}, {Destination})";
			}
		}
	}
}