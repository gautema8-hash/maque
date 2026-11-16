using System;
using System.Security.Cryptography;
using System.Text;
using MDD.Models;

namespace MDD.Common;

public class SysEncrypt
{
	public static string AESDecrypt(string text)
	{
		byte[] array = Convert.FromBase64String(text);
		RijndaelManaged rijndaelManaged = new RijndaelManaged();
		byte[] bytes = Encoding.ASCII.GetBytes(SysCommon.Miyao);
		rijndaelManaged.KeySize = 128;
		rijndaelManaged.Padding = PaddingMode.PKCS7;
		rijndaelManaged.Mode = CipherMode.ECB;
		using ICryptoTransform cryptoTransform = rijndaelManaged.CreateDecryptor(bytes, null);
		byte[] bytes2 = cryptoTransform.TransformFinalBlock(array, 0, array.Length);
		cryptoTransform.Dispose();
		return Encoding.UTF8.GetString(bytes2);
	}
}
