namespace FTG.Studios.MCC {
	
	public static class AssemblyNode {
		
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
			
			/*public override string ToString() {
				return $"Function(\nIdentifier=\"{Identifier}\"\nBody={Body}".Replace("\n", "\n ") + "\n)";
			}*/
		}
		
		public abstract class Operand : Node {
			public abstract string Emit();
		}
		
		public class Register : Operand {
			public readonly string Value;
			
			public Register(string value) {
				Value = value;
			}
			
			public override string Emit() {
				return "%eax";
			}
		}
		
		public class Immediate : Operand {
			public readonly int Value;
			
			public Immediate(int value) {
				Value = value;
			}
			
			public override string Emit() {
				return $"${Value}";
			}
		}
		
		public abstract class Instruction : Node {
			public abstract string Emit();
		}
		
		public class MOV : Instruction {
			public readonly Operand Source;
			public readonly Operand Destination;
			
			public MOV(Operand source, Operand destination) {
				Source = source;
				Destination = destination;
			}
			
			public override string Emit() {
				return $"movl {Source.Emit()}, {Destination.Emit()}";
			}
		}
		
		public class RET : Instruction {
			public override string Emit() {
				return $"ret";
			}
		}
	}
}