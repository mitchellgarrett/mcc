namespace FTG.Studios.MCC {
	
	public class ParseTree {
		
		public readonly ParseNode.Program Program;
		
		public ParseTree(ParseNode.Program program) {
			Program = program;
		}
		
		public override string ToString() {
			return Program.ToString();
		}
	}
}