using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Contract;

namespace Evil
{
    public class MyCode : MarshalByRefObject, ITest
    {
        public MyCode()
        {
            Console.WriteLine("[MyCode] new MyCode();");
        }

        #region ITest Members

        public void MyMethode()
        {
            Evilize();

            Console.WriteLine("[MyCode] MyCode.myMethode();");
        }

        #endregion

        private void Evilize()
        {
            try
            {
                // Test 1, IO
                var file = new StreamWriter(@"C:\evil\csharp_security.txt");
                file.WriteLine("Fail");
                file.Close();
            }
            catch (Exception)
            {
                Console.WriteLine("[Evil] Test 1 passed (File Access)");
            }

            try
            {
                // Test 2, http

                // used to build entire input
                var sb = new StringBuilder();

                // used on each read operation
                var buf = new byte[8192];

                // prepare the web page we will be asking for
                var request = (HttpWebRequest)
                              WebRequest.Create("http://www.google.com");

                // execute the request
                var response = (HttpWebResponse)
                               request.GetResponse();

                // we will read data via the response stream
                Stream resStream = response.GetResponseStream();

                int count = 0;

                do
                {
                    // fill the buffer with data
                    if (resStream != null) count = resStream.Read(buf, 0, buf.Length);

                    // make sure we read some data
                    if (count != 0)
                    {
                        // translate from bytes to ASCII text
                        string tempString = Encoding.ASCII.GetString(buf, 0, count);

                        // continue building the string
                        sb.Append(tempString);
                    }
                } while (count > 0); // any more data to read?

                // print out page source
                Console.WriteLine(sb.ToString());
            }
            catch (Exception)
            {
                Console.WriteLine("[Evil] Test 2 passed (HTTP Request)");
            }


            try
            {
                // Test 3, Socket


                var tcpListener = new TcpListener(IPAddress.Any, 1337);

                tcpListener.AcceptTcpClient();
            }
            catch (Exception)
            {
                Console.WriteLine("[Evil] Test 3 passed (TcpListener)");
            }


            try
            {
                // Test 4, Exit 

                Environment.Exit(1);
            }
            catch (Exception)
            {
                Console.WriteLine("[Evil] Test 4 passed (Environment access)");
            }


/*
            if (false)
            {
                // Test 5, Threading Bomb
                var a = new Thread(DoSleep);
                a.Start();
            }
*/


/*
            if (false)
            {
                // the lazy test 

                Console.WriteLine("I'm lazy...");
                Thread.Sleep(10000);
                Console.WriteLine("I just woke up!");
            }
*/
        }

/*
        private void DoSleep()
        {
            while (true)
            {
                Console.WriteLine("Doing work...");
                Thread.Sleep(1000);
            }
        }
*/

/*
        private void DoWork()
        {
            while (true)
            {
                var t = new Thread(DoWork);
                t.Start();
            }
        }
*/
    }
}