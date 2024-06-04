namespace FTG.Studios.MCC {
	
	public enum RegisterType { BP, AX, R10 };
	
	public static class RegisterTypeExtensions {
		
		static readonly string[] register_names = new string[] { "%rbp", "%eax", "%r10d" };
		
		public static string Emit(this RegisterType value) {
			return register_names[(int)value];
		}
	}
}