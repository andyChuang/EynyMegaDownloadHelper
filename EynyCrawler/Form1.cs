using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EynyCrawler;
using System.Collections.Specialized;
using System.IO;
using System.Diagnostics;

namespace EynyCrawler
{
    public partial class Form1 : Form
    {
        string hostUri = System.Configuration.ConfigurationManager.AppSettings["hostUri"];
        string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        Crawler crawler = new Crawler();

        public Form1()
        {
            InitializeComponent();
            textBox2.Text = System.Configuration.ConfigurationManager.AppSettings["account"];
            textBox3.Text = System.Configuration.ConfigurationManager.AppSettings["pwd"];
            // 綁定下拉選單
            Dictionary<string, string> test = new Dictionary<string, string>();
            test.Add("forum-2-1.html", "成人影片");
            test.Add("forum-205-1.html", "電影下載區");
            test.Add("forum-26-1.html", "遊戲下載區");
            test.Add("forum-1716-1.html", "電視劇下載區");
            comboBox1.DataSource = new BindingSource(test, null);
            comboBox1.DisplayMember = "Value";
            comboBox1.ValueMember = "Key";

            comboBox1.SelectedIndex = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {

            try
            {
                textBox1.Text += "程式已啟動"+Environment.NewLine;
                string account = textBox2.Text;
                string pwd = textBox3.Text;

                //建立文字檔
                Random rnd = new Random();
                 string filepath = path+"\\Mega下載連結.txt";
                 if (!File.Exists(filepath))
                {
                    var myFile=File.Create(filepath);
                    myFile.Close();
                    myFile.Dispose();
                }
                //寫入日期
                 using (TextWriter tw = new StreamWriter(filepath, true))
                 {
                     tw.WriteLine("日期:" + System.DateTime.Now.ToString() + Environment.NewLine + Environment.NewLine);
                     tw.Close();
                     tw.Dispose();
                 }

                //取得下拉選單選擇的連結
                 string value = comboBox1.SelectedValue.ToString();
                 //取得論壇首頁的html
                 string HomePage = crawler.getHTMLbyWebRequest(hostUri + value, account, pwd);
                //取得有mega關鍵字的文章連結
                Dictionary<string, string> ResultUrl = crawler.FindLink(HomePage);

                //取得第N頁的Url 用來找下一頁的文章
                Dictionary<string, string> JumpPage = crawler.FindPage(HomePage);

                //將第一頁的結果寫進Dictionary(文章列表)
                WriteToFile(ResultUrl, filepath);
                textBox1.Text += "第1頁完成"+ Environment.NewLine;

                //迴圈取得第2~N頁的html(文章列表)
                foreach (KeyValuePair<string, string> entry in JumpPage)
                {

                    HomePage = crawler.getHTMLbyWebRequest(hostUri + entry.Value, account, pwd);
                     //取得有mega關鍵字的文章連結
                     Dictionary<string, string> result = crawler.FindLink(HomePage);
                     WriteToFile(result, filepath);
                     textBox1.Text += "第"+entry.Key+"頁完成" + Environment.NewLine;
                }
                MessageBox.Show("全部檔案已完成!!");
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }
        //將結果寫進文字檔
        public void WriteToFile(Dictionary<string, string> ResultUrl, string filepath)
        {
            string account = textBox2.Text;
            string pwd = textBox3.Text;

            foreach (KeyValuePair<string, string> entry in ResultUrl)
            {
                string uri = hostUri + entry.Value;
                //取得目前是哪個論壇版塊
                string Type = comboBox1.Text;
                //取得下載頁面html
                string SubPage = crawler.getHTMLbyWebRequest(uri, account, pwd);
                //解析下載地址 
                var result = crawler.GetDownloadLink(SubPage,Type);

                //寫入文字檔
                using (TextWriter tw = new StreamWriter(filepath, true))
                {
                    tw.WriteLine("檔名:" + entry.Key + Environment.NewLine);
                    tw.WriteLine("文章連結:" + hostUri + entry.Value + Environment.NewLine);  
                    tw.WriteLine("檔案大小:" + result.FileSize + Environment.NewLine);
                    tw.WriteLine("解壓密碼:"+result.FilePassword+Environment.NewLine);
                    foreach (var link in result.DownloadLink)
                    {
                        tw.WriteLine("Mega連結:" + link + Environment.NewLine);
                    }
                    tw.WriteLine("===========================");
                    tw.Close();
                    tw.Dispose();
                }

            }
        }
        //連絡作者
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Specify that the link was visited.
            this.linkLabel1.LinkVisited = true;

            // Navigate to a URL.
            System.Diagnostics.Process.Start("mailto:vi000246@hotmail.com");
        }
        //paypal連結
        private void button2_Click(object sender, EventArgs e)
        {
            string url = "";

            string business = "vi000246@gmail.com";  // your paypal email
            string description = "贊助伊莉Mega下載小幫手";            // '%20' represents a space. remember HTML!
            string country = "TW";                  // AU, US, etc.
            string currency = "TWD";                 // AUD, USD, etc.

            url += "https://www.paypal.com/cgi-bin/webscr" +
                "?cmd=" + "_donations" +
                "&business=" + business +
                "&lc=" + country +
                "&item_name=" + description +
                "&currency_code=" + currency +
                "&bn=" + "PP%2dDonationsBF";

            System.Diagnostics.Process.Start(url);
        }
    }
}
