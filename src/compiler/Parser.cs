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
			
			ParseNode.Expression expression = ParseExpression(tokens, 0);
			
			Expect(tokens.Dequeue(), TokenType.Semicolon);
			
			return new ParseNode.ReturnStatement(expression);
		}
		
		static ParseNode.Expression ParseExpression(Queue<Token> tokens, int min_precedence) {
			ParseNode.Expression left_expression = ParseFactor(tokens);
			
			int current_precedence;
			while (Match(tokens.Peek(), TokenType.BinaryOperator) && (current_precedence = ((Syntax.BinaryOperator)tokens.Peek().Value).GetPrecedence()) >= min_precedence) {
				Token @operator = tokens.Dequeue();
				ParseNode.Expression right_expression = ParseExpression(tokens, current_precedence + 1);
				left_expression = new ParseNode.BinaryExpression((Syntax.BinaryOperator)@operator.Value, left_expression, right_expression);
			}
			
			return left_expression;
		}
		
		static ParseNode.Expression ParseFactor(Queue<Token> tokens) {
			if (Match(tokens.Peek(), TokenType.IntegerConstant)) return ParseConstantExpression(tokens);
			
			if (Match(tokens.Peek(), TokenType.UnaryOperator)) return ParseUnaryExpression(tokens);
			
			if (Match(tokens.Peek(), TokenType.OpenParenthesis)) {
				tokens.Dequeue();
				ParseNode.Expression expression = ParseExpression(tokens, 0);
				Expect(tokens.Dequeue(), TokenType.CloseParenthesis);
				return expression;
			}
			
			return null;
		}
		
		static ParseNode.UnaryExpression ParseUnaryExpression(Queue<Token> tokens) {
			Token @operator = tokens.Dequeue();
			
			// Convert binary subtraction operator to negation
			if (Match(@operator, TokenType.BinaryOperator, Syntax.BinaryOperator.Subtraction)) {
				@operator = new Token(TokenType.UnaryOperator, Syntax.UnaryOperator.Negation);
			}
			Expect(@operator, TokenType.UnaryOperator);
			
			ParseNode.Expression expression = ParseExpression(tokens, 0);
			
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
		
		static bool Match(Token token, TokenType expected_type) {
			return token.Type == expected_type;
		}
		
		static bool Match(Token token, TokenType expected_type, object expected_value) {
			return token.Type == expected_type && token.Value.Equals(expected_value);
		}
		
		static void Expect(Token token, TokenType expected_type) {
			if (token.Type != expected_type) {
				Console.WriteLine($"Expected: {expected_type}, got: {token}");
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