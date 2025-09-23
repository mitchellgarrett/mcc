using System.Collections.Generic;

namespace FTG.Studios.MCC {
	
	public static class ParseNode { 
		
		public abstract class Node { }
		
		public class Program : Node {
			public readonly List<FunctionDeclaration> FunctionDeclarations;
			
			public Program(List<FunctionDeclaration> functionDeclarations) {
				FunctionDeclarations = functionDeclarations;
			}
			
			public override string ToString() {
				return $"Program(\n{string.Join(", ", FunctionDeclarations)}".Replace("\n", "\n ") + "\n)";
			}
		}
		
		public abstract class BlockItem : Node { }
		
		public abstract class Declaration : BlockItem { }
		
		public class VariableDeclaration : Declaration, ForInitialization {
			public readonly Identifier Identifier;
			public readonly Expression Source;
			
			public VariableDeclaration(Identifier identifier, Expression source) {
				Identifier = identifier;
				Source = source;
			}

			public override string ToString()
			{
				if (Source != null)
					return $"VariableDeclaration({Identifier}, {Source})";
				return $"VariableDeclaration({Identifier})";
			}
		}
		
		public class FunctionDeclaration : Declaration {
			public readonly Identifier Identifier;
			public readonly List<Identifier> Parameters;
			public readonly Block Body;
			
			public FunctionDeclaration(Identifier identifier, List<Identifier> parameters, Block body) {
				Identifier = identifier;
				Parameters = parameters;
				Body = body;
			}
			
			public override string ToString() {
				return $"FunctionDeclaration(\nIdentifier=\"{Identifier}\", {string.Join(", ", Parameters)}\nBody(\n{Body}\n)".Replace("\n", "\n ") + "\n)";
			}
		}
		
		public class Statement : BlockItem { }
		
		public class Return : Statement {
			public readonly Expression Expression;
			
			public Return(Expression expression) {
				Expression = expression;
			}
			
			public override string ToString() {
				return $"Return(\n{Expression}".Replace("\n", "\n ") + "\n)";
			}
		}

		public class If : Statement
		{
			public readonly Expression Condition;
			public readonly Statement Then;
			public readonly Statement Else;
			public string InternalLabel;

			public If(Expression condition, Statement then, Statement @else)
			{
				Condition = condition;
				Then = then;
				Else = @else;
			}

			public override string ToString()
			{
				return $"If(Label=\"{InternalLabel}\", {Condition}, {Then}, {Else})".Replace("\n", "\n ");
			}
		}

		public class Break : Statement
		{
			public string InternalLabel;
			
			public override string ToString()
			{
				return $"Break(\"{InternalLabel}\")";
			}
		}
		
		public class Continue : Statement
		{
			public string InternalLabel;
			
			public override string ToString()
			{
				return $"Continue(\"{InternalLabel}\")";
			}
		}
		
		public class While : Statement
		{
			public readonly Expression Condition;
			public readonly Statement Body;
			public string InternalLabel;

			public While(Expression condition, Statement body)
			{
				Condition = condition;
				Body = body;
			}
			
			public override string ToString()
			{
				return $"While(Label=\"{InternalLabel}\", {Condition}, {Body})".Replace("\n", " \n");
			}
		}
		
		public class DoWhile : Statement
		{
			public readonly Expression Condition;
			public readonly Statement Body;
			public string InternalLabel;

			public DoWhile(Expression condition, Statement body)
			{
				Condition = condition;
				Body = body;
			}
			
			public override string ToString()
			{
				return $"DoWhile(Label=\"{InternalLabel}\", {Condition}, {Body})".Replace("\n", " \n");
			}
		}

		public interface ForInitialization { }
		
		public class For : Statement
		{
			public readonly ForInitialization Initialization;
			public readonly Expression Condition;
			public readonly Expression Post;
			public readonly Statement Body;
			public string InternalLabel;

			public For(ForInitialization initialization, Expression condition, Expression post, Statement body)
			{
				Initialization = initialization;
				Condition = condition;
				Post = post;
				Body = body;
			}

			public override string ToString()
			{
				return $"For(Label=\"{InternalLabel}\", {Initialization}, {Condition}, {Body})".Replace("\n", " \n");
			}
		}
		
		public class Block : Statement
		{
			public readonly List<BlockItem> Items;

			public Block(List<BlockItem> items)
			{
				Items = items;
			}

			public override string ToString()
			{
				string body_text = " ";
				foreach (BlockItem item in Items)
					body_text += (item.ToString() + '\n').Replace("\n", "\n ");
				return $"{body_text}\n)".Replace("\n", "\n ") + "\n)";
			}
		}
		
		public class Expression : Statement, ForInitialization { }
		
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

		public class Conditional : Expression
		{
			public readonly Expression Condition;
			public readonly Expression Then;
			public readonly Expression Else;
			
			public Conditional(Expression condition, Expression then, Expression @else)
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

		public class FunctionCall : Expression
		{
			public readonly Identifier Identifier;
			public readonly List<Expression> Arguments;

			public FunctionCall(Identifier identifier, List<Expression> arguments)
			{
				Identifier = identifier;
				Arguments = arguments;
			}
			
			public override string ToString()
			{
				string output = $"FunctionCall(Identifier=\"{Identifier}\"";
				foreach (var a in Arguments) output += $", {a}";
				output += ")";
				return output;
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