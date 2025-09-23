namespace FTG.Studios.MCC {

	public static partial class AssemblyNode
	{

		public abstract class Instruction : Node
		{
			public abstract string Emit();
		}

		public class MOV : Instruction
		{
			public Operand Source;
			public Operand Destination;

			public MOV(Operand source, Operand destination)
			{
				Source = source;
				Destination = destination;
			}

			public override string Emit()
			{
				return $"movl {Source.Emit()}, {Destination.Emit()}";
			}

			public override string ToString()
			{
				return $"MOV({Source}, {Destination})";
			}
		}

		public class RET : Instruction
		{
			public override string Emit()
			{
				return "\nmovq %rbp, %rsp\npopq %rbp\nret";
			}

			public override string ToString()
			{
				return "RET";
			}
		}

		public class IDIV : Instruction
		{
			public Operand Operand;

			public IDIV(Operand operand)
			{
				Operand = operand;
			}

			public override string Emit()
			{
				return $"idivl {Operand.Emit()}";
			}

			public override string ToString()
			{
				return $"IDIV({Operand})";
			}
		}

		public class CDQ : Instruction
		{
			public override string Emit()
			{
				return "cdq";
			}

			public override string ToString()
			{
				return "CDQ";
			}
		}

		public class CMP : Instruction
		{
			public Operand LeftOperand;
			public Operand RightOperand;

			public CMP(Operand left_operand, Operand right_operand)
			{
				LeftOperand = left_operand;
				RightOperand = right_operand;
			}

			public override string Emit()
			{
				return $"cmpl {LeftOperand.Emit()}, {RightOperand.Emit()}";
			}

			public override string ToString()
			{
				return $"CMP({LeftOperand.Emit()}, {RightOperand.Emit()})";
			}
		}

		public class JMP : Instruction
		{
			public readonly string Identifier;

			public JMP(string identifier)
			{
				Identifier = identifier;
			}

			public override string Emit()
			{
				return $"jmp {Identifier}";
			}

			public override string ToString()
			{
				return $"JMP({Identifier})";
			}
		}

		public class JMPCC : Instruction
		{
			public readonly string Identifier;
			public readonly ConditionType Condition;

			public JMPCC(string identifier, ConditionType condition)
			{
				Identifier = identifier;
				Condition = condition;
			}

			public override string Emit()
			{
				return $"j{Condition.ToString().ToLower()} {Identifier}";
			}

			public override string ToString()
			{
				return $"JMP({Condition}, {Identifier})";
			}
		}

		public class SETCC : Instruction
		{
			public Operand Operand;
			public readonly ConditionType Condition;

			public SETCC(Operand operand, ConditionType condition)
			{
				Operand = operand;
				Condition = condition;
			}

			public override string Emit()
			{
				return $"set{Condition.ToString().ToLower()} {Operand.Emit()}";
			}

			public override string ToString()
			{
				return $"SET({Condition}, {Operand})";
			}
		}

		public class Label : Instruction
		{
			public readonly string Identifier;

			public Label(string identifier)
			{
				Identifier = identifier;
			}

			public override string Emit()
			{
				return $"{Identifier}:";
			}

			public override string ToString()
			{
				return $"Label({Identifier})";
			}
		}

		public class AllocateStack : Instruction
		{
			public int Offset;

			public AllocateStack(int offset)
			{
				Offset = offset;
			}

			public override string Emit()
			{
				return $"subq ${Offset}, %rsp";
			}

			public override string ToString()
			{
				return $"AllocateStack({Offset})";
			}
		}

		public class DeallocateStack : Instruction
		{
			public int Offset;

			public DeallocateStack(int offset)
			{
				Offset = offset;
			}

			public override string Emit()
			{
				return $"addq ${Offset}, %rsp";
			}

			public override string ToString()
			{
				return $"DeallocateStack({Offset})";
			}
		}

		public class Unary : Instruction
		{
			public readonly Syntax.UnaryOperator Operator;
			public Operand Operand;

			public Unary(Syntax.UnaryOperator @operator, Operand operand)
			{
				Operator = @operator;
				Operand = operand;
			}

			public override string Emit()
			{
				switch (Operator)
				{
					case Syntax.UnaryOperator.BitwiseComplement: return $"notl {Operand.Emit()}";
					case Syntax.UnaryOperator.Negation: return $"negl {Operand.Emit()}";
				}
				return null;
			}

			public override string ToString()
			{
				return $"Unary({Operator}, {Operand})";
			}
		}

		public class Binary : Instruction
		{
			public readonly Syntax.BinaryOperator Operator;
			public Operand Source;
			public Operand Destination;

			public Binary(Syntax.BinaryOperator @operator, Operand source, Operand destination)
			{
				Operator = @operator;
				Source = source;
				Destination = destination;
			}

			public override string Emit()
			{
				return Operator switch
				{
					Syntax.BinaryOperator.Addition => $"addl {Source.Emit()}, {Destination.Emit()}",
					Syntax.BinaryOperator.Subtraction => $"subl {Source.Emit()}, {Destination.Emit()}",
					Syntax.BinaryOperator.Multiplication => $"imull {Source.Emit()}, {Destination.Emit()}",
					_ => null,
				};
			}

			public override string ToString()
			{
				return $"Binary({Operator}, {Source}, {Destination})";
			}
		}

		public class Push : Instruction
		{
			public Operand Operand;

			public Push(Operand operand)
			{
				Operand = operand;
			}

			public override string Emit()
			{
				// Push requires 64-bit values
				if (Operand is Register register)
					return $"pushq {register.Value.Emit64()}";
				return $"pushq {Operand.Emit()}";
			}

			public override string ToString()
			{
				return $"Push({Operand})";
			}
		}
		
		public class Call : Instruction
		{
			public string Identifier;

			public Call(string identifier)
			{
				Identifier = identifier;
			}
			
			public override string Emit()
			{
				return $"call {Identifier}";
			}

			public override string ToString()
			{
				return $"Call(\"{Identifier}\")";
			}
		}
	}
}