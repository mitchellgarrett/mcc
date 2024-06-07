namespace FTG.Studios.MCC {
	
	public enum RegisterType { BP, AX, DX, R10, R11 };
	
	public static class RegisterTypeExtensions {
		
		static readonly string[] register_names = new string[] { "%rbp", "%eax", "%edx", "%r10d", "%r11d" };
		
		public static string Emit(this RegisterType value) {
			return register_names[(int)value];
		}
		
		public static AssemblyNode.Register ToOperand(this RegisterType type) {
			return new AssemblyNode.Register(type);
		}
	}
}