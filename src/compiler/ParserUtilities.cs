using System.Collections.Generic;

namespace FTG.Studios.MCC
{

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
}