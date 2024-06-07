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
		
		public class BinaryInstruction : Instruction {
			public readonly Syntax.BinaryOperator Operator;
			public readonly Operand LeftOperand;
			public readonly Operand RightOperand;
			public readonly Operand Destination;
			
			public BinaryInstruction(Syntax.BinaryOperator @operator, Operand left_operand, Operand right_operand, Operand destination) {
				Operator = @operator;
				LeftOperand = left_operand;
				RightOperand = right_operand;
				Destination = destination;
			}
			
			public override string ToString() {
				return $"Binary({Operator}, {LeftOperand}, {RightOperand}, {Destination})";
			}
		}
	}
}