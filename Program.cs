using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Data.SqlClient;
using System.Data.Entity;

namespace SeleniumV3._2
{
    class Program
    {
        static void Main(string[] args)
        {
            ChromeDriver chromeDriver = new ChromeDriver();
            Login(chromeDriver);
            Thread.Sleep(3000);
            Parser(chromeDriver);
        }
        public static Mutex mtx = new Mutex();
        static void Parser(ChromeDriver chromeDriver)
        {
            List<IWebElement> webElements = new List<IWebElement>();
            Dictionary<string, string> linkDic = new Dictionary<string, string>();
            Dictionary<string, string> textDic = new Dictionary<string, string>();
            Dictionary<string, string[]> imgDic = new Dictionary<string, string[]>();

            Thread linkThread = new Thread(() => WriteLink(linkDic));
            Thread textThread = new Thread(() => WriteText(textDic));
            Thread imgThread = new Thread(() => WriteImg(imgDic));
            Thread readThread = new Thread(() => Read());

            linkThread.Start();
            textThread.Start();
            imgThread.Start();
            readThread.Start();
            for (; ; )
            {
                chromeDriver.ExecuteScript("window.scrollBy(0,2000)");
                webElements = (from item in chromeDriver.FindElements(By.CssSelector("[id^=post-]")) select item).ToList();
                foreach (IWebElement item in webElements)
                {
                    string id = item.GetAttribute("id").ToString();
                    if (!linkDic.ContainsKey(id))
                    {
                        if (item.FindElements(By.CssSelector("._post_content .post_link")).Count != 0)
                            linkDic.Add(id, item.FindElement(By.CssSelector("._post_content .post_link")).GetAttribute("href").ToString());
                        if (item.FindElements(By.CssSelector("._post_content .wall_post_text")).Count != 0)
                            textDic.Add(id, item.FindElement(By.CssSelector("._post_content .wall_post_text")).Text);
                        if (item.FindElements(By.CssSelector(".wall_post_cont >.page_post_sized_thumbs > a[onclick *= showPhoto]")).Count != 0)
                        {
                            List<IWebElement> imgList = (from itemImg in item.FindElements(By.CssSelector(".wall_post_cont >.page_post_sized_thumbs > a[onclick *= showPhoto]"))
                                                         select itemImg).ToList();
                            int j = 0;
                            string[] imgArr = new string[imgList.Count];
                            foreach (IWebElement itemImg in imgList)
                            {
                                string img = itemImg.GetAttribute("style").ToString();
                                img = img.Substring(img.IndexOf('/') - 6);
                                img = img.Substring(0, img.Length - 3);
                                imgArr[j] = img;
                                j++;
                            }
                            imgDic.Add(id, imgArr);
                        }
                    }
                }
                Thread.Sleep(50);
            }
        }
        static void Login(ChromeDriver chromeDriver)
        {
            chromeDriver.Navigate().GoToUrl("https://vk.com/feed");
            Auth(chromeDriver, "email", "");
            Auth(chromeDriver, "pass", "");
            List<IWebElement> webElements = chromeDriver.FindElementsById("login_button").ToList();
            foreach (IWebElement item in webElements)
            {
                if (!item.Displayed)
                {
                    continue;
                }
                if (!item.Text.ToLower().Equals("войти"))
                {
                    continue;
                }
                item.Click();
                break;
            }
        }

        static void Auth(ChromeDriver chromeDriver, string value1, string value2)
        {
            List<IWebElement> webElements = (from item in chromeDriver.FindElementsByName(value1) where item.Displayed select item).ToList();
            if (!webElements.Any())
                return;
            webElements[0].SendKeys(value2);
        }

