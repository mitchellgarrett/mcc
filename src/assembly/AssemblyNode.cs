using System.Collections.Generic;

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
			public List<Instruction> Body;
			
			public Function(string identifier, List<Instruction> body) {
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
		
		public abstract class Operand : Node {
			public abstract string Emit();
		}
		
		public class Register : Operand {
			public readonly RegisterType Value;
			
			public Register(RegisterType value) {
				Value = value;
			}
			
			public override string Emit() {
				return "%eax";
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
				return $"Mov({Source}, {Destination})";
			}
		}
		
		public class RET : Instruction {
			public override string Emit() {
				return "\nmovq %rbp, %rsp\npopq %rbp\nret";
			}
			
			public override string ToString() {
				return "Ret";
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
					case Syntax.UnaryOperator.Negate: return $"negl {Operand.Emit()}";
				}
				return null;
			}
			
			public override string ToString() {
				return $"Unary({Operator}, {Operand})";
			}
		}
	}
}