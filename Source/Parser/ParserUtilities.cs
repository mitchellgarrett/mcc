using System.Collections.Generic;
using FTG.Studios.MCC.Lexer;

namespace FTG.Studios.MCC.Parser;

public static partial class Parser
{

	static Token Dequeue(this LinkedList<Token> tokens)
	{
		Token node = tokens.First.Value;
		tokens.RemoveFirst();
		return node;
	}

	static Token Peek(this LinkedList<Token> tokens)
	{
		return tokens.First.Value;
	}
	
	static bool Match(Token token, TokenType expected_type) {
		return token.Type == expected_type;
	}
	
	static bool Match(Token token, TokenType expected_type, object expected_value) {
		return token.Type == expected_type && token.Value.Equals(expected_value);
	}
	
	static void Expect(Token token, TokenType expected_type) {
		if (token.Type != expected_type) {
			throw new ParserException($"Expected: {expected_type}, got: {token}", token);
		}
	}
	
	static void Expect(Token token, TokenType expected_type, object expected_value) {
		if (token.Type != expected_type || !token.Value.Equals(expected_value)) {
			throw new ParserException($"Expected: {expected_type}, {expected_value}, got: {token}", token);
		}
	}
}