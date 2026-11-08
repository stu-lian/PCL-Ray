using System.Collections.Generic;

namespace PCL.Core.Minecraft.Java.Scanner;

public interface IJavaScanner
{
	void Scan(ICollection<string> results);
}
