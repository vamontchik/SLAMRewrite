namespace SLAMRewrite.DataObjects;

public record InMemoryAudioFile(string FileName, string FullFilePath, byte[] Bytes);