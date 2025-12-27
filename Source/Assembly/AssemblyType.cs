namespace FTG.Studios.MCC.Assembly;

public enum AssemblyType { LongWord, QuadWord };

public static class AssemblyTypeExtensions
{
	public static int GetSize(this AssemblyType type)
	{
		return type switch
		{
			AssemblyType.LongWord => 4,
			AssemblyType.QuadWord => 8,
			_ => throw new System.Exception(),
		};
	}
	
	public static char GetSuffix(this AssemblyType type)
	{
		return type switch
		{
			AssemblyType.LongWord => 'l',
			AssemblyType.QuadWord => 'q',
			_ => throw new System.Exception(),
		};
	}
	
	public static string GetInitializer(this AssemblyType type)
	{
		return type switch
		{
			AssemblyType.LongWord => ".long",
			AssemblyType.QuadWord => ".quad",
			_ => throw new System.Exception(),
		};
	}
}