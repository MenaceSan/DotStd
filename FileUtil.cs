using System;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace DotStd
{
    public enum MimeId
    {
        // Common IANA MIME types and related file extensions.
        // in table app_mime
        // .NET defines many of these in System.Net.Mime.MediaTypeNames (BUT NOT ALL)

        [Description(@"application/octet-stream")]  // System.Net.Mime.MediaTypeNames.Application.Octet
        bin = 1,    // BIN (AKA BLANK) -> Unknown Binary blob ?

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
        [Description(@"image/svg+xml")]
        svg = 23,        //  Vector images              // Get SVG icons from https://fileicons.org/?view=square-o

        [Description(@"audio/wav")]
        wav = 24,
        [Description(@"audio/mp4")]     // iphone or android voice memos.
        m4a = 25,
        [Description(@"audio/mpeg")]
        mp3 = 26,

        [Description(@"video/avi")] // AKA "video/msvideo"
        avi = 27,
        [Description(@"video/mp4")]
        mp4 = 28,        //  video consumed by chrome. HTML5
        [Description(@"video/mpeg")]
        mpg = 29,        //  video AKA mpg, mpe
        [Description(@"video/x-flv")]
        flv = 30,        //  video
        [Description(@"video/quicktime")]
        mov = 31,   // iPhone movie clips.

        // Other common types:
        // webm (Google video)
        // CAB  (zip type)
        // CSS
        // TGA, TTF
        // AIF, 
        // MKV, SWF, WMV
        // BZ2, ISO
        // CFG, INI,
        // DAT, DB
        // CS, CPP
        // JS, 
        // RichText ?? for email NOT the same as RTF.

        MaxValue,
    }

    public static class FileUtil
    {
        // File system helpers.
        // avoid ",;" as DOS didn't support them as part of a name. https://en.wikipedia.org/wiki/8.3_filename
        // https://en.wikipedia.org/wiki/Filename

        public const string kVirtualStore = "VirtualStore";     // Windows.

        // NOTE: Linux does not like & used in file names? Though windows would allow it.
        public const string kFileNameDos = "!#$&'()-@^_`{}~";     // allow these, but Avoid "%" as it can  be used for encoding?
        public const string kFileNameNT  = "!#$&'()-@^_`{}~,=";   // allow DOS characters + ",="

        public const char kCharNT = '=';    // extra char allowed by NT.
        public const char kEncoder = '%';       // reserve this as it can be used to encode chars and hide things in the string ?

        public const char kDirDos = '\\';
        public const char kDirChar = '/';       // 
        public static readonly char[] kDirSeps = new char[] { kDirDos, kDirChar };

        public const string kDir = "/";     // path directory separator as string. Windows doesn't mind forward slash used as path. Normal for Linux.

        public const string kMimePng = @"image/png";       // like System.Net.Mime.MediaTypeNames.Image.Gif

        public enum AccessType
        {
            Read,
            Create,     // NOT USED ?
            Write,
            Delete,
        }

        public static string GetVirtualStoreName(string filePath)
        {
            // Windows File can be translated from "C:\Program Files\DirName\config.ini" to
            // "C:\Users\<account>\AppData\Local\VirtualStore\Program Files\DirName\config.ini

            string sProgFiles = System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);    // can get moved to VirtualStoreRoot
            if (!filePath.Contains(sProgFiles))    // only stuff in the programfiles folder is part of the virtual store.
                return null;
            string sAppData = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string sVirtualStore = Path.Combine(sAppData, kVirtualStore);   // M$ has a localized version of "VirtualStore" !?
            return Path.Combine(sVirtualStore, filePath.Substring(Path.GetPathRoot(filePath).Length));  // skip root info "c:" etc.
        }

        public static DateTime GetFileTime(DateTime dt)
        {
            // When comparing times we can round down to 2 seconds. FAT stores 2 second accuracy.
            const long ticks2Sec = TimeSpan.TicksPerSecond * 2;
            return new DateTime(dt.Ticks - (dt.Ticks % ticks2Sec), dt.Kind);
        }

        public static bool IsFileNameBasic(char ch, string extra)
        {
            // Is this a valid basic char in a file name?

            if (ch >= 'a' && ch <= 'z')     // always good
                return true;
            if (ch >= 'A' && ch <= 'Z')     // always good
                return true;
            if (StringUtil.IsDigit1(ch))     // always good
                return true;
            if (ch == '.')                  // always assume interior dots are ok. Only DOS 8.3 would have trouble.
                return true;
            if (extra.IndexOf(ch) >= 0)  // DOS + newer allowed chars. kFileNameDos or kFileNameNT
                return true;

            // DOS didn't support "\"*+,/:;<=>?\[]|" in a file name.
            return false;   // some other kind of char.
        }

        public static string MakeNormalizedName(string fileName)
        {
            // put the file in a form that only uses valid chars but try to avoid collisions.
            // Max chars = 255
            // allow file name chars. "-_ ". NOT kDir
            // https://superuser.com/questions/358855/what-characters-are-safe-in-cross-platform-file-names-for-linux-windows-and-os
            // NEVER take chars away from here. we may add them in the future.

            if (fileName == null)
                return null;

            var sb = new StringBuilder();
            int iLen = fileName.Length;
            if (iLen > 255) // max length.
                iLen = 255;

            bool lastEncode = false;
            for (int i = 0; i < iLen; i++)
            {
                char ch = fileName[i];
                if (IsFileNameBasic(ch, kFileNameDos))
                {
                    lastEncode = false;
                }
                else
                {
                    if (lastEncode)
                        continue;
                    ch = kEncoder;   // This might cause collision ??
                    lastEncode = true;
                }
                sb.Append(ch);
            }
            return sb.ToString();
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
            if (iLen > 260) // Max path name length. (or 255?)
                return false;
            if (bAllowSpaces && iLen <= 0)      // empty is ok ?
                return true;
            if (!bAllowRoots)    // allow rooted full path files.
            {
                if (Path.IsPathRooted(filePath))
                    return false;
            }

            for (int i = 0; i < iLen; i++)
            {
                char ch = filePath[i];
                if (IsFileNameBasic(ch, kFileNameNT))        // always good char
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

        public static bool IsReadOnly(string filePath)
        {
            // Assume file exists. is it read only?
            var info = new FileInfo(filePath);
            return info.IsReadOnly;
        }

        public static void RemoveAttributes(string filePath, FileAttributes attributesToRemove)
        {
            // Used to remove read only flag.
            FileAttributes attributes = File.GetAttributes(filePath);
            attributes = attributes & ~attributesToRemove;
            File.SetAttributes(filePath, attributes);
        }
        public static void RemoveReadOnlyFlag(string filePath)
        {
            // Used to remove read only flag.
            RemoveAttributes(filePath, FileAttributes.ReadOnly);
        }

        public static void FileDelete(string filePath)
        {
            // Use this for setting a common breakpoint for file deletes.
            // replace VB Computer.FileSystem.DeleteFile
            // if ( System.IO.File.Exists(filePath)) is not needed.
            System.IO.File.Delete(filePath);
        }

        public static void FileReplace(string filePathSrc, string filePathDest, bool bMove = true)
        {
            // Move/Copy and silently replace any file if it exists.
            // Assume Dir for filePathDest exists.

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

        public static bool IsKnownType(MimeId mimeId)
        {
            return mimeId > DotStd.MimeId.bin;
        }

        public static bool IsImageType(MimeId mimeId)
        {
            // For things that only make sense as images. Avatar, Logo etc.
            switch (mimeId)
            {
                case MimeId.jpg:
                case MimeId.gif:
                case MimeId.png:
                case MimeId.bmp:
                case MimeId.ico:
                case MimeId.svg:
                case MimeId.tiff:
                    return true;
                default:
                    return false;
            }
        }

        public static MimeId GetMimeIdExt(string fileExtension)
        {
            // Convert file name extension to enum MimeId.
            // like Mime type. MimeMapping.GetMimeMapping()
            // Don't allow EXE types.

            switch (fileExtension.ToLower())
            {
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
                case ".csv":
                    return MimeId.csv;
                case ".ico":
                    return MimeId.ico;
                case ".svg":
                    return MimeId.svg;
                case ".tif":
                case ".tiff":
                    return MimeId.tiff;
                case ".mpe":
                case ".mpg":
                    return MimeId.mpg;

                default:
                    return MimeId.bin;    // binary blob.
            }
        }

        public static MimeId GetMimeId(string fileName)
        {
            // Infer MIME type from the name extension.

            return GetMimeIdExt(Path.GetExtension(fileName));
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

        public static string GetContentTypeExt(string fileExtension)
        {
            // given a file extension get the mime type. like MimeMapping.GetMimeMapping()
            // Used with HttpContext.Current.Response.ContentType
            return GetContentType(GetMimeId(fileExtension));
        }

        public static string GetContentType(string fileName)
        {
            // Used with HttpContext.Current.Response.ContentType
            // like MimeMapping.GetMimeMapping()

            return GetContentTypeExt(Path.GetExtension(fileName));
        }
    }
}
