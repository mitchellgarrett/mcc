namespace FTG.Studios.MCC {
	
	public static partial class IntermediateNode {
		
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
	}
}