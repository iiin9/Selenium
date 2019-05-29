using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace MyService1
{
    
    public partial class Service1 : ServiceBase
    {
       // Timer timer;
        String str = "1234567890";
        public Service1()
        {
            InitializeComponent();
        }
       

        protected override void OnStart(string[] args)
        {
           //System.IO.File.Create(AppDomain.CurrentDomain.BaseDirectory + "OnStart " + DateTime.Now.ToString().Replace('.', '_').Replace(':', '_') + ".txt");
            //timer = new Timer();
            //timer.Interval = 1000;
            //timer.Enabled = true;
            //timer.Elapsed += timer1_Tick;
            //timer.Start();
            Thread parserThread = new Thread(new ThreadStart(timer1_Tick));
            parserThread.Start();
        }
        public void timer1_Tick()
        {
                //File.WriteAllText(@"C:\Users\IVAN\source\repos\Seleniumv3.1\Seleniumv3.1\bin\Debug\FuckFuckFuck.txt", "YA WORK'au");
                Dictionary<string, string> Links = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(@"C:\Users\IVAN\source\repos\SeleniumV3.2\SeleniumV3.2\bin\Debug\Links.json"));
                Dictionary<string, string> Text = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(@"C:\Users\IVAN\source\repos\SeleniumV3.2\SeleniumV3.2\bin\Debug\Text.json"));
                Dictionary<string, string[]> Img = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(File.ReadAllText(@"C:\Users\IVAN\source\repos\SeleniumV3.2\SeleniumV3.2\bin\Debug\Img.json"));

                int i = 0;

                string[] LINK = new string[Links.Values.Count];
                string[,] TEXT = new string[2, Links.Keys.Count];
                string[,] IMG = new string[2, Links.Keys.Count];
                foreach (string c in Links.Values)
                {
                    LINK[i] = c;
                    i++;
                }


                i = 0;
                foreach (string c in Links.Keys)
                {
                    TEXT[0, i] = c;
                    if (Text.ContainsKey(TEXT[0, i]))
                    {
                        TEXT[1, i] = Text[TEXT[0, i]];
                    }
                    else
                    {
                        TEXT[1, i] = null;
                    }
                    i++;
                }

                string value;
                i = 0;
                foreach (string c in Links.Keys)
                {
                    value = null;
                    IMG[0, i] = c;
                    if (Img.ContainsKey(IMG[0, i]))
                    {
                        for (int j = 0; j < Img[TEXT[0, i]].GetLength(0); j++)
                            value = value + Img[IMG[0, i]][j] + " ; ";

                        IMG[1, i] = value;
                    }
                    i++;
                }
                //AddToDb(Links, LINK, TEXT, IMG);
                using (UserContext db = new UserContext())
                {
                    int j = 0;
                    //File.AppendAllText(@"C:\Users\IVAN\source\repos\Seleniumv3.1\Seleniumv3.1\bin\Debug\FuckFuckFuck.txt", "BreakPoint2\n\r");
                    foreach (string c in Links.Keys)
                    {
                        Table pt = null;
                        pt = db.Table.Find(c);
                        if (pt == null)
                        {
                            //File.AppendAllText(@"C:\Users\IVAN\source\repos\Seleniumv3.1\Seleniumv3.1\bin\Debug\FuckFuckFuck.txt", "BreakPoint3\n\r");
                            Table Table = new Table { Id = c, Link = LINK[j], Text = TEXT[1, j], Img = IMG[1, j] };
                            db.Table.Add(Table);
                            db.SaveChanges();
                            
                        }
                        j++;
                    }
                }
                Thread.Sleep(200);
            }

        protected override void OnStop()
        {
            using (UserContext db = new UserContext())
            {
                db.SaveChanges();
            }
            Thread.Sleep(1000);
        }
        public class Table
        {
            public string Id { get; set; }
            public string Link { get; set; }
            public string Text { get; set; }
            public string Img { get; set; }
        }


        class UserContext : DbContext
        {
            public UserContext()
                : base("DbConnection")
            { }

            public DbSet<Table> Table { get; set; }
        }
    }
}
