using System.Collections.Generic;
using FTG.Studios.MCC.Lexer;

namespace FTG.Studios.MCC.Parser;

public static class ParseNode { 
	
	public abstract class Node;
	
	public class Program(List<Declaration> declarations) : Node {
		public readonly List<Declaration> Declarations = declarations;

		public override string ToString() {
			return $"Program(\n{string.Join(", ", Declarations)}".Replace("\n", "\n ") + "\n)";
		}
	}
	
	public abstract class BlockItem : Node;
	
	public abstract class Declaration : BlockItem;
	
	public enum StorageClass { None, Static, Extern }
	
	public enum PrimitiveType { Integer, Long };
	
	public class VariableDeclaration(Identifier identifier, PrimitiveType type, StorageClass storage_class, Expression source) : Declaration, ForInitialization
	{
		public readonly Identifier Identifier = identifier;
		public readonly PrimitiveType Type = type;
		public readonly StorageClass StorageClass = storage_class;
		public readonly Expression Source = source;

		public override string ToString()
		{
			if (Source != null)
				return $"VariableDeclaration(\"{Identifier}\", {Type}, {StorageClass}, {Source})";
			return $"VariableDeclaration(\"{Identifier}\", {Type}, {StorageClass})";
		}
	}
	
	public class FunctionDeclaration(Identifier identifier, PrimitiveType return_type, StorageClass storage_class, List<Identifier> parameters, Block body) : Declaration {
		public readonly Identifier Identifier = identifier;
		public readonly PrimitiveType ReturnType = return_type;
		public readonly StorageClass StorageClass = storage_class;
		public readonly List<Identifier> Parameters = parameters;
		public readonly Block Body = body;

		public override string ToString() {
			return $"FunctionDeclaration(\nIdentifier=\"{Identifier}\",\nReturnType={ReturnType},\nStorage={StorageClass},\n {string.Join(", ", Parameters)}\nBody(\n{Body}\n)".Replace("\n", "\n ") + "\n)";
		}
	}
	
	public class Statement : BlockItem;
	
	public class Return(Expression expression) : Statement {
		public readonly Expression Expression = expression;

		public override string ToString() {
			return $"Return(\n{Expression}".Replace("\n", "\n ") + "\n)";
		}
	}

	public class If(Expression condition, Statement then, Statement @else) : Statement
	{
		public readonly Expression Condition = condition;
		public readonly Statement Then = then;
		public readonly Statement Else = @else;
		public string InternalLabel;

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
	
	public class While(Expression condition, Statement body) : Statement
	{
		public readonly Expression Condition = condition;
		public readonly Statement Body = body;
		public string InternalLabel;

		public override string ToString()
		{
			return $"While(Label=\"{InternalLabel}\", {Condition}, {Body})".Replace("\n", " \n");
		}
	}
	
	public class DoWhile(Expression condition, Statement body) : Statement
	{
		public readonly Expression Condition = condition;
		public readonly Statement Body = body;
		public string InternalLabel;

		public override string ToString()
		{
			return $"DoWhile(Label=\"{InternalLabel}\", {Condition}, {Body})".Replace("\n", " \n");
		}
	}

	public interface ForInitialization;
	
	public class For(ForInitialization initialization, Expression condition, Expression post, Statement body) : Statement
	{
		public readonly ForInitialization Initialization = initialization;
		public readonly Expression Condition = condition;
		public readonly Expression Post = post;
		public readonly Statement Body = body;
		public string InternalLabel;

		public override string ToString()
		{
			return $"For(Label=\"{InternalLabel}\", {Initialization}, {Condition}, {Body})".Replace("\n", " \n");
		}
	}
	
	public class Block(List<BlockItem> items) : Statement
	{
		public readonly List<BlockItem> Items = items;

		public override string ToString()
		{
			string body_text = " ";
			foreach (BlockItem item in Items)
				body_text += (item.ToString() + '\n').Replace("\n", "\n ");
			return $"{body_text}\n)".Replace("\n", "\n ") + "\n)";
		}
	}
	
	public class Expression : Statement, ForInitialization;
	
	public class BinaryExpression(Syntax.BinaryOperator @operator, Expression left_expression, Expression right_expression) : Expression {
		public readonly Syntax.BinaryOperator Operator = @operator;
		public readonly Expression LeftExpression = left_expression;
		public readonly Expression RightExpression = right_expression;

		public override string ToString() {
			return $"Binary({Operator}, {LeftExpression}, {RightExpression})".Replace("\n", "\n ");
		}
	}

	public class Conditional(Expression condition, Expression then, Expression @else) : Expression
	{
		public readonly Expression Condition = condition;
		public readonly Expression Then = then;
		public readonly Expression Else = @else;

		public override string ToString()
		{
			return $"Conditional({Condition}, {Then}, {Else})".Replace("\n", "\n ");
		}
	}

	public class FunctionCall(Identifier identifier, List<Expression> arguments) : Expression
	{
		public readonly Identifier Identifier = identifier;
		public readonly List<Expression> Arguments = arguments;

		public override string ToString()
		{
			string output = $"FunctionCall(Identifier=\"{Identifier}\"";
			foreach (var a in Arguments) output += $", {a}";
			output += ")";
			return output;
		}
	}
	
	public class Cast(PrimitiveType target_type, Expression expression)
	{
		public readonly PrimitiveType TargetType = target_type;
		public readonly Expression Expression = expression;
		
		public override string ToString()
		{
			return $"Cast({TargetType}, {Expression})".Replace("\n", "\n ");
		}
	}

	public class Factor : Expression { }
	
	public class Constant : Factor;

	public class IntegerConstant(int value) : Constant
	{
		public readonly int Value = value;

		public override string ToString()
		{
			return $"IntegerConstant({Value})".Replace("\n", "\n ");
		}
	}
	
	public class LongConstant(long value) : Constant {
		public readonly long Value = value;

		public override string ToString() {
			return $"LongConstant({Value})".Replace("\n", "\n ");
		}
	}
	
	public class Variable(Identifier identifier) : Factor {
		public readonly Identifier Identifier = identifier;

		public override string ToString() {
			return $"Variable({Identifier})";
		}
	}
	
	public class Assignment(Expression destination, Expression source) : Expression {
		public readonly Expression Destination = destination;
		public readonly Expression Source = source;

		public override string ToString() {
			return $"Assignment({Destination}, {Source})";
		}
	}
	
	public class UnaryExpression(Syntax.UnaryOperator @operator, Expression expression) : Factor {
		public readonly Syntax.UnaryOperator Operator = @operator;
		public readonly Expression Expression = expression;

		public override string ToString() {
			return $"Unary({Operator}, {Expression})".Replace("\n", "\n ");
		}
	}
	
	public class Identifier(string value) : Node {
		public readonly string Value = value;

		public override string ToString() {
			return Value;
		}
	}
}