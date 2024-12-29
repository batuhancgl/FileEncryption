using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace FileEncryption
{
    public static class EncryptionHelper
    {
        public static string AES_key = "1234567890123456";
        public static string AES_iv = "1234567890123456";
        public static async Task<string> SaveFileZip(IFormFile file, string outputPath)
        {
            outputPath = outputPath.Replace(file.FileName, string.Empty);
            if (file == null || file.Length == 0) return string.Empty;
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FileName);
            var test = file.FileName + ".enc";
            var zipFileName = $"{fileNameWithoutExtension}.zip";
            var rename = await RenameFile(zipFileName, outputPath);
            var zipFilePath = Path.Combine(outputPath, rename);
            var testt = Path.Combine(outputPath, test);
            using (MemoryStream stream = new())
            {
                await file.CopyToAsync(stream);
                using (var zipFileStream = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (var zipArchive = new ZipArchive(zipFileStream, ZipArchiveMode.Create, true))
                    {
                        var zipEntry = zipArchive.CreateEntry(file.FileName);
                        using (var entryStream = zipEntry.Open())
                        {
                            stream.Seek(0, SeekOrigin.Begin);
                            await stream.CopyToAsync(entryStream);
                        }
                    }
                }
            }
            var result = EncryptFile(zipFilePath, testt, Encoding.UTF8.GetBytes(AES_key), Encoding.UTF8.GetBytes(AES_iv));
            return result;
        }
        public static async Task<(byte[] file, string type, string name)> UnZipFileFromZip(string filePath)
        {
            if (!File.Exists(filePath)) return (null, null, null);
            using var ms = await DecryptFile(
                filePath,
                Encoding.UTF8.GetBytes(AES_key),
                Encoding.UTF8.GetBytes(AES_iv));

            using (var zipFileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var zipArchive = new ZipArchive(ms, ZipArchiveMode.Read))
                {
                    var fileEntry = zipArchive.Entries.FirstOrDefault();
                    if (fileEntry == null) return (null, null, null);

                    using (var fileStream = fileEntry.Open())
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await fileStream.CopyToAsync(memoryStream);
                            var contentType = GetMimeType(fileEntry.Name);
                            return (memoryStream.ToArray(), contentType, fileEntry.Name);
                        }
                    }
                }
            }
        }
        public static async Task<MemoryStream> DecryptFile(string inputFile, byte[] key, byte[] iv)
        {
            var outputStream = new MemoryStream();
            using (var fsInput = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var aes = Aes.Create())
            using (var cryptoStream = new CryptoStream(fsInput, aes.CreateDecryptor(key, iv), CryptoStreamMode.Read))
            {
                aes.Key = key;
                aes.IV = iv;
                await cryptoStream.CopyToAsync(outputStream);
            }
            outputStream.Position = 0;
            return outputStream;
        }
        public static string EncryptFile(string inputFile, string outputFile, byte[] key, byte[] iv)
        {
            using (FileStream fsInput = new(inputFile, FileMode.Open, FileAccess.Read))
            {
                using FileStream fsEncrypted = new(outputFile, FileMode.Create, FileAccess.Write);
                using Aes aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;
                using CryptoStream cs = new(fsEncrypted, aes.CreateEncryptor(), CryptoStreamMode.Write);
                byte[] buffer = new byte[1024];
                int read;
                while ((read = fsInput.Read(buffer, 0, buffer.Length)) > 0)
                {
                    cs.Write(buffer, 0, read);
                }
            }
            System.IO.File.Delete(inputFile);
            return outputFile;
        }
        public static async Task<string> RenameFile(string fileName, string fileFolderFullPath)
        {
            if (!Directory.Exists(fileFolderFullPath))
            {
                Directory.CreateDirectory(fileFolderFullPath);
            }
            string fileExtension = Path.GetExtension(fileName);
            string fileNameWithoutExtension = CharacterRegulatory(Path.GetFileNameWithoutExtension(fileName));
            string filePath = Path.Combine(fileFolderFullPath, fileNameWithoutExtension + fileExtension);
            if (File.Exists(filePath))
            {
                int fileCount = 1;
                string newFileName = CharacterRegulatory(fileNameWithoutExtension) + "-" + fileCount + fileExtension;
                filePath = Path.Combine(fileFolderFullPath, newFileName);

                while (File.Exists(filePath))
                {
                    fileCount++;
                    newFileName = CharacterRegulatory(fileNameWithoutExtension) + "-" + fileCount + fileExtension;
                    filePath = Path.Combine(fileFolderFullPath, newFileName);
                }
                return newFileName;
            }
            else
            {
                string newFileName = CharacterRegulatory(fileNameWithoutExtension) + fileExtension;
                return newFileName;
            }
        }
        public static string CharacterRegulatory(string name)
          => name.Replace("\"", "")
              .Replace("!", "")
              .Replace("'", "")
              .Replace("^", "")
              .Replace("+", "")
              .Replace("%", "")
              .Replace("&", "")
              .Replace("/", "")
              .Replace("(", "")
              .Replace(")", "")
              .Replace("=", "")
              .Replace("?", "")
              .Replace("_", "")
              .Replace(" ", "-")
              .Replace("@", "")
              .Replace("€", "")
              .Replace("¨", "")
              .Replace("~", "")
              .Replace(",", "")
              .Replace(";", "")
              .Replace(":", "")
              .Replace(".", "-")
              .Replace("Ö", "o")
              .Replace("ö", "o")
              .Replace("Ü", "u")
              .Replace("ü", "u")
              .Replace("ı", "i")
              .Replace("İ", "i")
              .Replace("ğ", "g")
              .Replace("Ğ", "g")
              .Replace("æ", "")
              .Replace("ß", "")
              .Replace("â", "a")
              .Replace("î", "i")
              .Replace("ş", "s")
              .Replace("Ş", "s")
              .Replace("Ç", "c")
              .Replace("ç", "c")
              .Replace("<", "")
              .Replace(">", "")
              .Replace("|", "");
        public static string GetMimeType(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".txt" => "text/plain",
                ".html" => "text/html",
                _ => "application/octet-stream",
            };
        }
        public static IFormFile ConvertFilePathToIFormFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Dosya bulunamadı.", filePath);

            // Dosya akışını aç
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            // Dosya adı ve içerik türü
            var fileName = Path.GetFileName(filePath);
            var contentType = GetMimeType(filePath); // İsteğe bağlı: içerik türü belirleme

            // FormFile oluştur
            var formFile = new FormFile(fileStream, 0, fileStream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
            return formFile;
        }
        public static string EncryptString(string plainText, byte[] key, byte[] iv)
        {
            using Aes aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Padding = PaddingMode.PKCS7;
            using MemoryStream memoryStream = new();
            using (CryptoStream cryptoStream = new(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                using (StreamWriter streamWriter = new(cryptoStream))
                {
                    streamWriter.Write(plainText);
                }
            }
            var testo = Convert.ToBase64String(memoryStream.ToArray());
            return testo;
        }

        public static string DecryptString(string inputFile, byte[] key, byte[] iv)
        {
            using Aes aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Padding = PaddingMode.PKCS7;
            using MemoryStream memoryStream = new(Convert.FromBase64String(inputFile));
            using CryptoStream cryptoStream = new(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using StreamReader streamReader = new(cryptoStream);
            return streamReader.ReadToEnd();
        }

    }
}