using System.Collections.Generic;
using FTG.Studios.MCC.Lexer;

namespace FTG.Studios.MCC.Parser;

public static partial class Parser
{

	static Token Dequeue(this LinkedList<Token> tokens) {
		Token node = tokens.First.Value;
		tokens.RemoveFirst();
		return node;
	}

	static Token Peek(this LinkedList<Token> tokens) {
		return tokens.First.Value;
	}
}