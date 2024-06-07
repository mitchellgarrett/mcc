namespace FTG.Studios.MCC {
	
	public static class ParseNode { 
		
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
			public readonly Identifier Identifier;
			public readonly Statement Body;
			
			public Function(Identifier identifier, Statement body) {
				Identifier = identifier;
				Body = body;
			}
			
			public override string ToString() {
				return $"Function(\nIdentifier=\"{Identifier}\"\nBody(\n{Body}\n)".Replace("\n", "\n ") + "\n)";
			}
		}
		
		public class Statement : Node { }
		
		public class ReturnStatement : Statement {
			public readonly Expression Expression;
			
			public ReturnStatement(Expression expression) {
				Expression = expression;
			}
			
			public override string ToString() {
				return $"Return(\n{Expression}".Replace("\n", "\n ") + "\n)";
			}
		}
		
		public class Expression : Node { }
		
		public class BinaryExpression : Expression {
			public readonly Syntax.BinaryOperator Operator;
			public readonly Expression LeftExpression;
			public readonly Expression RightExpression;
			
			public BinaryExpression(Syntax.BinaryOperator @operator, Expression left_expression, Expression right_expression) {
				Operator = @operator;
				LeftExpression = left_expression;
				RightExpression = right_expression;
			}
			
			public override string ToString() {
				return $"Unary({Operator}, {LeftExpression})".Replace("\n", "\n ");
			}
		}
		
		public class Factor : Expression { }
		
		public class ConstantExpression : Factor {
			public readonly int Value;
			
			public ConstantExpression(int value) {
				Value = value;
			}
			
			public override string ToString() {
				return $"Constant({Value})".Replace("\n", "\n ");
			}
		}
		
		public class UnaryExpression : Factor {
			public readonly Syntax.UnaryOperator Operator;
			public readonly Expression Expression;
			
			public UnaryExpression(Syntax.UnaryOperator @operator, Expression expression) {
				Operator = @operator;
				Expression = expression;
			}
			
			public override string ToString() {
				return $"Unary({Operator}, {Expression})".Replace("\n", "\n ");
			}
		}
		
		public class Identifier : Node {
			public readonly string Value;
			
			public Identifier(string value) {
				Value = value;
			}
			
			public override string ToString() {
				return Value;
			}
		}
	}
}