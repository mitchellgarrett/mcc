namespace FTG.Studios.MCC {
	
	public static partial class AssemblyNode {

		public abstract class Operand : Node {
			public abstract string Emit();
		}
		
		public class Register : Operand {
			public readonly RegisterType Value;
			
			public Register(RegisterType value) {
				Value = value;
			}
			
			public override string Emit() {
				return Value.Emit();
			}
			
			public override string ToString() {
				return $"Register({Value})";
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
			
			public override string ToString() {
				return $"Immediate({Value})";
			}
		}
		
		public class Variable : Operand {
			public readonly string Identifier;
			
			public Variable(string identifier) {
				Identifier = identifier;
			}
			
			public override string Emit() {
				return $"{Identifier}";
			}
			
			public override string ToString() {
				return $"Pseudo({Identifier})";
			}
		}
		
		public class StackAccess : Operand {
			public readonly int Offset;
			
			public StackAccess(int offset) {
				Offset = offset;
			}
			
			public override string Emit() {
				return $"{Offset}(%rbp)";
			}
			
			public override string ToString() {
				return $"Stack({Offset})";
			}
		}
	}
}