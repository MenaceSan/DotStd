using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DotStd
{
    /// <summary>
    /// Common IANA MIME types and related file extensions.
    /// in table app_mime
    /// .NET defines many of these in System.Net.Mime.MediaTypeNames (BUT NOT ALL)
    /// Sample icons for each mime/file type can be found: https://fileicons.org/?view=square-o
    /// </summary>
    public enum MimeId
    {
        [Description(@"application/octet-stream")]  // System.Net.Mime.MediaTypeNames.Application.Octet
        bin = 1,    // BIN (AKA BLANK) -> Unknown Binary blob ? default type.

        [Description(@"application/msword")]
        doc = 2,    // old binary format.
        [Description(@"application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
        docx = 3,    // https://fossbytes.com/doc-vs-docx-file-difference-use/

        [Description(@"application/vnd.ms-excel")]
        xls = 4,      // old binary format.
        [Description(@"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        xlsx = 5,

        [Description(@"application/vnd.ms-powerpoint")]
        ppt = 6,        // old binary format.
        [Description(@"application/vnd.openxmlformats-officedocument.presentationml.presentation")]
        pptx = 7,

        [Description(@"application/pdf")]   // System.Net.Mime.MediaTypeNames.Application.Pdf
        pdf = 8,
        [Description(@"application/rtf")]   // System.Net.Mime.MediaTypeNames.Application.Rtf
        rtf = 9,
        [Description(@"application/zip")]
        zip = 10,        //  anything
        [Description(@"application/json")]      // System.Net.Mime.MediaTypeNames.Application.Json
        json = 11,        // data

        [Description(@"text/html")]
        html = 12,  // AKA .htm
        [Description(@"text/plain")]
        txt = 13,        //  
        [Description(@"text/csv")]
        csv = 14,
        [Description(@"text/xml")]      // System.Net.Mime.MediaTypeNames.Text.Xml
        xml = 15,       // not the same as application/xml ?
        [Description(@"text/calendar")]
        ics = 16,       // https://wiki.fileformat.com/email/ics/

        [Description(@"image/jpeg")]
        jpg = 17,       // AKA JEPG
        [Description(@"image/png")]
        png = 18,
        [Description(@"image/gif")]
        gif = 19,
        [Description(@"image/bmp")]
        bmp = 20,
        [Description(@"image/x-icon")]
        ico = 21,
        [Description(@"image/tiff")]
        tiff = 22,

        [Description(@"image/tga")]
        tga = 23,
        [Description(@"image/webp")]
        webp = 24,  // google image.

        [Description(@"image/svg+xml")]
        svg = 25,        //  Vector images              // Get SVG icons from https://fileicons.org/?view=square-o

        [Description(@"audio/wav")]
        wav = 26,
        [Description(@"audio/mp4")]     // iphone or android voice memos.
        m4a = 27,
        [Description(@"audio/mpeg")]
        mp3 = 28,
        [Description(@"audio/webm")]
        weba = 29,

        [Description(@"video/avi")] // AKA "video/msvideo"
        avi = 30,
        [Description(@"video/mp4")]
        mp4 = 31,        //  video consumed by chrome. HTML5
        [Description(@"video/mpeg")]
        mpg = 32,        //  video AKA mpg, mpe
        [Description(@"video/x-flv")]
        flv = 33,        //  video
        [Description(@"video/quicktime")]
        mov = 34,   // iPhone movie clips.
        [Description(@"video/webm")]
        webm = 35,   // Google video 

        // Other common file types:
        // CAB  (zip type)
        // BZ2,     // compressed binary
        // AIF, (audio)
        // CSS
        // TTF (font)
        // MKV, SWF, WMV (video)
        // ISO  (?)
        // CFG, INI, (text config)
        // DAT, DB
        // CS, CPP
        // JS, 
        // RichText ?? for email NOT the same as RTF.
        // ogg (supported by mozilla)

        MaxValue,
    }

    public static class FileUtil
    {
        // File system helpers.
        // avoid ",;" as DOS didn't support them as part of a name. https://en.wikipedia.org/wiki/8.3_filename
        // https://en.wikipedia.org/wiki/Filename

        public const int kLenMax = 255; // max safe file name length. (or 260?)

        // NOTE: Linux does not like & used in file names? Though Windows would allow it.
        public const string kFileNameUrl = "!'()-_~";  // Extra chars that are safe for URL, DOS and NT.
        public const string kFileNameDos = "!'()-_~#$&@^`{}";     // allow these, but Avoid "%" as it can  be used for encoding? safe for DOS and NT
        public const string kFileNameNT  = "!'()-_~#$&@^`{},=";   // allow DOS characters plus ",=". Safe for NT. what about '+' ?

        public const char kCharNT = '=';        // extra char allowed by NT. used to extend the file name.
        public const char kEncoder = '%';       // reserve this as it can be used to encode chars and hide things in the string ?

        public const char kDirDos = '\\';       // used only for Windows/DOS
        public const char kDirChar = '/';       // 
        public static readonly char[] kDirSeps = new char[] { kDirDos, kDirChar };

        public const string kDir = "/";     // path directory separator as string. Windows doesn't mind forward slash used as path. Normal for Linux.

        public const string kVirtualStore = "VirtualStore";     // Windows virtualization junk.

        public const string kExtHtm = ".htm";
        public const string kExtHtml = ".html";
        public const string kExtCsv = ".csv";

        public const string kMimePng = @"image/png";       // like System.Net.Mime.MediaTypeNames.Image.Gif

        public enum AccessType
        {
            // What sort of file access do i have?
            Read,
            Create,     // NOT USED ?
            Write,
            Delete,
        }

        public static bool IsKnownType(MimeId mimeId)
        {
            // else just treat as a binary blob.
            return mimeId > DotStd.MimeId.bin;
        }

        public static bool IsImageType(MimeId mimeId)
        {
            // Is this MimeId type an image? e.g. Avatar, Logo etc.
            switch (mimeId)
            {
                case MimeId.jpg:
                case MimeId.gif:
                case MimeId.png:
                case MimeId.bmp:
                // case MimeId.ico:    // Supported?
                case MimeId.tiff:
                case MimeId.tga:
                case MimeId.webp:
                case MimeId.svg:
                    return true;
                default:
                    return false;
            }
        }

        public static MimeId GetMimeIdFromExt(string fileExtension)
        {
            // Convert file name extension to enum MimeId.
            // like Mime type. MimeMapping.GetMimeMapping()
            // Don't allow EXE types.

            switch (fileExtension.ToLower())
            {
                case kExtHtml:
                case kExtHtm:
                    return MimeId.html;

                case ".jpg":    // more commonly used than .jpeg ext.
                case ".jpeg":
                    return MimeId.jpg;
                case ".gif":
                    return MimeId.gif;
                case ".png":
                    return MimeId.png;
                case ".bmp":
                    return MimeId.bmp;
                case ".pdf":
                    return MimeId.pdf;
                case ".doc":
                    return MimeId.doc;
                case ".docx":
                    return MimeId.docx;
                case ".xls":
                    return MimeId.xls;
                case ".xlsx":
                    return MimeId.xlsx;
                case ".ppt":
                    return MimeId.ppt;
                case ".pptx":
                    return MimeId.pptx;
                case ".txt":
                    return MimeId.txt;
                case kExtCsv:
                    return MimeId.csv;
                case ".ico":
                    return MimeId.ico;
                case ".svg":
                    return MimeId.svg;
                case ".tif":
                case ".tiff":
                    return MimeId.tiff;
                case ".tga":
                    return MimeId.tga;
                case ".webp":
                    return MimeId.webp;
                case ".mpe":
                case ".mpg":
                    return MimeId.mpg;

                default:
                    return MimeId.bin;    // binary blob.
            }
        }

        public static MimeId GetMimeIdFromFileName(string fileName)
        {
            // Infer MIME type from the file name (and extension).

            return GetMimeIdFromExt(Path.GetExtension(fileName));
        }

        public static string GetContentType(MimeId mimeId)
        {
            // Get MIME type name for MimeId id.
            // e.g. "text/plain"
            // like MimeMapping.GetMimeMapping()

            if (mimeId <= MimeId.bin || mimeId >= MimeId.MaxValue)
            {
                // Some other content type? octet-stream
                mimeId = MimeId.bin;
            }

            return mimeId.ToDescription();
        }

        public static MimeId GetMimeIdFromContentType(string contentType)
        {
            // convert the mime trype name to enum . MimeId
            // TODO Reversee lookup string against. MimeId.Description

            return MimeId.bin;
        }

        public static string GetContentTypeExt(string fileExtension)
        {
            // given a file extension get the mime type as mime type name string. like MimeMapping.GetMimeMapping()
            // Used with HttpContext.Current.Response.ContentType
            return GetContentType(GetMimeIdFromFileName(fileExtension));
        }

        public static string GetContentType(string fileName)
        {
            // Used with HttpContext.Current.Response.ContentType
            // like MimeMapping.GetMimeMapping()

            return GetContentTypeExt(Path.GetExtension(fileName));
        }

        public static DateTime GetFileTime(DateTime dt)
        {
            // When comparing times we can round down to 2 seconds. FAT stores 2 second accuracy.
            const long ticks2Sec = TimeSpan.TicksPerSecond * 2;
            return new DateTime(dt.Ticks - (dt.Ticks % ticks2Sec), dt.Kind);
        }

        public static bool IsNameCharValid(char ch, string otherCharsAllowed)
        {
            // Is this a valid basic char in a file name? ALL file systems and URL support this ?

            if (ch < ' ')
                return false;   // Special chars are NEVER allowed
            if (ch >= 'a' && ch <= 'z')     // always good
                return true;
            if (ch >= 'A' && ch <= 'Z')     // always good
                return true;
            if (StringUtil.IsDigit1(ch))     // always good
                return true;
            if (ch == '.')                  // always assume interior dots are OK. Only DOS 8.3 would have trouble.
                return true;

            if (otherCharsAllowed.Contains(ch))  // allow these safe chars. DOS + newer allowed chars. kFileNameDos or kFileNameNT
                return true;

            // e.g. DOS didn't support "\"*+,/:;<=>?\[]|" in a file name.
            return false;   // some other kind of char.
        }

        public static bool IsDirSep(char ch)
        {
            return ch == kDirChar || ch == kDirDos;
        }

        public static bool IsPathValid(string filePath, bool bAllowDirs, bool bAllowSpaces = true, bool bAllowRoots = false, bool bAllowRelatives = false)
        {
            // Keep internal file names simple ASCII. do not allow UNICODE.
            // Is filename composed of simple chars " _-." a-z, A-Z, 0-9, 
            // Assume these are not user created names. Don't need to use any fancy chars.
            // bAllowDirs = allow '/' '\', kDirChar
            // bAllowRoots = allow ':' or "\\"
            // bAllowRelatives = allow "..". beware . %2e%2e%2f represents ../ https://www.owasp.org/index.php/Path_Traversal
            // bAllowSpaces = allow spaces.
            // similar to System.Security.Permissions.FileIOPermission.CheckIllegalCharacters
            // NEVER allow "*?"<>|"

            int iLen = filePath.Length;
            if (iLen > kLenMax) // Max path name length. (or 260?)
                return false;
            if (bAllowSpaces && iLen <= 0)      // empty is OK ?
                return true;
            if (!bAllowRoots)    // allow rooted full path files.
            {
                if (Path.IsPathRooted(filePath))
                    return false;
            }

            for (int i = 0; i < iLen; i++)
            {
                char ch = filePath[i];
                if (IsNameCharValid(ch, kFileNameNT))        // always good char
                    continue;
                if (ch == ' ')
                {
                    if (!bAllowSpaces)
                        return false;
                    continue;
                }
                if (ch == ':')
                {
                    if (!bAllowRoots)
                        return false;
                    continue;
                }
                if (IsDirSep(ch))
                {
                    if (!bAllowDirs)
                        return false;
                    // don't allow ../ or ..\
                    if (!bAllowRelatives && i >= 2 && filePath[i - 1] == '.' && filePath[i - 2] == '.')
                        return false;
                    continue;
                }
                return false;       // its bad. Don't allow "%," and others.
            }
            return true;
        }

        public static string? EncodeSafeName(string fileName)
        {
            // encode a UNICODE string into a valid/safe file name. a form that only uses valid chars safe for file systems and URL.
            // Max chars = 255 = kLenMax
            // allow file name chars like "-_". NOT kDir
            // https://superuser.com/questions/358855/what-characters-are-safe-in-cross-platform-file-names-for-linux-windows-and-os
            // https://www.w3schools.com/tags/ref_urlencode.ASP

            byte[] bytes = Encoding.UTF8.GetBytes(fileName);  // get UTF8 encoding as bytes.

            var sb = new StringBuilder();

            for (int i = 0; i < bytes.Length; i++)  // UTF8 length
            {
                byte b = bytes[i];
                char ch = (char)b;

                if (!IsNameCharValid(ch, kFileNameUrl))
                {
                    if (ch < ' ')
                    {
                        return null;   // Special chars are NEVER allowed. Bad filename.
                    }

                    if (sb.Length + 3 > kLenMax) // truncate.
                        break;

                    sb.Append(kEncoder);    // encode char as %XX
                    SerializeUtil.ToHexChars(sb, b);
                    continue;
                }

                if (sb.Length >= kLenMax)   // truncate. max length.
                    break;
                sb.Append(ch);
            }

            return sb.ToString();
        }

        public static string? DecodeSafeName(string fileName)
        {
            // Get the displayable UNICODE name from the safe encoded (URL or file system) name.
            // Decode the encoded special chars.
            // @RETURN null if  this is not a valid encoded name!

            int len = fileName.Length;
            if (len > kLenMax) // max length exceeded! this is not a valid encoded name!
            {
                return null;
            }

            var lb = new List<byte>();
            for (int i = 0; i < len; i++)
            {
                char ch = fileName[i];

                if (ch == kEncoder)
                {
                    // decode char from %XX 
                    i++;
                    if (i + 2 > len)
                        break;
                    int v2 = SerializeUtil.FromHexChar2(fileName, i);
                    if (v2 < 0)
                    {
                        return null;    // this should NOT happen!  this is not a valid encoded name!
                    }
                    ch = (char)v2;
                    i++;
                }
                else if (!IsNameCharValid(ch, kFileNameUrl))
                {
                    return null;    // this should NOT happen!  this is not a valid encoded name!
                }

                lb.Add((byte)ch);
            }

            // Convert UTF8 to string.
            return Encoding.UTF8.GetString(lb.ToArray());      // filename is now UNICODE.
        }

        public static string? GetVirtualStoreName(string filePath)
        {
            // Windows File can be translated from "C:\Program Files\DirName\config.ini" (Where i put it)
            // to "C:\Users\<account>\AppData\Local\VirtualStore\Program Files\DirName\config.ini  (Where Windows actually put it)

            string progFiles = System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);    // can get moved to VirtualStoreRoot
            if (!filePath.Contains(progFiles))    // only stuff in the ProgramFiles folder is part of the virtual store.
                return null;

            string appData = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string virtualStore = Path.Combine(appData, kVirtualStore);   // M$ has a localized version of "VirtualStore" !?
            int lenRoot = Path.GetPathRoot(filePath)?.Length ?? 0;
            return Path.Combine(virtualStore, filePath.Substring(lenRoot));  // skip root info "c:" etc.
        }

        public static bool IsReadOnly(string filePath)
        {
            // Assume file info.Exists. is it read only?
            var info = new FileInfo(filePath);
            return info.IsReadOnly;
        }

        public static void RemoveAttributes(string filePath, FileAttributes attributesToRemove)
        {
            // Used to remove read only flag.
            FileAttributes attributes = File.GetAttributes(filePath);
            attributes &= ~attributesToRemove;
            File.SetAttributes(filePath, attributes);
        }
        public static void RemoveReadOnlyFlag(string filePath)
        {
            // Used to remove read only flag.
            RemoveAttributes(filePath, FileAttributes.ReadOnly);
        }

        /// <summary>
        /// This is the same default buffer size as
        /// <see cref="StreamReader"/> and <see cref="FileStream"/>.
        /// </summary>
        public const int kDefaultBufferSize = 4096;

        /// <summary>
        /// Indicates that
        /// 1. The file is to be used for asynchronous reading.
        /// 2. The file is to be accessed sequentially from beginning to end.
        /// </summary>
        private const FileOptions kDefaultOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;

        public static async Task<List<string>> ReadAllLinesAsync(string path, Encoding encoding)
        {
            var lines = new List<string>();

            // Open the FileStream with the same FileMode, FileAccess
            // and FileShare as a call to File.OpenText would've done.
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, kDefaultBufferSize, kDefaultOptions))
            {
                using (var reader = new StreamReader(stream, encoding))
                {
                    string? line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        lines.Add(line);
                    }
                }
            }

            return lines;
        }
        public static Task<List<string>> ReadAllLinesAsync(string path)
        {
            return ReadAllLinesAsync(path, Encoding.UTF8);
        }


        public static async Task<string> ReadAllTextAsync(string path)
        {
            // Read a text or HTML format file.
            // NOTE: This may block on open. Not true async. M$ samples say this is ok. Win32 has no async file open !!!!
            // return await File.ReadAllTextAsync(path, Encoding.UTF8); // ALSO May block on file open!? AND Not available in .NET standard ?

            using (var fileRead = new StreamReader(path, Encoding.UTF8))
            {
                return await fileRead.ReadToEndAsync();
            }
        }

        public static void FileDelete(string filePath)
        {
            // Use this for setting a common breakpoint for file deletes.
            // replace VB Computer.FileSystem.DeleteFile
            // if ( System.IO.File.Exists(filePath)) is not needed. delete will succeed anyhow.
            System.IO.File.Delete(filePath);
        }

        /// <summary>
        /// Move/Copy and silently replace any file if it exists.
        /// Assume Dir for filePathDest exists.
        /// </summary>
        /// <param name="filePathSrc"></param>
        /// <param name="filePathDest"></param>
        /// <param name="bMove"></param>
        public static void FileReplace(string filePathSrc, string filePathDest, bool bMove = true)
        {
            if (filePathSrc == filePathDest)
                return;
            if (!DirUtil.DirCreateForFile(filePathDest))
            {
                FileDelete(filePathDest);
            }
            if (bMove)
                System.IO.File.Move(filePathSrc, filePathDest);
            else
                System.IO.File.Copy(filePathSrc, filePathDest, true);
        }
    }
}
