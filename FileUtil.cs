using System;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace DotStd
{
    public enum DocumentType
    {
        // Common IANA MIME types and related file extensions.
        // in table app_doc_type
        // .NET defines many of these in MediaTypeNames
        // Get SVG icons from https://fileicons.org/?view=square-o

        [Description(@"application/octet-stream")]
        BIN = 0,    // BIN (AKA BLANK) -> Unknown Binary blob ?

        [Description(@"application/msword")]
        DOC = 1,    // old binary format.
        [Description(@"application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
        DOCX = 2,    // https://fossbytes.com/doc-vs-docx-file-difference-use/

        [Description(@"application/vnd.ms-excel")]
        XLS = 3,      // old binary format.
        [Description(@"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        XLSX = 4,

        [Description(@"application/vnd.ms-powerpoint")]
        PPT = 5,        // old binary format.
        [Description(@"application/vnd.openxmlformats-officedocument.presentationml.presentation")]
        PPTX = 6,

        [Description(@"application/pdf")]
        PDF = 7,

        [Description(@"image/jpeg")]
        JPG = 8,       // AKA JEPG
        [Description(@"image/png")]
        PNG = 9,
        [Description(@"image/gif")]
        GIF = 10,
        [Description(@"image/bmp")]
        BMP = 11,
        [Description(@"image/x-icon")]
        ICO = 12,

        [Description(@"text/csv")]
        CSV = 13,
        [Description(@"text/html")]
        HTML = 14,  // AKA .htm
        [Description(@"text/plain")]
        TXT = 15,        //  

        [Description(@"image/svg+xml")]
        SVG = 16,        //  Vector images
        [Description(@"video/mp4")]
        MP4 = 17,        //  video consumed by chrome. HTML5
        [Description(@"video/x-flv")]
        FLV = 18,        //  video

        [Description(@"audio/wav")]
        WAV = 19,
        [Description(@"application/zip")]
        ZIP = 20,        //  anything
        [Description(@"application/json")]      // MediaTypeNames.Application.Json
        JSON = 21,        // data

        [Description(@"text/calendar")]  
        ICS = 22,       // https://wiki.fileformat.com/email/ics/

        // AVI
        // MPG
        // webm
        // Other common types:
        // RTF, XML, TGA, TTF
        // AIF, MP3
        // MKV, SWF, MOV, 
        // BZ2, ISO
        // CFG, INI,
        // DAT, DB, CAB
        // CS, CPP, JS, CSS

        MaxValue = 20,
    }

    public static class FileUtil
    {
        // File system helpers.
        // avoid ",;" as DOS didn't support them as part of a name. https://en.wikipedia.org/wiki/8.3_filename

        public const string kVirtualStore = "VirtualStore";
        public const string kFileNameDos = "!#$&'()-@^_`{}~";     // allow these, but Avoid "%" as it can  be used for encoding?
        public const string kFileNameNT = "!#$&'()-@^_`{}~,";   // allow DOS + ","
        public const char kEncoder = '%';       // reserve this as it can be used to encode chars and hide things in the string ?
        public const string kDir = "/";     // path dir sep. Windows doesn't mind forward slash used as path.

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

        public static bool IsFileNameBasic(char ch)
        {
            // Is this a valid basic char in a file name?

            if (ch >= 'a' && ch <= 'z')     // always good
                return true;
            if (ch >= 'A' && ch <= 'Z')     // always good
                return true;
            if (ch >= '0' && ch <= '9')     // always good
                return true;
            if (ch == '.')                  // always assume interior dots are ok. Only DOS 8.3 would have trouble.
                return true;
            //if (kFileNameDos.IndexOf(ch) >= 0)  // even DOS allowed these special chars.
            //    return true;
            if (kFileNameNT.IndexOf(ch) >= 0)  // DOS + newer allowed chars.
                return true;

            // DOS didn't support "\"*+,/:;<=>?\[]|" in a file name.
            return false;   // some other kind of char.
        }

        public static string MakeNormalizedName(string fileName)
        {
            // put the file in a form that only uses valid chars but try to avoid collisions.
            // Max chars = 255
            // allow file name chars. "-_ "
            // https://superuser.com/questions/358855/what-characters-are-safe-in-cross-platform-file-names-for-linux-windows-and-os
            // NEVER take chars away from here. we may add them in the future.

            if (fileName == null)
                return null;

            var sb = new StringBuilder();
            int iLen = fileName.Length;
            if (iLen > 255)
                iLen = 255;

            bool lastEncode = false;
            for (int i = 0; i < iLen; i++)
            {
                char ch = fileName[i];
                if (IsFileNameBasic(ch))
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

        public static bool IsPathValid(string filePath, bool bAllowDirs, bool bAllowSpaces = true, bool bAllowRoots = false, bool bAllowRelatives = false)
        {
            // Keep internal file names simple ASCII. do not allow UNICODE.
            // Is filename composed of simple chars " _-." a-z, A-Z, 0-9, 
            // Assume these are not user created names. Don't need to use any fancy chars.
            // bAllowDirs = allow '/' '\'
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
                if (IsFileNameBasic(ch))        // always good char
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
                if (ch == '/' || ch == '\\')
                {
                    if (!bAllowDirs)
                        return false;
                    // don't allow ../ or ..\
                    if (!bAllowRelatives && i >= 2 && filePath[i - 1] == '.' && filePath[i - 2] == '.')
                        return false;
                    continue;
                }
                return false;       // its bad. Dont allow "%," and others.
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

        public static bool IsImageType(DocumentType docType)
        {
            // For things that only make sense as images. Avatar, Logo etc.
            switch (docType)
            {
                case DocumentType.JPG:
                case DocumentType.GIF:
                case DocumentType.PNG:
                case DocumentType.BMP:
                case DocumentType.ICO:
                case DocumentType.SVG:
                    return true;
                default:
                    return false;
            }
        }

        public static DocumentType GetDocumentTypeIdExt(string fileExtension)
        {
            // Convert file name extension to DocumentType enum.
            // like Mime type. MimeMapping.GetMimeMapping()
            // Don't allow EXE types.

            switch (fileExtension.ToLower())
            {
                case ".jpg":    // more commonly used than .jpeg ext.
                case ".jpeg":
                    return DocumentType.JPG;
                case ".gif":
                    return DocumentType.GIF;
                case ".png":
                    return DocumentType.PNG;
                case ".bmp":
                    return DocumentType.BMP;
                case ".pdf":
                    return DocumentType.PDF;
                case ".doc":
                    return DocumentType.DOC;
                case ".docx":
                    return DocumentType.DOCX;
                case ".xls":
                    return DocumentType.XLS;
                case ".xlsx":
                    return DocumentType.XLSX;
                case ".ppt":
                    return DocumentType.PPT;
                case ".pptx":
                    return DocumentType.PPTX;
                case ".txt":
                    return DocumentType.TXT;
                case ".csv":
                    return DocumentType.CSV;
                case ".ico":
                    return DocumentType.ICO;
                case ".svg":
                    return DocumentType.SVG;
                default:
                    return DocumentType.BIN;    // binary blob.
            }
        }

        public static DocumentType GetDocumentTypeId(string fileName)
        {
            return GetDocumentTypeIdExt(Path.GetExtension(fileName));
        }

        public static string GetContentType(DocumentType docType)
        {
            // Get MIME type for DocumentType
            // e.g. "text/plain"
            // like MimeMapping.GetMimeMapping()

            if (docType <= DocumentType.BIN || docType >= DocumentType.MaxValue)
            {
                // Some other content type? octet-stream
                docType = DocumentType.BIN;
            }

            return docType.ToDescription();
        }

        public static string GetContentTypeExt(string fileExtension)
        {
            // given a file extension get the mime type. like MimeMapping.GetMimeMapping()
            // Used with HttpContext.Current.Response.ContentType
            return GetContentType(GetDocumentTypeId(fileExtension));
        }

        public static string GetContentType(string fileName)
        {
            // Used with HttpContext.Current.Response.ContentType
            // like MimeMapping.GetMimeMapping()

            return GetContentTypeExt(Path.GetExtension(fileName));
        }
    }
}