        private static void WriteLink(Dictionary<string, string> linkDic)
        {
            for (; ; )
            {
                mtx.WaitOne();
                while (linkDic.Count == 0)
                    Thread.Sleep(10);
                if (!System.IO.File.Exists("Link.json"))
                    File.WriteAllText("Link.json", JsonConvert.SerializeObject(linkDic));
                else
                {
                    Dictionary<string, string> fileDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("Link.json"));
                    lock (linkDic)
                    {
                        foreach (string s in linkDic.Keys)
                        {
                            if (!fileDic.ContainsKey(s))
                                fileDic.Add(s, linkDic[s]);
                        }
                        File.WriteAllText("Link.json", JsonConvert.SerializeObject(fileDic));
                    }
                }
                mtx.ReleaseMutex();
                Thread.Sleep(100);
            }
        }
        public static void WriteText(Dictionary<string, string> textDic)
        {
            for (; ; )
            {
                mtx.WaitOne();
                lock (textDic)
                {
                    while (textDic.Count == 0)
                        Thread.Sleep(10);
                    if (!System.IO.File.Exists("Text.json"))
                        File.WriteAllText("Text.json", JsonConvert.SerializeObject(textDic));
                    else
                    {
                        Dictionary<string, string> fileDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("Text.json"));
                        foreach (string s in textDic.Keys)
                        {
                            if (!fileDic.ContainsKey(s))
                                fileDic.Add(s, textDic[s]);
                        }
                        File.WriteAllText("Text.json", JsonConvert.SerializeObject(fileDic));
                    }
                }
                mtx.ReleaseMutex();
                Thread.Sleep(100);
            }
        }
        public static void WriteImg(Dictionary<string, string[]> imgDic)
        {
            for (; ; )
            {
                mtx.WaitOne();
                while (imgDic.Count == 0)
                    Thread.Sleep(10);
                if (!System.IO.File.Exists("Img.json"))
                    File.WriteAllText("Img.json", JsonConvert.SerializeObject(imgDic));
                else
                {
                    Dictionary<string, string[]> fileDic = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(File.ReadAllText("Img.json"));
                    lock (imgDic)
                    {
                        foreach (string s in imgDic.Keys)
                        {
                            if (!fileDic.ContainsKey(s))
                                fileDic.Add(s, imgDic[s]);
                        }
                        File.WriteAllText("Img.json", JsonConvert.SerializeObject(fileDic));
                    }
                }
                mtx.ReleaseMutex();
                Thread.Sleep(100);
            }
        }
        public static void Read()
        {
            for (; ; )
            {
                if (!System.IO.File.Exists("Link.json"))
                    continue;
                else
                {
                    mtx.WaitOne();
                    ReadFile();
                    mtx.ReleaseMutex();
                }
            }
        }
        public static void ReadFile()
        {
            if (System.IO.File.Exists("Link.json") && System.IO.File.Exists("Text.json") && System.IO.File.Exists("Img.json"))
            {
                StreamReader id = new StreamReader("Link.json");
                StreamReader text = new StreamReader("Text.json");
                StreamReader img = new StreamReader("Img.json");
                Dictionary<string, string> Link = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("Link.json"));
                Dictionary<string, string> Text = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("Text.json"));
                Dictionary<string, string[]> Img = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(File.ReadAllText("Img.json"));

                int i = 0;

                string[] LINK = new string[Link.Values.Count];
                string[,] TEXT = new string[2, Link.Keys.Count];
                string[,] IMG = new string[2, Link.Keys.Count];
                foreach (string c in Link.Values)
                {
                    LINK[i] = c;
                    i++;
                }

                i = 0;
                foreach (string c in Link.Keys)
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
                foreach (string c in Link.Keys)
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
                AddToDb(Link, LINK, TEXT, IMG);
                Thread.Sleep(2000);
                id.Close();
                text.Close();
                img.Close();
            }
        }
        public static void AddToDb(Dictionary<string, string> Link, string[] LINK, string[,] TEXT, string[,] IMG)
        {
            using (UserContext db = new UserContext())
            {
                int i = 0;

                foreach (string c in Link.Keys)
                {
                    Table pt = null;
                    pt = db.Posts.Find(c);
                    if (pt == null)
                    {
                        Table post = new Table { Id = c, Link = LINK[i], Text = TEXT[1, i], Img = IMG[1, i] };
                        db.Posts.Add(post);
                        db.SaveChanges();
                    }
                    i++;
                }
            }
        }
        class UserContext : DbContext
        {
            public UserContext()
                : base("DbConnection")
            { }
            public DbSet<Table> Posts { get; set; }
        }
        public class Table
        {
            public string Id { get; set; }
            public string Link { get; set; }
            public string Text { get; set; }
            public string Img { get; set; }
        }
    }
}