namespace FTG.Studios.MCC {
	
	public static partial class IntermediateNode {
		
		public abstract class Operand : Node {
			public abstract string ToCommentString();
		}
		
		public class Constant : Operand {
			public readonly int Value;
			
			public Constant(int value) {
				Value = value;
			}

			public override string ToCommentString()
			{
				return Value.ToString();
			}

			public override string ToString() {
				return $"Constant({Value})";
			}
		}
		
		public static Constant ToIntermediateConstant(this int value) {
			return new Constant(value);
		}
		
		public class Variable : Operand {
			public readonly string Identifier;
			
			public Variable(string identifier) {
				Identifier = identifier;
			}
			
			public override string ToCommentString()
			{
				return Identifier;
			}
			
			public override string ToString() {
				return $"Variable(\"{Identifier}\")";
			}
		}
	}
}