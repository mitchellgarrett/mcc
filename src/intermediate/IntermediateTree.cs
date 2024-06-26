namespace FTG.Studios.MCC {
	
	public class IntermediateTree {
		
		public readonly IntermediateNode.Program Program;
		
		public IntermediateTree(IntermediateNode.Program program) {
			Program = program;
		}
		
		public override string ToString() {
			return Program.ToString();
		}
	}
}