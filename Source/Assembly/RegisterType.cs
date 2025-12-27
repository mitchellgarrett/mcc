namespace FTG.Studios.MCC.Assembly;

public enum RegisterType { BP, AX, CX, DX, DI, SI, R8, R9, R10, R11, SP };

public static class RegisterTypeExtensions
{

	static readonly string[] register_names_8 = ["%bp", "%al", "%cl", "%dl", "%dil", "%sil", "%r8b", "r9b", "%r10b", "%r11b", "%sp"];
	static readonly string[] register_names_32 = ["%ebp", "%eax", "%ecx", "%edx", "%edi", "%esi", "%r8d", "%r9d", "%r10d", "%r11d", "%esp"];
	static readonly string[] register_names_64 = ["%rbp", "%rax", "%rcx", "%rdx", "%rdi", "%rsi", "%r8", "%r9", "%r10", "%r11", "%rsp"];

	public static string Emit8(this RegisterType value)
	{
		return register_names_8[(int)value];
	}

	public static string Emit32(this RegisterType value)
	{
		return register_names_32[(int)value];
	}

	public static string Emit64(this RegisterType value)
	{
		return register_names_64[(int)value];
	}

	public static AssemblyNode.Register ToOperand(this RegisterType type)
	{
		return new AssemblyNode.Register(type);
	}

	public static readonly RegisterType[] FunctionCallOrder = [RegisterType.DI, RegisterType.SI, RegisterType.DX, RegisterType.CX, RegisterType.R8, RegisterType.R9];
}