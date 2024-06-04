using System;
using System.Collections.Generic;

namespace FTG.Studios.MCC
{
	
	public static class Parser {
		
		public static ParseTree Parse(List<Token> tokens) {
			Queue<Token> stream = new Queue<Token>(tokens);
			ParseNode.Program program = ParseProgram(stream);
			return new ParseTree(program);
		}
		
		static ParseNode.Program ParseProgram(Queue<Token> tokens) {
			ParseNode.Function function = ParseFunction(tokens);
			return new ParseNode.Program(function);
		}
		
		static ParseNode.Function ParseFunction(Queue<Token> tokens) {
			Expect(tokens.Dequeue(), TokenType.Keyword, Syntax.Keyword.Integer);
			
			Expect(tokens.Peek(), TokenType.Identifier);
			ParseNode.Identifier identifier = ParseIdentifier(tokens);
			
			Expect(tokens.Dequeue(), TokenType.OpenParenthesis);
			
			Expect(tokens.Dequeue(), TokenType.Keyword, Syntax.Keyword.Void);
			
			Expect(tokens.Dequeue(), TokenType.CloseParenthesis);
			Expect(tokens.Dequeue(), TokenType.OpenBrace);
			
			ParseNode.Statement body = ParseStatement(tokens);
			
			Expect(tokens.Dequeue(), TokenType.CloseBrace);
			
			return new ParseNode.Function(identifier, body);
		}
		
		static ParseNode.Statement ParseStatement(Queue<Token> tokens) {
			return ParseReturnStatement(tokens);
		}
		
		static ParseNode.ReturnStatement ParseReturnStatement(Queue<Token> tokens) {
			Expect(tokens.Dequeue(), TokenType.Keyword, Syntax.Keyword.Return);
			
			ParseNode.Expression expression = ParseExpression(tokens);
			
			Expect(tokens.Dequeue(), TokenType.Semicolon);
			
			return new ParseNode.ReturnStatement(expression);
		}
		
		static ParseNode.Expression ParseExpression(Queue<Token> tokens) {
			if (Match(tokens.Peek(), TokenType.OpenParenthesis)) {
				tokens.Dequeue();
				ParseNode.Expression expression = ParseUnaryExpression(tokens);
				Expect(tokens.Dequeue(), TokenType.CloseParenthesis);
				return expression;
			}
			
			if (Match(tokens.Peek(), TokenType.UnaryOperator)) return ParseUnaryExpression(tokens);
			
			return ParseConstantExpression(tokens);
		}
		
		static ParseNode.UnaryExpression ParseUnaryExpression(Queue<Token> tokens) {
			Token @operator = tokens.Dequeue();
			Expect(@operator, TokenType.UnaryOperator);
			
			ParseNode.Expression expression = ParseExpression(tokens);
			
			return new ParseNode.UnaryExpression((Syntax.UnaryOperator)@operator.Value, expression);
		}
		
		static ParseNode.ConstantExpression ParseConstantExpression(Queue<Token> tokens) {
			Token token = tokens.Dequeue();
			Expect(token, TokenType.IntegerConstant);
			return new ParseNode.ConstantExpression((int)token.Value);
		}
		
		static ParseNode.Identifier ParseIdentifier(Queue<Token> tokens) {
			Token token = tokens.Dequeue();
			Expect(token, TokenType.Identifier);
			return new ParseNode.Identifier((string)token.Value);
		}
		
		static bool Match(Token token, TokenType expected) {
			return token.Type == expected;
		}
		
		static void Expect(Token token, TokenType expected) {
			if (token.Type != expected) {
				Console.WriteLine($"Expected: {expected}, got: {token}");
				Environment.Exit(1);
			}
		}
		
		static void Expect(Token token, TokenType expected_type, object expected_value) {
			if (token.Type != expected_type || !token.Value.Equals(expected_value)) {
				Console.WriteLine($"Expected: {expected_type}, {expected_value}, got: {token}");
				Environment.Exit(1);
			}
		}
	}
}