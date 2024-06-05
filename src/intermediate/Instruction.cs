namespace FTG.Studios.MCC {
	
	public static partial class IntermediateNode {

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