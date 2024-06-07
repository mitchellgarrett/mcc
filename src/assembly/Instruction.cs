namespace FTG.Studios.MCC {
	
	public static partial class AssemblyNode {
		
		public abstract class Instruction : Node {
			public abstract string Emit();
		}
		
		public class MOV : Instruction {
			public Operand Source;
			public Operand Destination;
			
			public MOV(Operand source, Operand destination) {
				Source = source;
				Destination = destination;
			}
			
			public override string Emit() {
				return $"movl {Source.Emit()}, {Destination.Emit()}";
			}
			
			public override string ToString() {
				return $"MOV({Source}, {Destination})";
			}
		}
		
		public class RET : Instruction {
			public override string Emit() {
				return "\nmovq %rbp, %rsp\npopq %rbp\nret";
			}
			
			public override string ToString() {
				return "RET";
			}
		}
		
		public class IDIV : Instruction {
			public Operand Operand;
			
			public IDIV(Operand operand) {
				Operand = operand;
			}
			
			public override string Emit() {
				return $"idivl {Operand.Emit()}";
			}
			
			public override string ToString() {
				return $"IDIV({Operand})";
			}
		}
		
		public class CDQ : Instruction {
			public override string Emit() {
				return "cdq";
			}
			
			public override string ToString() {
				return "CDQ";
			}
		}
		
		public class AllocateStackInstruction : Instruction {
			public int Offset;
			
			public AllocateStackInstruction(int offset) {
				Offset = offset;
			}
			
			public override string Emit() {
				return $"subq ${Offset}, %rsp";
			}
			
			public override string ToString() {
				return $"AllocateStack({Offset})";
			}
		}
		
		public class UnaryInstruction : Instruction {
			public readonly Syntax.UnaryOperator Operator;
			public Operand Operand;
			
			public UnaryInstruction(Syntax.UnaryOperator @operator, Operand operand) {
				Operator = @operator;
				Operand = operand;
			}
			
			public override string Emit() {
				switch (Operator)
				{
					case Syntax.UnaryOperator.BitwiseComplement: return $"notl {Operand.Emit()}";
					case Syntax.UnaryOperator.Negation: return $"negl {Operand.Emit()}";
				}
				return null;
			}
			
			public override string ToString() {
				return $"Unary({Operator}, {Operand})";
			}
		}
		
		public class BinaryInstruction : Instruction {
			public readonly Syntax.BinaryOperator Operator;
			public Operand Source;
			public Operand Destination;
			
			public BinaryInstruction(Syntax.BinaryOperator @operator, Operand source, Operand destination) {
				Operator = @operator;
				Source = source;
				Destination = destination;
			}
			
			public override string Emit() {
				return Operator switch
				{
					Syntax.BinaryOperator.Addition => $"addl {Source.Emit()}, {Destination.Emit()}",
					Syntax.BinaryOperator.Subtraction => $"subl {Source.Emit()}, {Destination.Emit()}",
					Syntax.BinaryOperator.Multiplication => $"imull {Source.Emit()}, {Destination.Emit()}",
					_ => null,
				};
			}
			
			public override string ToString() {
				return $"Binary({Operator}, {Source}, {Destination})";
			}
		}
	}
}