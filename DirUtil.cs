﻿using System;
using System.IO;

namespace DotStd
{
    /// <summary>
    /// Helper util for directories.
    /// </summary>
    public static class DirUtil
    {
        /// <summary>
        /// .NET oddly lacks a deep/recursive directory copy function.
        /// </summary>
        /// <param name="sourceDirName"></param>
        /// <param name="destDirName"></param>
        /// <param name="skipIfExists"></param>
        public static void DirCopy(string sourceDirName, string destDirName, bool skipIfExists)
        {
            var dir = new DirectoryInfo(sourceDirName);

            // If the source directory does not exist, throw an exception.
            if (!dir.Exists)
            {
                // throw new DirectoryNotFoundException( "Source directory does not exist or could not be found: " + sourceDirName);
                return;
            }

            // If the destination directory does not exist, create it.
            if (Directory.Exists(destDirName))
            {
                if (skipIfExists)
                    return;
            }
            else
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the file contents of the directory to copy.
            FileInfo[] files = dir.GetFiles();
            DirectoryInfo[] dirs = dir.GetDirectories();

            foreach (FileInfo file in files)
            {
                // Create the path to the new copy of the file.
                string temppath = Path.Combine(destDirName, file.Name);

                // Copy the file.
                file.CopyTo(temppath, false);
            }

            foreach (DirectoryInfo subdir in dirs)
            {
                // Create the subdirectory.
                string tempPath = Path.Combine(destDirName, subdir.Name);

                // Copy the sub-directories.
                DirCopy(subdir.FullName, tempPath, false);
            }
        }

        /// <summary>
        /// Will create any missing parent directories as well.
        /// like VB FileIO.FileSystem.DirectoryExists, FileIO.FileSystem.CreateDirectory
        /// ignore if the dir already exists.
        /// Will throw on failure.
        /// </summary>
        /// <param name="sDir"></param>
        /// <returns>true = the dir needed to be created. it didn't exist.</returns>
        public static bool DirCreate(string? sDir)
        {
            if (String.IsNullOrEmpty(sDir)) // this dir.
                return false;
            if (System.IO.Directory.Exists(sDir))   // is this needed?
                return false;
            System.IO.Directory.CreateDirectory(sDir);
            return true;
        }

        public static bool DirCreateForFile(string filePath)
        {
            // I'm about to open a file for writing. make sure the directory exists.
            return DirUtil.DirCreate(Path.GetDirectoryName(filePath));
        }

        /// <summary>
        /// Delete all files in a directory. Possibly recursive.
        /// remove ReadOnly bits.
        /// </summary>
        /// <param name="dirPath"></param>
        /// <param name="bRecursive"></param>
        /// <returns></returns>
        public static int DirEmpty(string dirPath, bool bRecursive = false)
        {
            if (!System.IO.Directory.Exists(dirPath))   // avoid DirectoryNotFoundException
                return -1;
            int iCount = 0;
            string[] files = Directory.GetFiles(dirPath);
            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);    // remove ReadOnly bit
                File.Delete(file);
                iCount++;
            }
            if (bRecursive)
            {
                string[] dirs = Directory.GetDirectories(dirPath);
                foreach (string sDir2 in dirs)
                {
                    iCount += DirDelete(sDir2);
                }
            }
            return iCount;
        }

        public static int DirDelete(string sDir)
        {
            // Empty a dir then delete the dir itself. recursive.
            int iCount = DirEmpty(sDir, true);
            if (iCount < 0)   // Empty it myself. i can change attributes.
                return -1;
            System.IO.Directory.Delete(sDir, false);
            return iCount + 1;
        }

        public static int DirEmptyOld(string sDir, int nOlderThanHours = 24, string sPattern = "*.*")
        {
            // Delete just old stuff in this sDir.
            if (!System.IO.Directory.Exists(sDir))   // avoid DirectoryNotFoundException
                return -1; // nothing to delete
            int iCount = 0;
            try
            {
                DateTime tExpired = DateTime.UtcNow.Subtract(TimeSpan.FromHours(nOlderThanHours));     
                var dir = new DirectoryInfo(sDir);
                foreach (System.IO.FileInfo file in dir.GetFiles(sPattern))
                {
                    if (file.CreationTimeUtc < tExpired)    
                    {
                        file.Delete();
                        iCount++;
                    }
                }
            }
            catch (Exception)
            {
                // OK ignore errors, as this is not critical functionality
            }
            return iCount;
        }
    }
}
