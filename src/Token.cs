namespace FTG.Studios.MCC {
	
	public readonly struct Token {
		
		public readonly TokenType Type;
		public readonly object Value;
		
		public Token(TokenType type) {
			this.Type = type;
			Value = null;
		}
		
		public Token(TokenType type, object value) {
			this.Type = type;
			this.Value = value;
		}
		
		public bool IsValid {
			get { return Type != TokenType.Invalid; }
		}
		
		public static Token Invalid {
			get { return new Token(TokenType.Invalid); }
		}
		
		public override string ToString() {
			if (Value == null) return $"{Type}";
			return $"{Type}, {Value}";
		}
	}
}