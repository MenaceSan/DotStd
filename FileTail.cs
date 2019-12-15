using System;
using System.IO;
using System.Collections;

#if false
namespace DotStd
{
    class FileTail
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 0)
                throw new ArgumentNullException("File Name", "you must supply a file name as an argument");

            string fileName = args[0];

            Start:

            try
            {
                using (var reader = new StreamReader(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    //start at the end of the file
                    long lastMaxOffset = reader.BaseStream.Length;

                    while (true)
                    {
                        System.Threading.Thread.Sleep(100);

                        //if the file size has not changed, idle
                        if (reader.BaseStream.Length == lastMaxOffset)
                            continue;

                        //seek to the last max offset
                        reader.BaseStream.Seek(lastMaxOffset, SeekOrigin.Begin);

                        //read out of the file until the EOF
                        string line = "";
                        while ((line = reader.ReadLine()) != null)
                            Console.WriteLine(line);

                        //update the last max offset
                        lastMaxOffset = reader.BaseStream.Position;
                    }
                }
            }
            catch (Exception ex)
            {
                 LoggerUtil.DebugException("EmailMessage.SendSafeAsync", ex);
               Console.WriteLine(ex.ToString());

                //prompt user to restart
                Console.Write("Would you like to try re-opening the file? Y/N:");
                if (Console.ReadLine().ToUpper() == "Y")
                    goto Start;
            }

        }
    }
}
#endif
