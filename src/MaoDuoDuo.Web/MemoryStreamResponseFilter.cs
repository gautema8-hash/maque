using System;
using System.IO;
using System.Text;
using CefSharp;
using MDD.Models;

namespace MaoDuoDuo.Web;

public class MemoryStreamResponseFilter : IResponseFilter, IDisposable
{
	private MemoryStream memoryStream = new MemoryStream();

	public FilterStatus Filter(Stream dataIn, out long dataInRead, Stream dataOut, out long dataOutWritten)
	{
		dataInRead = (dataOutWritten = 0L);
		if (dataIn == null)
		{
			return FilterStatus.Done;
		}
		byte[] array = new byte[dataIn.Length];
		int num = dataIn.Read(array, 0, array.Length);
		if (num > 0)
		{
			StringBuilder stringBuilder = new StringBuilder();
			ConvertLargeBufferToStringBuilder(array, stringBuilder, 8192);
			SysCommon.ResponseJson.Append(stringBuilder);
			dataOut.Write(array, 0, num);
			dataInRead = num;
			dataOutWritten = num;
			return FilterStatus.NeedMoreData;
		}
		return FilterStatus.Done;
	}

	private static void ConvertLargeBufferToStringBuilder(byte[] buffer, StringBuilder result, int chunkSize)
	{
		if (buffer == null || result == null)
		{
			throw new ArgumentNullException("buffer 或 result 参数为 null");
		}
		int num = 0;
		for (num = 0; num < buffer.Length; num += chunkSize)
		{
			int count = Math.Min(chunkSize, buffer.Length - num);
			string @string = Encoding.UTF8.GetString(buffer, num, count);
			result.Append(@string);
		}
	}

	private static Stream ConvertStringBuilderToStream(StringBuilder sb)
	{
		MemoryStream memoryStream = new MemoryStream();
		using (StreamWriter streamWriter = new StreamWriter(memoryStream, Encoding.UTF8, 1024, leaveOpen: true))
		{
			for (int i = 0; i < sb.Length; i += 8192)
			{
				int num = Math.Min(8192, sb.Length - i);
				streamWriter.Write(sb.ToString(i, num));
			}
		}
		memoryStream.Position = 0L;
		return memoryStream;
	}

	public bool InitFilter()
	{
		return true;
	}

	public void Dispose()
	{
		memoryStream.Dispose();
	}

	public string GetResponseData()
	{
		return Encoding.UTF8.GetString(memoryStream.ToArray());
	}
}
