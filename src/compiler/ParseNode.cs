using System.Collections.Generic;

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
			public readonly Block Body;
			
			public Function(Identifier identifier, Block body) {
				Identifier = identifier;
				Body = body;
			}
			
			public override string ToString() {
				return $"Function(\nIdentifier=\"{Identifier}\"\nBody(\n{Body}\n)".Replace("\n", "\n ") + "\n)";
			}
		}
		
		public class BlockItem : Node { }
		
		public class Declaration : BlockItem {
			public readonly Identifier Identifier;
			public readonly Expression Source;
			
			public Declaration(Identifier identifier, Expression source) {
				Identifier = identifier;
				Source = source;
			}

			public override string ToString()
			{
				if (Source != null)
					return $"Declaration({Identifier}, {Source})";
				return $"Declaration({Identifier})";
			}
		}
		
		public class Statement : BlockItem { }
		
		public class ReturnStatement : Statement {
			public readonly Expression Expression;
			
			public ReturnStatement(Expression expression) {
				Expression = expression;
			}
			
			public override string ToString() {
				return $"Return(\n{Expression}".Replace("\n", "\n ") + "\n)";
			}
		}

		public class IfStatement : Statement
		{
			public readonly Expression Condition;
			public readonly Statement Then;
			public readonly Statement Else;

			public IfStatement(Expression condition, Statement then, Statement @else)
			{
				Condition = condition;
				Then = then;
				Else = @else;
			}

			public override string ToString()
			{
				return $"If({Condition}, {Then}, {Else})".Replace("\n", "\n ");
			}
		}
		
		public class Block : Statement
		{
			public readonly List<BlockItem> Items;

			public Block(List<BlockItem> items)
			{
				Items = items;
			}
			
			public override string ToString() {
				string body_text = " ";
				foreach (BlockItem item in Items)
					body_text += (item.ToString() + '\n').Replace("\n", "\n ");
				return $"{body_text}\n)".Replace("\n", "\n ") + "\n)";
			}
		}
		
		public class Expression : Statement { }
		
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
				return $"Binary({Operator}, {LeftExpression}, {RightExpression})".Replace("\n", "\n ");
			}
		}

		public class ConditionalExpression : Expression
		{
			public readonly Expression Condition;
			public readonly Expression Then;
			public readonly Expression Else;
			
			public ConditionalExpression(Expression condition, Expression then, Expression @else)
			{
				Condition = condition;
				Then = then;
				Else = @else;
			}

			public override string ToString()
			{
				return $"Conditional({Condition}, {Then}, {Else})".Replace("\n", "\n ");
			}
		}
		
		public class Factor : Expression { }
		
		public class Constant : Factor {
			public readonly int Value;
			
			public Constant(int value) {
				Value = value;
			}
			
			public override string ToString() {
				return $"Constant({Value})".Replace("\n", "\n ");
			}
		}
		
		public class Variable : Factor {
			public readonly Identifier Identifier;
			
			public Variable(Identifier identifier) {
				Identifier = identifier;
			}
			
			public override string ToString() {
				return $"Variable({Identifier})";
			}
		}
		
		public class Assignment : Expression {
			public readonly Expression Destination;
			public readonly Expression Source;
			
			public Assignment(Expression destination, Expression source) {
				Destination = destination;
				Source = source;
			}
			
			public override string ToString() {
				return $"Assignment({Destination}, {Source})";
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