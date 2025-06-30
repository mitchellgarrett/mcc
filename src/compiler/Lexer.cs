using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FTG.Studios.MCC {
	
	public static class Lexer {
		
		public static List<Token> Tokenize(string source) {
			List<Token> tokens = new List<Token>();
			string lexeme = string.Empty;
			for (int index = 0; index < source.Length; index++)
			{
				char c = source[index];
				
				if (char.IsWhiteSpace(c)) {
					if (!string.IsNullOrEmpty(lexeme))
					{
						tokens.Add(BuildToken(lexeme));
						lexeme = string.Empty;
					}
					continue;
				}
				
				Token token = BuildToken(c);
				if (token.IsValid) {
					Token current_token = BuildToken(lexeme);
					if (current_token.IsValid)
					{
						tokens.Add(current_token);
					}
					tokens.Add(token);
					lexeme = string.Empty;
					continue;
				}
				
				lexeme += c;
			}
			
			if (!string.IsNullOrEmpty(lexeme)) tokens.Add(BuildToken(lexeme));
			
			return tokens;
		}
		
		static Token BuildToken(char lexeme) {
			switch (lexeme) {
				
				// Puncuation
				case Syntax.semicolon: return new Token(TokenType.Semicolon);
				case Syntax.open_parenthesis: return new Token(TokenType.OpenParenthesis);
				case Syntax.close_parenthesis: return new Token(TokenType.CloseParenthesis);
				case Syntax.open_brace: return new Token(TokenType.OpenBrace);
				case Syntax.close_brace: return new Token(TokenType.CloseBrace);
				
				// Unary Operators
				case Syntax.operator_bitwise_complement: return new Token(TokenType.UnaryOperator, Syntax.UnaryOperator.BitwiseComplement);
				
				// Binary Operators
				case Syntax.operator_addition: return new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.Addition);
				case Syntax.operator_subtraction: return new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.Subtraction);
				case Syntax.operator_multiplication: return new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.Multiplication);
				case Syntax.operator_division: return new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.Division);
				case Syntax.operator_remainder: return new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.Remainder);
				case Syntax.operator_assignment: return new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.Assignment);
			}
			
			return Token.Invalid;
		}
		
		static Token BuildToken(string lexeme) {
			// Unary operators
			if (lexeme == Syntax.operator_decrement) return new Token(TokenType.UnaryOperator, Syntax.UnaryOperator.Decrement);
			
			// Binary operators
			if (lexeme == Syntax.operator_logical_and) return new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.LogicalAnd);
			if (lexeme == Syntax.operator_logical_or) return new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.LogicalOr);
			if (lexeme == Syntax.operator_logical_equal) return new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.LogicalEqual);
			if (lexeme == Syntax.operator_logical_not_equal) return new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.LogicalNotEqual);
			if (lexeme == Syntax.operator_logical_less) return new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.LogicalLess);
			if (lexeme ==  Syntax.operator_logical_greater) return new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.LogicalGreather);
			if (lexeme == Syntax.operator_logical_less_equal) return new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.LogicalLessEqual);
			if (lexeme == Syntax.operator_logical_greater_equal) return new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.LogicalGreatherEqual);
			
			// Check if keyword
			for	(int index = 0; index < Syntax.keywords.Length; index++) {
				if (lexeme == Syntax.keywords[index]) return new Token(TokenType.Keyword, (Syntax.Keyword)index);
			}
			
			// Check if integer literal
			if (Regex.IsMatch(lexeme, Syntax.integer_literal))
			{
				return new Token(TokenType.IntegerConstant, int.Parse(lexeme));
			}
			
			// Check if identifier
			if (Regex.IsMatch(lexeme, Syntax.identifier))
			{
				return new Token(TokenType.Identifier, lexeme);
			}
			
			return Token.Invalid;
		}
	}
}