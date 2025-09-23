namespace FTG.Studios.MCC {
	
	public readonly struct Token {

		public readonly int Line;
		public readonly TokenType Type;
		public readonly object Value;
		
		public Token(int line, TokenType type) {
			Line = line;
			Type = type;
			Value = null;
		}
		
		public Token(int line, TokenType type, object value) {
			Line = line;
			Type = type;
			Value = value;
		}
		
		public bool IsValid {
			get { return Type != TokenType.Invalid; }
		}
		
		public static Token Invalid(int line) {
			return new Token(line, TokenType.Invalid);
		}
		
		public override string ToString() {
			if (Value == null) return $"{Type}";
			if (Type == TokenType.Comment || Type == TokenType.Identifier)
				return $"{Type}, \"{Value}\"";
			return $"{Type}, {Value}";
		}
	}
}